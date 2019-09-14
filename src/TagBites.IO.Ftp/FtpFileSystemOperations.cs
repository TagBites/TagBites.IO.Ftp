using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using FluentFTP;
using TagBites.IO.Operations;

namespace TagBites.IO.Ftp
{
    internal class FtpFileSystemOperations : IFileSystemOperations, IFileSystemPermissionsOperations, IDisposable
    {
        private bool HashCodeNotSupported { get; set; }

        private FluentFTP.FtpClient Client { get; }

        public FtpFileSystemOperations(string address, string username, string password, Encoding encoding = null, FtpDataConnectionType connectionType = FtpDataConnectionType.AutoPassive)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            address = address.Replace("\\", "/");
            if (!address.StartsWith("ftp://", StringComparison.CurrentCultureIgnoreCase))
                address = "ftp://" + address;

            var url = new Uri(address);
            var host = url.Host;
            var rootDirectory = url.AbsolutePath;
            var port = url.Port > 0 ? url.Port : 21;

            if (!string.IsNullOrEmpty(rootDirectory) && rootDirectory != "/")
                throw new ArgumentNullException(nameof(address));

            Client = new FtpClient(host, port, new NetworkCredential(username, password))
            {
                Encoding = encoding ?? Encoding.UTF8,
                DataConnectionType = connectionType,
                //EncryptionMode = FtpEncryptionMode.Explicit,
                RetryAttempts = 3
            };
            Client.ValidateCertificate += (control, args) => args.Accept = true;
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName) => GetInfo(fullName);
        public string CorrectPath(string path) => path;

        public Stream ReadFile(FileLink file)
        {
            lock (Client)
                using (var ms = new MemoryStream())
                {
                    using (var rs = Client.OpenRead(file.FullName))
                        rs.CopyTo(ms);

                    Client.GetReply();
                    return new MemoryStream(ms.ToArray());
                }
        }
        public IFileLinkInfo WriteFile(FileLink file, Stream stream)
        {
            lock (Client)
                Client.Upload(stream, file.FullName, FtpExists.Overwrite, true);

            return GetFileInfo(file.FullName);
        }
        public IFileLinkInfo MoveFile(FileLink source, FileLink destination)
        {
            lock (Client)
                Client.MoveFile(source.FullName, destination.FullName);

            return GetFileInfo(destination.FullName);
        }
        public void DeleteFile(FileLink file)
        {
            lock (Client)
                Client.DeleteFile(file.FullName);
        }

        public IFileSystemStructureLinkInfo CreateDirectory(DirectoryLink directory)
        {
            lock (Client)
                Client.CreateDirectory(directory.FullName);

            return GetDirectoryInfo(directory.FullName);
        }
        public IFileSystemStructureLinkInfo MoveDirectory(DirectoryLink source, DirectoryLink destination)
        {
            lock (Client)
                Client.MoveDirectory(source.FullName, destination.FullName);

            return GetDirectoryInfo(destination.FullName);
        }
        public void DeleteDirectory(DirectoryLink directory, bool recursive)
        {
            lock (Client)
                Client.DeleteDirectory(directory.FullName);
        }
        public IList<IFileSystemStructureLinkInfo> GetLinks(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            var items = new List<IFileSystemStructureLinkInfo>();

            lock (Client)
            {
                foreach (var line in Client.GetListing(directory.FullName, FtpListOption.SizeModify))
                {
                    if (line.Type == FtpFileSystemObjectType.Link)// || IgnoredFiles.Contains(line.Name))
                        continue;

                    var item = GetInfo(PathHelper.Combine(directory.FullName, line.Name), line);
                    items.Add(item);
                }
            }

            return items;
        }

        public IFileSystemStructureLinkInfo UpdateMetadata(FileSystemStructureLink link, IFileSystemLinkMetadata metadata)
        {
            lock (Client)
            {
                if (metadata.LastWriteTime.HasValue)
                    Client.SetModifiedTime(link.FullName, metadata.LastWriteTime.Value);
            }
            return GetInfo(link.FullName);
        }

        public bool HasReadAccess(FileSystemStructureLink link) => (link.Info as LinkInfo)?.CanRead != false;
        public bool HasWriteAccess(FileSystemStructureLink link) => (link.Info as LinkInfo)?.CanWrite != false;

        private LinkInfo GetFileInfo(string fullName)
        {
            var info = GetInfo(fullName);
            return info != null && !info.IsDirectory ? info : null;
        }
        private LinkInfo GetDirectoryInfo(string fullName)
        {
            var info = GetInfo(fullName);
            return info != null && info.IsDirectory ? info : null;
        }
        private LinkInfo GetInfo(string fullName)
        {
            lock (Client)
            {
                var item = Client.GetObjectInfo(fullName, true);
                return item != null ? GetInfo(fullName, item) : null;
            }
        }
        private LinkInfo GetInfo(string fullPath, FtpListItem line)
        {
            var item = new LinkInfo(this, fullPath);
            item.IsDirectory = line.Type == FtpFileSystemObjectType.Directory;
            item.CreationTime = line.Created == DateTime.MinValue ? line.Modified : line.Created;
            item.LastWriteTime = line.Modified == DateTime.MinValue ? line.Created : line.Modified;
            item.Length = line.Size;
            item.CanRead = (line.OwnerPermissions & FtpPermission.Read) != 0;
            item.CanWrite = (line.OwnerPermissions & FtpPermission.Write) != 0;

            return item;
        }

        public void Dispose() => Client.Dispose();

        private class LinkInfo : IFileLinkInfo
        {
            private FileHash? _hash;

            private FtpFileSystemOperations Owner { get; }

            public string FullName { get; }
            public bool Exists => true;
            public bool IsDirectory { get; set; }

            public DateTime? CreationTime { get; set; }
            public DateTime? LastWriteTime { get; set; }
            public bool IsHidden => false;
            public bool IsReadOnly => false;

            public string ContentPath => FullName;
            public long Length { get; set; }
            public FileHash Hash
            {
                get
                {
                    if (!_hash.HasValue)
                        _hash = GetHash();
                    return _hash.Value;
                }
            }

            public bool CanRead { get; set; }
            public bool CanWrite { get; set; }

            public LinkInfo(FtpFileSystemOperations owner, string fullName)
            {
                Owner = owner;
                FullName = fullName;
            }


            private FileHash GetHash()
            {
                try
                {
                    if (!Owner.HashCodeNotSupported)
                    {
                        var hash = Owner.Client.GetHash(FullName);
                        if (hash != null)
                        {
                            var algorithm = GetHashAlgorithm(hash.Algorithm);
                            if (algorithm != FileHashAlgorithm.None && hash.IsValid)
                                return new FileHash(algorithm, hash.Value);
                        }
                    }
                }
                catch (FtpCommandException ex) when (ex.CompletionCode == "500")
                {
                    Owner.HashCodeNotSupported = true;
                }
                catch
                {
                    // ignored
                }

                return FileHash.Empty;
            }
            private static FileHashAlgorithm GetHashAlgorithm(FtpHashAlgorithm algorithm)
            {
                switch (algorithm)
                {
                    case FtpHashAlgorithm.SHA1: return FileHashAlgorithm.Sha1;
                    case FtpHashAlgorithm.SHA256: return FileHashAlgorithm.Sha256;
                    case FtpHashAlgorithm.SHA512: return FileHashAlgorithm.Sha512;
                    case FtpHashAlgorithm.MD5: return FileHashAlgorithm.Md5;
                    case FtpHashAlgorithm.CRC: return FileHashAlgorithm.Crc;
                    default: return FileHashAlgorithm.None;
                }
            }
        }
    }
}
