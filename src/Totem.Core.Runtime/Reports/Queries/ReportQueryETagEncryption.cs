namespace Totem.Reports.Queries;

public sealed class ReportQueryETagEncryption : IReportQueryETagEncryption
{
    // TODO

    public string Encrypt(string etag) => etag;
    public string Decrypt(string encryptedETag) => encryptedETag;
}
