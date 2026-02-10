#nullable enable

using FluentFTP;
using System;
using System.Net;
using System.Text;

namespace TagBites.IO.Ftp;
/// <summary>
/// Provides the configuration settings for a single FTP client.
/// </summary>
public class FtpConnectionConfig : FtpConfig
{
    internal string Host { get; }
    internal int Port { get; }
    internal NetworkCredential Credential { get; }
    /// <summary>
    /// Gets or sets the text encoding being used when talking with the server.
    /// </summary>
    public Encoding? Encoding { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpConnectionConfig"/> class.
    /// </summary>
    /// <param name="address">The ftp host address.</param>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FtpConnectionConfig(string address, string username, string password)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        address = address.Replace("\\", "/");
        if (!address.StartsWith("ftp://", StringComparison.CurrentCultureIgnoreCase) &&
            !address.StartsWith("ftps://", StringComparison.CurrentCultureIgnoreCase))
        {
            address = "ftp://" + address;
        }


        var url = new Uri(address);
        var host = url.Host;
        var rootDirectory = url.AbsolutePath;
        var port = url.Port > 0 ? url.Port : 21;

        if (!string.IsNullOrEmpty(rootDirectory) && rootDirectory != "/")
            throw new ArgumentNullException(nameof(address));

        Host = host;
        Port = port;
        Credential = new NetworkCredential(username, password);

        DataConnectionType = FtpDataConnectionType.AutoActive;
        EncryptionMode = FtpEncryptionMode.Explicit;
        RetryAttempts = 3;
    }
}
