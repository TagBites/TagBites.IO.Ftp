using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TagBites.IO.Operations;
using TagBites.IO.Streams;

namespace TagBites.IO.Ftp
{
    internal class FtpFileSystemOperations :
        IFileSystemWriteOperations,
        IFileSystemDirectReadWriteOperations,
        IFileSystemPermissionsOperations,
        IFileSystemMetadataSupport,
        IDisposable
    {
        private readonly FtpConnectionConfig _connectionConfig;
        private bool HashCodeNotSupported { get; set; }

        private FtpClient _client;
        private FtpClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new FtpClient(_connectionConfig.Host, _connectionConfig.Port, _connectionConfig.Credential);
                    if (_connectionConfig.Encoding != null)
                        _client.Encoding = _connectionConfig.Encoding;
                    _client.ValidateCertificate += (control, args) => args.Accept = true;
                }

                return _client;
            }
        }

        public bool SupportsIsHiddenMetadata => false;
        public bool SupportsIsReadOnlyMetadata => false;
        public bool SupportsLastWriteTimeMetadata => true;

        public FtpFileSystemOperations(FtpConnectionConfig connectionConfig)
        {
            _connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName) => GetInfo(fullName);
        public string CorrectPath(string path) => path;

        public void ReadFile(FileLink file, Stream stream)
        {
            lock (Client)
            {
                using (var rs = Client.OpenRead(file.FullName))
                    rs.CopyTo(stream);

                Client.GetReply();
            }
        }
        public IFileLinkInfo WriteFile(FileLink file, Stream stream, bool overwrite)
        {
            lock (Client)
            {
                if (overwrite)
                    Client.Upload(stream, file.FullName, FtpExists.Overwrite, true);
                else
                {
                    if (!Client.Upload(stream, file.FullName, FtpExists.Skip, true))
                        throw new IOException("Unable to create a new file. File already exists or an unknown error occur during transfer.");
                }
            }

            return GetFileInfo(file.FullName);
        }

        public FileAccess GetSupportedDirectAccess(FileLink file) => FileAccess.Read;
        public Stream OpenFileStream(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            Monitor.Enter(Client);
            try
            {
                var stream = Client.OpenRead(file.FullName);

                return new NotifyOnCloseStream(stream, () =>
                {
                    try
                    {
                        Client.GetReply();
                    }
                    finally
                    {
                        Monitor.Exit(Client);
                    }
                });
            }
            catch
            {
                Monitor.Exit(Client);
                throw;
            }
        }

        public IFileLinkInfo MoveFile(FileLink source, FileLink destination, bool overwrite)
        {
            lock (Client)
            {
                if (overwrite)
                    Client.MoveFile(source.FullName, destination.FullName);
                else
                {
                    if (!Client.MoveFile(source.FullName, destination.FullName, FtpExists.Skip))
                        throw new IOException("Unable to move a new file. File already exists or an unknown error occur during transfer.");
                }
            }

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
            {
                if (!recursive && Client.GetListing(directory.FullName, FtpListOption.SizeModify).Length > 0)
                    throw new IOException($"The directory is not empty: {directory.FullName}");

                Client.DeleteDirectory(directory.FullName);
            }
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
            return info is { IsDirectory: false } ? info : null;
        }
        private LinkInfo GetDirectoryInfo(string fullName)
        {
            var info = GetInfo(fullName);
            return info is { IsDirectory: true } ? info : null;
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
            var item = new LinkInfo(this, fullPath, line.Type == FtpFileSystemObjectType.Directory)
            {
                CreationTime = line.Created == DateTime.MinValue ? CheckDateTime(line.Modified) : line.Created,
                LastWriteTime = line.Modified == DateTime.MinValue ? CheckDateTime(line.Created) : line.Modified,
                Length = line.Size,
                CanRead = line.OwnerPermissions == 0 || (line.OwnerPermissions & FtpPermission.Read) != 0,
                CanWrite = line.OwnerPermissions == 0 || (line.OwnerPermissions & FtpPermission.Write) != 0
            };

            return item;
        }
        private DateTime? CheckDateTime(DateTime dateTime)
        {
            return dateTime == DateTime.MinValue ? (DateTime?)null : dateTime;
        }

        public void Dispose() => Client.Dispose();

        private class LinkInfo : IFileLinkInfo
        {
            private FileHash? _hash;

            private FtpFileSystemOperations Owner { get; }

            public string FullName { get; }
            public bool Exists => true;
            public bool? IsDirectory { get; }

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

            public LinkInfo(FtpFileSystemOperations owner, string fullName, bool isDirectory)
            {
                Owner = owner;
                FullName = fullName;
                IsDirectory = isDirectory;
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
