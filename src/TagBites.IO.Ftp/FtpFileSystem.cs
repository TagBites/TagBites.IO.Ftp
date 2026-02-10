using FluentFTP;
using System;
using System.Text;

namespace TagBites.IO.Ftp;

/// <summary>
/// Exposes static method for creating ftp file system.
/// </summary>
public static class FtpFileSystem
{
    /// <summary>
    /// Creates a Ftp file system.
    /// </summary>
    /// <param name="address">The ftp host address.</param>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="encoding">The encoding applied to the contents of files.</param>
    /// <param name="connectionType">The Ftp Data connection type.</param>
    /// <returns>A Ftp file system contains the procedures that are used to perform file and directory operations.</returns>
    public static FileSystem Create(string address, string username, string password, Encoding encoding = null, FtpDataConnectionType connectionType = FtpDataConnectionType.AutoPassive)
    {
        var connectionConfig = new FtpConnectionConfig(address, username, password)
        {
            DataConnectionType = connectionType,
            Encoding = encoding
        };
        return new FileSystem(new FtpFileSystemOperations(connectionConfig));
    }

    /// <summary>
    /// Creates a Ftp file system.
    /// </summary>
    /// <param name="connectionConfig">FTP connection config.</param>
    /// <returns>A Ftp file system contains the procedures that are used to perform file and directory operations.</returns>
    public static FileSystem Create(FtpConnectionConfig connectionConfig)
    {
        if (connectionConfig == null)
            throw new ArgumentNullException(nameof(connectionConfig));

        return new FileSystem(new FtpFileSystemOperations(connectionConfig));
    }
}