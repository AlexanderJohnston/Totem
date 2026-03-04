using Outermind;
using Quantum.Topics.Clients;
using Quantum.Topics.Clients.Parsers;
using Xunit;

namespace Quantum.Tests
{
  public class GenericClientPathParserTests
  {
    [Theory]
    [InlineData(@"\\cmgfx.local\san\NARA202416724\2-Frames2\Pallet 10\Box 01\Roll_1",
      "NARA", "NARA202416724", "2-Frames2", "Pallet 10", "Box 01", "Roll_1")]
    [InlineData(@"\\cmgfx.local\san\NotreDameUniv202518539\01-Originals\0-Copied and Moved to be QCd\Box 129\Folder 3",
      "NotreDameUniv", "NotreDameUniv202518539", "01-Originals", "0-Copied and Moved to be QCd", "Box 129", "Folder 3")]
    [InlineData(@"\\cmgfx.local\san\DatabankOtisApCards202518052\1-Originals\0-Verified\Level 14 Tray 1\1F7057A",
      "DatabankOtis", "DatabankOtisApCards202518052", "1-Originals", "0-Verified", "Level 14 Tray 1", "1F7057A")]
    [InlineData(@"\\cmgfx.local\san\Madison202416075\02-PhotoQC\04-QC Complete\Box 129\Folder 3",
      "Madison", "Madison202416075", "02-PhotoQC", "04-QC Complete", "Box 129", "Folder 3")]
    public void ParsesAllClientPaths(
      string path, string profileName,
      string expectedClient, string expectedProject, string expectedPallet,
      string expectedBox, string expectedRoll)
    {
      var result = GenericClientPathParser.TryParse(path, profileName);

      Assert.NotNull(result);
      Assert.Equal(expectedClient, result.Client);
      Assert.Equal(expectedProject, result.Project);
      Assert.Equal(expectedPallet, result.Pallet);
      Assert.Equal(expectedBox, result.Box);
      Assert.Equal(expectedRoll, result.Roll);
      Assert.Equal(path, result.FullPath);
    }

    [Fact]
    public void ReturnsNullForEmptyPath()
    {
      Assert.Null(GenericClientPathParser.TryParse("", "NARA"));
      Assert.Null(GenericClientPathParser.TryParse(null, "NARA"));
      Assert.Null(GenericClientPathParser.TryParse("   ", "NARA"));
    }

    [Fact]
    public void ReturnsNullForUnknownProfile()
    {
      var path = @"\\cmgfx.local\san\Unknown123\Project\Pallet\Box\Roll";
      Assert.Null(GenericClientPathParser.TryParse(path, "Unknown"));
    }

    [Fact]
    public void ReturnsNullForTooFewSegments()
    {
      var path = @"\\cmgfx.local\san\NARA202416724\Project\Pallet\Box";
      Assert.Null(GenericClientPathParser.TryParse(path, "NARA"));
    }

    [Fact]
    public void BoxIsAlwaysSecondToLast()
    {
      // Extra segments between pallet and box should still work
      var path = @"\\cmgfx.local\san\NARA202416724\2-Frames2\Pallet 10\SubFolder\Extra\Box 01\Roll_1";
      var result = GenericClientPathParser.TryParse(path, "NARA");

      Assert.NotNull(result);
      Assert.Equal("Box 01", result.Box);
      Assert.Equal("Roll_1", result.Roll);
    }

    [Fact]
    public void RollIsAlwaysLastSegment()
    {
      var path = @"\\cmgfx.local\san\DatabankOtisApCards202518052\1-Originals\0-Verified\Level 14 Tray 1\1F7057A";
      var result = GenericClientPathParser.TryParse(path, "DatabankOtis");

      Assert.NotNull(result);
      Assert.Equal("1F7057A", result.Roll);
    }
  }

  public class ClientProfileRegistryTests
  {
    [Theory]
    [InlineData(@"\\cmgfx.local\san\NARA202416724\2-Frames2\Pallet 10\Box 01\Roll_1", "NARA")]
    [InlineData(@"\\cmgfx.local\san\NotreDameUniv202518539\01-Originals\0-Copied and Moved to be QCd\Box 129\Folder 3", "NotreDameUniv")]
    [InlineData(@"\\cmgfx.local\san\DatabankOtisApCards202518052\1-Originals\0-Verified\Level 14 Tray 1\1F7057A", "DatabankOtis")]
    [InlineData(@"\\cmgfx.local\san\Madison202416075\02-PhotoQC\04-QC Complete\Box 129\Folder 3", "Madison")]
    public void MatchesCorrectProfile(string path, string expectedPrefix)
    {
      var segments = path.Split(new[] { '\\', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

      Assert.True(ClientProfileRegistry.TryMatch(path, segments, out var profile));
      Assert.Equal(expectedPrefix, profile.ClientPrefix);
    }

    [Fact]
    public void DoesNotMatchPathWithoutFinalOutputSignal()
    {
      // NARA path without "frames" keyword
      var path = @"\\cmgfx.local\san\NARA202416724\3-indexing\Pallet 10\Box 01\Roll_1";
      var segments = path.Split(new[] { '\\', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

      Assert.False(ClientProfileRegistry.TryMatch(path, segments, out _));
    }

    [Fact]
    public void DoesNotMatchUnknownClient()
    {
      var path = @"\\cmgfx.local\san\UnknownClient123\SomeFolder\Data\Box 1\Roll 1";
      var segments = path.Split(new[] { '\\', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

      Assert.False(ClientProfileRegistry.TryMatch(path, segments, out _));
    }

    [Fact]
    public void DoesNotMatchTooFewSegments()
    {
      var path = @"\\cmgfx.local\san";
      var segments = path.Split(new[] { '\\', '/' }, System.StringSplitOptions.RemoveEmptyEntries);

      Assert.False(ClientProfileRegistry.TryMatch(path, segments, out _));
    }
  }
}
