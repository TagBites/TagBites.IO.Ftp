using FluentFTP;
using FluentFTP.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TagBites.IO.Operations;
using TagBites.IO.Streams;
using TagBites.Utils;

namespace TagBites.IO.Ftp
{
    internal class FtpFileSystemOperations :
        IFileSystemWriteOperations,
        IFileSystemAsyncWriteOperations,
        IFileSystemPermissionsOperations,
        IFileSystemMetadataSupport,
        IDisposable
    {
        private readonly FtpConnectionConfig _connectionConfig;
        private readonly AsyncLock _locker = new();

        private FtpClient _client;
        private AsyncFtpClient _asyncClient;

        private bool HashCodeNotSupported { get; set; }

        private FtpClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new FtpClient(_connectionConfig.Host, _connectionConfig.Credential, _connectionConfig.Port, _connectionConfig);
                    if (_connectionConfig.Encoding != null)
                        _client.Encoding = _connectionConfig.Encoding;
                    _client.ValidateCertificate += (control, args) => args.Accept = true;
                }

                return _client;
            }
        }
        private AsyncFtpClient AsyncClient
        {
            get
            {
                if (_asyncClient == null)
                {
                    _asyncClient = new AsyncFtpClient(_connectionConfig.Host, _connectionConfig.Credential, _connectionConfig.Port, _connectionConfig);
                    if (_connectionConfig.Encoding != null)
                        _client.Encoding = _connectionConfig.Encoding;
                    _asyncClient.ValidateCertificate += (control, args) => args.Accept = true;
                }

                return _asyncClient;
            }
        }

        public string Kind => KnowFileSystemKind.Ftp;
        public string Name => _connectionConfig.Host;

        public bool SupportsIsHiddenMetadata => false;
        public bool SupportsIsReadOnlyMetadata => false;
        public bool SupportsLastWriteTimeMetadata => true;

        public FtpFileSystemOperations(FtpConnectionConfig connectionConfig)
        {
            _connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }


        public IFileSystemStructureLinkInfo GetLinkInfo(string fullName) => GetInfo(fullName);
        public async Task<IFileSystemStructureLinkInfo> GetLinkInfoAsync(string fullName) => await GetInfoAsync(fullName).ConfigureAwait(false);

        public void ReadFile(FileLink file, Stream stream)
        {
            using (_locker.Lock())
            {
                using (var rs = PrepareClient().OpenRead(file.FullName))
                    rs.CopyTo(stream);

                PrepareClient().GetReply();
            }
        }
        public async Task ReadFileAsync(FileLink file, Stream stream)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                using var rs = await (await PrepareClientAsync().ConfigureAwait(false)).OpenRead(file.FullName).ConfigureAwait(false);
                await rs.CopyToAsync(stream).ConfigureAwait(false);

                await (await PrepareClientAsync().ConfigureAwait(false)).GetReply(default); // TODO BJ
            }
        }
        public IFileLinkInfo WriteFile(FileLink file, Stream stream, bool overwrite)
        {
            using (_locker.Lock())
            {
                if (overwrite)
                    PrepareClient().UploadStream(stream, file.FullName, FtpRemoteExists.Overwrite, true);
                else
                {
                    if (PrepareClient().UploadStream(stream, file.FullName, FtpRemoteExists.Skip, true)! != FtpStatus.Success)
                        throw new IOException("Unable to create a new file. File already exists or an unknown error occur during transfer.");
                }
            }

            return GetFileInfo(file.FullName);
        }
        public async Task<IFileLinkInfo> WriteFileAsync(FileLink file, Stream stream, bool overwrite)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                if (overwrite)
                    await (await PrepareClientAsync().ConfigureAwait(false)).UploadStream(stream, file.FullName, FtpRemoteExists.Overwrite, true).ConfigureAwait(false);
                else
                {
                    if (await (await PrepareClientAsync().ConfigureAwait(false)).UploadStream(stream, file.FullName, FtpRemoteExists.Skip, true).ConfigureAwait(false) != FtpStatus.Success)
                        throw new IOException("Unable to create a new file. File already exists or an unknown error occur during transfer.");
                }
            }

            return await GetFileInfoAsync(file.FullName).ConfigureAwait(false);
        }

        public FileAccess GetSupportedDirectAccess(FileLink file) => FileAccess.Read;
        public Stream OpenFileStream(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            using (_locker.Lock())
            {
                var stream = PrepareClient().OpenRead(file.FullName);

                return new NotifyOnCloseStream(stream, () =>
                {
                    try
                    {
                        PrepareClient().GetReply();
                    }
                    finally
                    {
                        //_semaphore.Release();
                    }
                });
            }
        }
        public async Task<Stream> OpenFileStreamAsync(FileLink file, FileAccess access, bool overwrite)
        {
            if (access != FileAccess.Read)
                throw new NotSupportedException();

            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                var stream = await (await PrepareClientAsync().ConfigureAwait(false)).OpenRead(file.FullName).ConfigureAwait(false);

                return new NotifyOnCloseStream(stream, () =>
                {
                    try
                    {
                        //(await PrepareClientAsync().ConfigureAwait(false)).GetReply();
                    }
                    finally
                    {
                        //_semaphoreSlim.Release();
                    }
                });
            }
        }

        public IFileLinkInfo MoveFile(FileLink source, FileLink destination, bool overwrite)
        {
            using (_locker.Lock())
            {
                if (overwrite)
                    PrepareClient().MoveFile(source.FullName, destination.FullName);
                else
                {
                    if (!PrepareClient().MoveFile(source.FullName, destination.FullName, FtpRemoteExists.Skip))
                        throw new IOException("Unable to move a new file. File already exists or an unknown error occur during transfer.");
                }
            }

            return GetFileInfo(destination.FullName);
        }
        public async Task<IFileLinkInfo> MoveFileAsync(FileLink source, FileLink destination, bool overwrite)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                if (overwrite)
                    await (await PrepareClientAsync().ConfigureAwait(false)).MoveFile(source.FullName, destination.FullName).ConfigureAwait(false);
                else
                {
                    if (!await (await PrepareClientAsync().ConfigureAwait(false)).MoveFile(source.FullName, destination.FullName, FtpRemoteExists.Skip).ConfigureAwait(false))
                        throw new IOException("Unable to move a new file. File already exists or an unknown error occur during transfer.");
                }
            }

            return await GetFileInfoAsync(destination.FullName);
        }

        public void DeleteFile(FileLink file)
        {
            using (_locker.Lock())
            {
                PrepareClient().DeleteFile(file.FullName);
            }
        }
        public async Task DeleteFileAsync(FileLink file)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                await (await PrepareClientAsync().ConfigureAwait(false)).DeleteFile(file.FullName).ConfigureAwait(false);
            }
        }

        public IFileSystemStructureLinkInfo CreateDirectory(DirectoryLink directory)
        {
            using (_locker.Lock())
            {
                PrepareClient().CreateDirectory(directory.FullName);
            }

            return GetDirectoryInfo(directory.FullName);
        }
        public async Task<IFileSystemStructureLinkInfo> CreateDirectoryAsync(DirectoryLink directory)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                await (await PrepareClientAsync().ConfigureAwait(false)).CreateDirectory(directory.FullName).ConfigureAwait(false);
            }

            return await GetDirectoryInfoAsync(directory.FullName).ConfigureAwait(false);
        }

        public IFileSystemStructureLinkInfo MoveDirectory(DirectoryLink source, DirectoryLink destination)
        {
            using (_locker.Lock())
            {
                PrepareClient().MoveDirectory(source.FullName, destination.FullName);
            }

            return GetDirectoryInfo(destination.FullName);
        }
        public async Task<IFileSystemStructureLinkInfo> MoveDirectoryAsync(DirectoryLink source, DirectoryLink destination)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                await (await PrepareClientAsync().ConfigureAwait(false)).MoveDirectory(source.FullName, destination.FullName).ConfigureAwait(false);
            }

            return await GetDirectoryInfoAsync(destination.FullName).ConfigureAwait(false);
        }

        public void DeleteDirectory(DirectoryLink directory, bool recursive)
        {
            using (_locker.Lock())
            {
                if (!recursive && PrepareClient().GetListing(directory.FullName, FtpListOption.SizeModify).Length > 0)
                    throw new IOException($"The directory is not empty: {directory.FullName}");

                PrepareClient().DeleteDirectory(directory.FullName);
            }
        }
        public async Task DeleteDirectoryAsync(DirectoryLink directory, bool recursive)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                if (!recursive && (await (await PrepareClientAsync().ConfigureAwait(false)).GetListing(directory.FullName, FtpListOption.SizeModify).ConfigureAwait(false)).Length > 0)
                    throw new IOException($"The directory is not empty: {directory.FullName}");

                await (await PrepareClientAsync().ConfigureAwait(false)).DeleteDirectory(directory.FullName).ConfigureAwait(false);
            }
        }

        public IList<IFileSystemStructureLinkInfo> GetLinks(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            var items = new List<IFileSystemStructureLinkInfo>();

            using (_locker.Lock())
            {
                foreach (var line in PrepareClient().GetListing(directory.FullName, FtpListOption.SizeModify))
                {
                    if (line.Type == FtpObjectType.Link)// || IgnoredFiles.Contains(line.Name))
                        continue;

                    var item = GetInfo(PathHelper.Combine(directory.FullName, line.Name), line);
                    items.Add(item);
                }
            }

            return items;
        }
        public async Task<IList<IFileSystemStructureLinkInfo>> GetLinksAsync(DirectoryLink directory, FileSystem.ListingOptions options)
        {
            var items = new List<IFileSystemStructureLinkInfo>();

            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                foreach (var line in await (await PrepareClientAsync().ConfigureAwait(false)).GetListing(directory.FullName, FtpListOption.SizeModify).ConfigureAwait(false))
                {
                    if (line.Type == FtpObjectType.Link)// || IgnoredFiles.Contains(line.Name))
                        continue;

                    var item = GetInfo(PathHelper.Combine(directory.FullName, line.Name), line);
                    items.Add(item);
                }
            }

            return items;
        }

        public IFileSystemStructureLinkInfo UpdateMetadata(FileSystemStructureLink link, IFileSystemLinkMetadata metadata)
        {
            using (_locker.Lock())
            {
                if (metadata.LastWriteTime.HasValue)
                    PrepareClient().SetModifiedTime(link.FullName, metadata.LastWriteTime.Value);
            }

            return GetInfo(link.FullName);
        }
        public async Task<IFileSystemStructureLinkInfo> UpdateMetadataAsync(FileSystemStructureLink link, IFileSystemLinkMetadata metadata)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                if (metadata.LastWriteTime.HasValue)
                    await (await PrepareClientAsync().ConfigureAwait(false)).SetModifiedTime(link.FullName, metadata.LastWriteTime.Value).ConfigureAwait(false);
            }
            return await GetInfoAsync(link.FullName).ConfigureAwait(false);
        }

        public bool HasReadAccess(FileSystemStructureLink link) => (link.Info as LinkInfo)?.CanRead != false;
        public bool HasWriteAccess(FileSystemStructureLink link) => (link.Info as LinkInfo)?.CanWrite != false;

        private LinkInfo GetFileInfo(string fullName)
        {
            var info = GetInfo(fullName);
            return info is { IsDirectory: false } ? info : null;
        }
        private async Task<LinkInfo> GetFileInfoAsync(string fullName)
        {
            var info = await GetInfoAsync(fullName).ConfigureAwait(false);
            return info is { IsDirectory: false } ? info : null;
        }
        private LinkInfo GetDirectoryInfo(string fullName)
        {
            var info = GetInfo(fullName);
            return info is { IsDirectory: true } ? info : null;
        }
        private async Task<LinkInfo> GetDirectoryInfoAsync(string fullName)
        {
            var info = await GetInfoAsync(fullName).ConfigureAwait(false);
            return info is { IsDirectory: true } ? info : null;
        }
        private LinkInfo GetInfo(string fullName)
        {
            using (_locker.Lock())
            {
                var client = PrepareClient();
                if (!client.IsConnected)
                    client.AutoConnect();

                try
                {
                    if (fullName == "/")
                    {
                        // BUG WORKAROUND: IF file system doesn't support MLST command there is problem with root directory
                        if (!client.HasFeature(FtpCapability.MLSD))
                            return GetInfo(fullName, new FtpListItem("/", 0, FtpObjectType.Directory, DateTime.MinValue));
                    }

                    var item = client.GetObjectInfo(fullName, true);
                    return item != null ? GetInfo(fullName, item) : null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
        private async Task<LinkInfo> GetInfoAsync(string fullName)
        {
            using (await _locker.LockAsync().ConfigureAwait(false))
            {
                try
                {
                    var client = await PrepareClientAsync().ConfigureAwait(false);
                    if (fullName == "/")
                    {
                        // BUG WORKAROUND: IF file system doesn't support MLST command there is problem with root directory
                        if (!client.HasFeature(FtpCapability.MLSD))
                            return GetInfo(fullName, new FtpListItem("/", 0, FtpObjectType.Directory, DateTime.MinValue));
                    }

                    var item = await client.GetObjectInfo(fullName, true).ConfigureAwait(false);
                    return item != null ? GetInfo(fullName, item) : null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
        private LinkInfo GetInfo(string fullPath, FtpListItem line)
        {
            var item = new LinkInfo(this, fullPath, line.Type == FtpObjectType.Directory)
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

        public void Dispose()
        {
            Client?.Dispose();
            AsyncClient?.Dispose();
        }

        private FtpClient PrepareClient()
        {
            if (!Client.IsConnected)
                Client.AutoConnect();

            return Client;
        }
        private async Task<AsyncFtpClient> PrepareClientAsync()
        {
            if (!AsyncClient.IsConnected)
                await AsyncClient.Connect().ConfigureAwait(false); ;

            return AsyncClient;
        }

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
                        var hash = Owner.PrepareClient().GetChecksum(FullName);
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
