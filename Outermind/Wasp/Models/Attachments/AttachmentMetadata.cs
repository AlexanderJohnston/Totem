namespace Quantum.Wasp.Models.Attachments;

public class AttachmentMetadata
{
    public string Guid { get; set; }
    public string FileName { get; set; }
    public string FormType { get; set; }
    public string AssociatedTag { get; set; }
    public long? FileSize { get; set; }
}
