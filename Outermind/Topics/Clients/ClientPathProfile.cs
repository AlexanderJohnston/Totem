namespace Quantum.Topics.Clients
{
  /// <summary>
  /// Defines how to identify and parse paths for a specific client.
  /// Client = segments[2], Box = segments[^2], Roll = segments[^1] are universal.
  /// Only PalletSegmentIndex varies per client.
  /// </summary>
  public class ClientPathProfile
  {
    public string ClientPrefix { get; }
    public string FinalOutputSignal { get; }
    public int MinSegments { get; }
    public int PalletSegmentIndex { get; }

    public ClientPathProfile(
      string clientPrefix,
      string finalOutputSignal,
      int palletSegmentIndex = 4,
      int minSegments = 7)
    {
      ClientPrefix = clientPrefix;
      FinalOutputSignal = finalOutputSignal;
      PalletSegmentIndex = palletSegmentIndex;
      MinSegments = minSegments;
    }
  }
}
