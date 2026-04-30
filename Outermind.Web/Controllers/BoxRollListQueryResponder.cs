using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Quantum.Queries.Clients;
using Totem;
using Totem.Timeline.Client;
using Totem.Timeline.Mvc;

namespace Outermind.Controllers
{
  static class BoxRollListQueryResponder
  {
    internal static async Task<IActionResult> Get(
      Controller controller,
      Id id,
      IQueryDb queryDb,
      JsonOptions jsonOptions)
    {
      var content = await queryDb.ReadQueryContent(
        controller.Request.Headers[HeaderNames.IfNoneMatch],
        typeof(BoxRollList),
        id);

      if(content.NotModified)
      {
        return new QueryNotModifiedResult(content.ETag);
      }

      using var data = content.ReadData();

      var query = await JsonSerializer.DeserializeAsync<BoxRollList>(data, jsonOptions.JsonSerializerOptions)
        ?? throw new InvalidOperationException($"Query content for {typeof(BoxRollList).Name} was empty.");

      controller.Response.Headers[HeaderNames.ETag] = content.ETag.ToString();

      return new JsonResult(BoxRollListResponse.From(query));
    }
  }
}
