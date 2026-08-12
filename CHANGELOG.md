# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-08-23

### Changed
- Requires `TagBites.IO` 2.0.0.
- The license is Apache-2.0, previously MIT.
- Depends on `FluentFTP` 54.2.0, previously 53.0.2.

## [1.4.0] - 2026-02-10

### Added
- `FtpConnectionConfig` accepts an `ftps://` address.
- Auto passive is the default data connection type.
- The default `FluentFTP` client configuration can be supplied by the caller.
- `ListingOptions.Recursive` is handled by the provider, so the file system no longer walks the tree itself.

### Fixed
- `WriteFile` applied the wrong overwrite condition.

## [1.3.5] - 2026-02-02

### Changed
- Updated to `FluentFTP` 53.0.2.

## [1.3.4] - 2026-01-12

### Fixed
- Listing failed against servers that do not support the `MLST` command.

## [1.3.3] - 2024-09-11

### Fixed
- Corrections to the file system kind introduced in 1.3.2.

## [1.3.2] - 2024-09-11

### Changed
- The file system kind is reported through the `KnowFileSystemKind` enum instead of a string.

## [1.3.1] - 2024-03-21

### Added
- File system name and kind.

### Removed
- The `CorrectPath` method, now provided by the core library.

## [1.3.0] - 2023-06-27

### Added
- Asynchronous write operations.
- `FtpConnectionConfig` for connection settings.

## [1.2.0] - 2023-02-17

### Changed
- Updated to the current core library version.

## [1.1.0] - 2021-10-22

### Added
- First release. FTP and FTPS support for `TagBites.IO`, built on `FluentFTP`, including metadata support information.

[2.0.0]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.4.0...2.0.0
[1.4.0]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.5...1.4.0
[1.3.5]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.4...1.3.5
[1.3.4]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.3...1.3.4
[1.3.3]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.2...1.3.3
[1.3.2]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.1...1.3.2
[1.3.1]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.3.0...1.3.1
[1.3.0]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/TagBites/TagBites.IO.Ftp/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/TagBites/TagBites.IO.Ftp/releases/tag/1.1.0
