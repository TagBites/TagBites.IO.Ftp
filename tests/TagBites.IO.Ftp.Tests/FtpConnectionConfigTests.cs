using System;
using FluentFTP;
using Xunit;

namespace TagBites.IO.Ftp.Tests;

public class FtpConnectionConfigTests
{
    [Fact]
    public void Constructor_NullAddress_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FtpConnectionConfig(null!, "user", "pass"));
    }

    [Fact]
    public void Constructor_PlainHost_DefaultsToFtpSchemeAndPort21()
    {
        var config = new FtpConnectionConfig("ftp.example.com", "user", "pass");

        Assert.Equal("ftp.example.com", config.Host);
        Assert.Equal(21, config.Port);
    }

    [Fact]
    public void Constructor_HostWithExplicitPort_UsesGivenPort()
    {
        var config = new FtpConnectionConfig("ftp://ftp.example.com:2121", "user", "pass");

        Assert.Equal("ftp.example.com", config.Host);
        Assert.Equal(2121, config.Port);
    }

    [Fact]
    public void Constructor_FtpsScheme_HostParsedWithoutDoublePrefix()
    {
        var config = new FtpConnectionConfig("ftps://ftp.example.com", "user", "pass");

        Assert.Equal("ftp.example.com", config.Host);
    }

    [Fact]
    public void Constructor_TrailingBackslash_NormalizedToForwardSlashAndAccepted()
    {
        var config = new FtpConnectionConfig(@"ftp.example.com\", "user", "pass");

        Assert.Equal("ftp.example.com", config.Host);
    }

    [Fact]
    public void Constructor_BackslashSubpath_NormalizedThenRejectedAsNonRoot()
    {
        // A back-slash-separated, non-root path is rejected the same way a forward-slash one would be.
        Assert.Throws<ArgumentNullException>(() => new FtpConnectionConfig(@"ftp.example.com\subpath", "user", "pass"));
    }

    [Fact]
    public void Constructor_NonRootPathInAddress_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FtpConnectionConfig("ftp://ftp.example.com/some/path", "user", "pass"));
    }

    [Fact]
    public void Constructor_SetsUsernameAndPassword()
    {
        var config = new FtpConnectionConfig("ftp.example.com", "user", "pass");

        Assert.Equal("user", config.Credential.UserName);
        Assert.Equal("pass", config.Credential.Password);
    }

    [Fact]
    public void Constructor_SetsExpectedDefaults()
    {
        var config = new FtpConnectionConfig("ftp.example.com", "user", "pass");

        Assert.Equal(FtpEncryptionMode.Explicit, config.EncryptionMode);
        Assert.Equal(FtpDataConnectionType.AutoPassive, config.DataConnectionType);
        Assert.Equal(3, config.RetryAttempts);
    }
}
