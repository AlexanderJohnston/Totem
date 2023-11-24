namespace Totem.Reports.Queries;

public interface IReportQueryETagEncryption
{
    string Encrypt(string etag);
    string Decrypt(string encryptedETag);
}
