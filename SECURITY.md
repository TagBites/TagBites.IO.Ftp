# Security Policy

## Supported versions

Security fixes are provided for the latest released version.

| Version | Supported |
|---------|-----------|
| 1.4.x   | ✅        |
| < 1.4   | ❌        |

## Reporting a vulnerability

Please **do not** open a public issue for security problems.

Report vulnerabilities privately through GitHub: **Security → [Report a vulnerability](https://github.com/TagBites/TagBites.IO.Ftp/security/advisories/new)**.

Include a description, the affected version, and a minimal program that reproduces the issue. We aim to acknowledge reports within a few business days and to release a fix or mitigation as soon as a valid issue is confirmed.

## Security model

This package is a provider for [TagBites.IO](https://github.com/TagBites/TagBites.IO). The core security model - no sandbox, paths are the only limit, advisory permissions, content buffered through the system temporary directory - is described in the [core security policy](https://github.com/TagBites/TagBites.IO/blob/master/SECURITY.md). What follows is specific to this provider.

### Transport

TLS is enabled by default: `FtpConnectionConfig` sets `EncryptionMode = FtpEncryptionMode.Explicit` for every address, including one written as `ftp://`.

**The server certificate is accepted without verification.** `FtpFileSystemOperations.ConfigureDefaultConfig` installs `client.ValidateCertificate += (_, args) => args.Accept = true;`, so an FTPS connection is encrypted but not authenticated and offers no protection against an active man-in-the-middle. Treat FTPS through this provider as protection against passive eavesdropping only, and prefer a network path you already trust.

### Credentials

The username and password are held in a `NetworkCredential` for the lifetime of the file system. Over plain `ftp://` with a server that refuses AUTH TLS, they travel in clear text.
