# TagBites.IO.Ftp

[![Nuget](https://img.shields.io/nuget/v/TagBites.IO.Ftp.svg)](https://www.nuget.org/packages/TagBites.IO.Ftp/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)
[![License](https://img.shields.io/github/license/TagBites/TagBites.IO.Ftp)](https://github.com/TagBites/TagBites.IO.Ftp/blob/master/LICENSE.md)

FTP/FTPS file system support for [TagBites.IO](https://github.com/TagBites/TagBites.IO), built on [FluentFTP](https://github.com/robinrodricks/FluentFTP). Browse, read, write and sync a remote FTP server through the same `FileSystem` API used for local disk and other storages.

## Install

```
dotnet add package TagBites.IO.Ftp
```

Targets `netstandard2.0`. Depends on `FluentFTP`.

## Usage

```csharp
using TagBites.IO.Ftp;

var fs = FtpFileSystem.Create("ftp.example.com", "user", "password");

var file = fs.GetFile("/reports/summary.txt");
file.WriteAllText("Hello world!");

var content = file.ReadAllText();
```

For full control over the connection (encryption, data connection type, encoding, timeouts, ...) pass a `FtpConnectionConfig` instead:

```csharp
using FluentFTP;
using TagBites.IO.Ftp;

var config = new FtpConnectionConfig("ftp.example.com", "user", "password")
{
    DataConnectionType = FtpDataConnectionType.PASV
};
var fs = FtpFileSystem.Create(config);
```

## Capabilities

- Synchronous and asynchronous operations.
- Reports permissions from the server, so `CanRead` and `CanWrite` reflect the remote account.
- Metadata: last write time only. Hidden and read-only flags are not exposed by FTP.
- TLS is on by default, but the server certificate is **not verified** - see [SECURITY.md](SECURITY.md).

## Links

- [Changelog](https://github.com/TagBites/TagBites.IO.Ftp/blob/master/CHANGELOG.md)
- [Security policy](https://github.com/TagBites/TagBites.IO.Ftp/blob/master/SECURITY.md)
- [License (MIT)](https://github.com/TagBites/TagBites.IO.Ftp/blob/master/LICENSE.md)
