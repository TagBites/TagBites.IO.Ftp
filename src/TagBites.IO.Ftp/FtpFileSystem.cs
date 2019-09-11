using System.Text;
using FluentFTP;

namespace TagBites.IO.Ftp
{
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
            return  new FileSystem(new FtpFileSystemOperations(address, username, password, encoding, connectionType));
        }
    }
}
