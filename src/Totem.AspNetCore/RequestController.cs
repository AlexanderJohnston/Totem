using System.Collections.Specialized;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Totem
{
    public abstract class RequestController : ControllerBase
    {
        protected IActionResult Respond(HttpStatusCode code, NameValueCollection headers, object? content)
        {
            Response.StatusCode = (int) code;

            foreach(var header in headers.AllKeys)
            {
                if(header != null)
                {
                    Response.Headers[header] = headers[header];
                }
            }

            return content switch
            {
                StreamContent streamContent => File(streamContent.Stream, streamContent.ContentType, streamContent.ContentName),
                Stream stream => File(stream, StreamContent.DefaultContentType, StreamContent.DefaultContentName),
                object value => Ok(value),
                _ => Ok(),
            };
        }
    }
}