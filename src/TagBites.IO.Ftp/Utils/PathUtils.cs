namespace TagBites.IO.Ftp.Utils;

internal static class PathUtils
{
    public static string Combine(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path2))
            return path1;

        if (string.IsNullOrEmpty(path1))
            return path2;

        var ch = path1[path1.Length - 1];

        if (ch != Path.DirectorySeparatorChar && ch != Path.AltDirectorySeparatorChar && ch != Path.VolumeSeparatorChar)
            return path1 + "/" + path2;

        return path1 + path2;
    }
}
