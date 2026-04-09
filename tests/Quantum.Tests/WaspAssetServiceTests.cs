using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Outermind.Service;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Xunit;

namespace Quantum.Tests
{
  public class WaspAssetServiceTests
  {
    [Fact]
    public async Task GetClientBatchAsync_FetchesAdditionalPagesWhenTotalCountExceedsCurrentPageWindow()
    {
      var requests = new List<AdvancedSearchParameters>();
      var responses = new Queue<WaspResult<List<AssetInfo>>>(new[]
      {
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "JOB-001-Box-1" },
            new() { AssetTag = "JOB-001-Box 1-APP-41" }
          },
          TotalRecordsLongCount = 501
        },
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "JOB-001-Box-2" }
          },
          TotalRecordsLongCount = 501
        }
      });

      using var client = new HttpClient(new StubHttpMessageHandler(async request =>
      {
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.test/public-api/assets/assetadvancedinfosearch", request.RequestUri?.ToString());

        var body = await request.Content!.ReadFromJsonAsync<AdvancedSearchParameters>();
        requests.Add(body!);

        return CreateJsonResponse(responses.Dequeue());
      }))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      var service = new WaspAssetService(client);

      var batch = await service.GetClientBatchAsync(0);

      Assert.NotNull(batch);
      Assert.Equal("JOB-001", batch.JobNumber);
      Assert.Equal(new[] { "JOB-001-Box 1-APP-41", "JOB-001-Box-1", "JOB-001-Box-2" }, batch.AssetIds);
      Assert.Collection(requests,
        first =>
        {
          Assert.Equal(500, first.PageSize);
          Assert.Equal(1, first.PageNumber);
          Assert.Null(first.TotalCountFromPriorFetch);
          Assert.True(first.IgnoreAttachments);
          Assert.True(first.IgnoreGeoLocation);
        },
        second =>
        {
          Assert.Equal(500, second.PageSize);
          Assert.Equal(2, second.PageNumber);
          Assert.Equal(501, second.TotalCountFromPriorFetch);
          Assert.True(second.IgnoreAttachments);
          Assert.True(second.IgnoreGeoLocation);
        });
    }

    [Fact]
    public async Task GetClientBatchAsync_ReturnsUnknownPrefixBatchBeforeClientBatches()
    {
      using var client = new HttpClient(new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "LEGACY-ASSET" },
            new() { AssetTag = "JOB-002-Box-2" },
            new() { AssetTag = "JOB-001-Box-1" },
            new() { AssetTag = "JOB-001-Box 1-APP-41" }
          },
          TotalRecordsLongCount = 4
        }))))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      var service = new WaspAssetService(client);

      var unassigned = await service.GetClientBatchAsync(0);
      var firstClient = await service.GetClientBatchAsync(1);
      var secondClient = await service.GetClientBatchAsync(2);

      Assert.Null(unassigned.JobNumber);
      Assert.Equal(new[] { "LEGACY-ASSET" }, unassigned.AssetIds);

      Assert.Equal("JOB-001", firstClient.JobNumber);
      Assert.Equal(new[] { "JOB-001-Box 1-APP-41", "JOB-001-Box-1" }, firstClient.AssetIds);

      Assert.Equal("JOB-002", secondClient.JobNumber);
      Assert.Equal(new[] { "JOB-002-Box-2" }, secondClient.AssetIds);
    }

    [Fact]
    public async Task GetClientBatchAsync_ThrowsWhenWaspReturnsApplicationError()
    {
      using var client = new HttpClient(new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(
        new WaspResult<List<AssetInfo>>
        {
          HasError = true,
          Messages = new List<WtResult>
          {
            new() { Message = "Invalid search." }
          }
        }))))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      var service = new WaspAssetService(client);

      var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetClientBatchAsync(0));

      Assert.Equal("Invalid search.", ex.Message);
    }

    static HttpResponseMessage CreateJsonResponse<T>(T value) =>
      new(HttpStatusCode.OK)
      {
        Content = JsonContent.Create(value)
      };

    sealed class StubHttpMessageHandler : HttpMessageHandler
    {
      readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendAsync;

      public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
      {
        _sendAsync = sendAsync;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        _sendAsync(request);
    }
  }
}
