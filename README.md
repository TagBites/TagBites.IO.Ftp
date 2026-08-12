# TagBites.IO.Ftp

[![Nuget](https://img.shields.io/nuget/v/TagBites.IO.Ftp.svg)](https://www.nuget.org/packages/TagBites.IO.Ftp/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)
[![License](https://img.shields.io/github/license/TagBites/TagBites.IO.Ftp)](https://github.com/TagBites/TagBites.IO.Ftp/blob/master/LICENSE.md)
[![Downloads](https://img.shields.io/nuget/dt/TagBites.IO.Ftp.svg)](https://www.nuget.org/packages/TagBites.IO.Ftp/)

FTP/FTPS file system support for [TagBites.IO](https://github.com/TagBites/TagBites.IO), built on [FluentFTP](https://github.com/robinrodricks/FluentFTP). Browse, read, write and sync a remote FTP server through the same `FileSystem` API used for local disk and other storages.

## Install

```
dotnet add package TagBites.IO.Ftp
```

Targets `netstandard2.0`. Depends on `FluentFTP`.

## Usage

```csharp
using var fs = FtpFileSystem.Create("ftp.example.com", "user", "password");

var file = fs.GetFile("/reports/summary.txt");
file.WriteAllText("Hello world!");

var content = file.ReadAllText(); // "Hello world!"
```

Connection options, capabilities and limits: [documentation](https://tagbites.com/io/file-systems/ftp).

## Links

- [Changelog](https://tagbites.com/io/changelog#ftp)
