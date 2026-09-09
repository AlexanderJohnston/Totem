using Outermind.Microfilm;
using Xunit;

namespace Quantum.Tests
{
  public class WaspAssetTagParserTests
  {
    [Theory]
    [InlineData("202618918-1", "202618918", "1")]
    [InlineData("202214137-275.01", "202214137", "275.01")]
    [InlineData("JOB-001-1", "JOB-001", "1")]
    [InlineData("202416388 - Box 1-", "202416388", "1")]
    [InlineData("202618852-Box-1", "202618852", "1")]
    public void TryParseBox_RecognizesConfirmedBoxFormats(
      string assetId,
      string expectedJobNumber,
      string expectedBoxName)
    {
      var parsed = WaspAssetTagParser.TryParseBox(assetId, out var jobNumber, out var boxName);

      Assert.True(parsed);
      Assert.Equal(expectedJobNumber, jobNumber);
      Assert.Equal(expectedBoxName, boxName);
    }

    [Theory]
    [InlineData("202214137-Box GPN-001", "202214137", "GPN", "001")]
    [InlineData("202416288-Box S-2159-0001-0001", "202416288", "S", "2159-0001-0001")]
    [InlineData("202416388 - Box 1-1", "202416388", "1", "1")]
    [InlineData("202517233-Historian Box-1", "202517233", "Historian Box", "1")]
    [InlineData("202416552-Tray-1", "202416552", "Tray", "1")]
    [InlineData("202214137-Phase 5-1", "202214137", "Phase 5", "1")]
    [InlineData("202214137-OS-002", "202214137", "OS", "002")]
    [InlineData("202314890-Ship6-1", "202314890", "Ship6", "1")]
    [InlineData("202416288 - Film-1", "202416288", "Film", "1")]
    [InlineData("202416288 - Photo-2", "202416288", "Photo", "2")]
    public void TryParseRoll_RecognizesConfirmedRollFormats(
      string assetId,
      string expectedJobNumber,
      string expectedBoxName,
      string expectedRollName)
    {
      var parsed = WaspAssetTagParser.TryParseRoll(
        assetId,
        out var jobNumber,
        out var boxName,
        out var rollName);

      Assert.True(parsed);
      Assert.Equal(expectedJobNumber, jobNumber);
      Assert.Equal(expectedBoxName, boxName);
      Assert.Equal(expectedRollName, rollName);
    }

    [Fact]
    public void UnsupportedNamedAsset_IsNotParsed()
    {
      Assert.False(WaspAssetTagParser.TryParseBox("JOB-001-Legacy", out _, out _));
      Assert.False(WaspAssetTagParser.TryParseRoll("JOB-001-Legacy", out _, out _, out _));
    }

    [Theory]
    [InlineData("202214137-Box GPN-001")]
    [InlineData("202214137-OS-002")]
    [InlineData("202416288 - Photo-2")]
    public void RollAsset_IsNotAlsoParsedAsBox(string assetId)
    {
      Assert.False(WaspAssetTagParser.TryParseBox(assetId, out _, out _));
    }
  }
}
