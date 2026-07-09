# TagBites.IO.Ftp

FTP/FTPS file system support for [TagBites.IO](https://github.com/TagBites/TagBites.IO), built on [FluentFTP](https://github.com/robinrodricks/FluentFTP). Browse, read, write and sync a remote FTP server through the same `FileSystem` API used for local disk and other storages.

## Install

```
dotnet add package TagBites.IO.Ftp
```

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

## License

See [https://www.tagbites.com/io](https://www.tagbites.com/io) for licensing terms.
