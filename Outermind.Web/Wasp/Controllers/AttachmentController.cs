using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Attachments;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Controllers;

public class AttachmentController : WaspHttpClient
{
    public AttachmentController(HttpClient http) : base(http) { }

    public Task<WaspResult<List<WtResult>>> UploadForFormsAsync(object attachmentData) =>
        PostAsync<List<WtResult>>("public-api/attachments/uplodAttachmentsForForms", attachmentData);

    public Task<WaspResult<List<WtResult>>> UploadBatchForFormsAsync(object attachmentData) =>
        PostAsync<List<WtResult>>("public-api/attachments/uplodBatchAttachmentsForForms", attachmentData);

    public Task<Stream> DownloadAsync(string guid) =>
        PostDownloadAsync($"public-api/attachments/download/{guid}");

    public Task<WaspResult<List<AttachmentMetadata>>> GetMetadataByTagAsync(object request) =>
        PostAsync<List<AttachmentMetadata>>("public-api/attachments/getAttachmentmetadataByAssociatedTag", request);

    public Task<WaspResult<List<AttachmentMetadata>>> GetByTagsListAsync(object request) =>
        PostAsync<List<AttachmentMetadata>>("public-api/attachments/getAttachmentsByAssociatedTagsList", request);
}
