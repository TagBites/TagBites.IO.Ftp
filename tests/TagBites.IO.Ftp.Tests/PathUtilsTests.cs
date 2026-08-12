using TagBites.IO.Ftp.Utils;
using Xunit;

namespace TagBites.IO.Ftp.Tests;

public class PathUtilsTests
{
    [Theory]
    [InlineData("a", "b", "a/b")]
    [InlineData("a/", "b", "a/b")]
    [InlineData("a", "", "a")]
    [InlineData("", "b", "b")]
    public void Combine_VariousInputs_CorrectResult(string path1, string path2, string expected)
    {
        Assert.Equal(expected, PathUtils.Combine(path1, path2));
    }
}
