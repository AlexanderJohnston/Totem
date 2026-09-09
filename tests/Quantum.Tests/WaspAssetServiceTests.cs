using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Outermind.Microfilm;
using Outermind.Service;
using Quantum.Wasp.Models.Assets;
using Quantum.Wasp.Models.Common;
using Xunit;

namespace Quantum.Tests
{
  public class WaspAssetServiceTests
  {
    [Fact]
    public void AddWaspAssetService_ResolvesTypedClient()
    {
      var services = new ServiceCollection();
      services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
          ["Wasp:BaseUrl"] = "https://example.test",
          ["Wasp:Token"] = "test-token"
        })
        .Build());

      services.AddWaspAssetService();

      using var provider = services.BuildServiceProvider();

      var service = provider.GetRequiredService<IWaspAssetService>();

      Assert.IsType<WaspAssetService>(service);
    }

    [Fact]
    public async Task GetClientBatchAsync_FetchesEachKnownJobSeparatelyAndPagesWithinJob()
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
        },
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "JOB-002-Box-1" }
          },
          TotalRecordsLongCount = 1
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

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var batch = await service.GetClientBatchAsync(0, new[] { " job-002 ", "JOB-001", "job-001", " " });

      Assert.NotNull(batch);
      Assert.Equal("JOB-001", batch.JobNumber);
      Assert.Equal(new[] { "JOB-001-Box 1-APP-41", "JOB-001-Box-1", "JOB-001-Box-2" }, batch.AssetIds);
      Assert.Collection(requests,
        request => AssertRequest(request, 1, null, "JOB-001"),
        request => AssertRequest(request, 2, 501, "JOB-001"),
        request => AssertRequest(request, 1, null, "job-002"));

      static void AssertRequest(
        AdvancedSearchParameters request,
        int expectedPageNumber,
        long? expectedTotalCount,
        string expectedJobNumber)
      {
        Assert.Equal(500, request.PageSize);
        Assert.Equal(expectedPageNumber, request.PageNumber);
        Assert.Equal(expectedTotalCount, request.TotalCountFromPriorFetch);
        Assert.True(request.IgnoreAttachments);
        Assert.True(request.IgnoreGeoLocation);
        Assert.NotNull(request.Filter);
        Assert.Equal("and", request.Filter.Logic);

        var filter = Assert.Single(request.Filter.Filters);
        Assert.Equal("AssetTag", filter.Field);
        Assert.Equal("startswith", filter.Operator);
        Assert.Equal(expectedJobNumber, filter.Value?.ToString());
      }
    }

    [Fact]
    public async Task GetClientBatchAsync_ReturnsOnlyKnownClientBatches()
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

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var firstClient = await service.GetClientBatchAsync(0, new[] { "JOB-001", "JOB-002" });
      var secondClient = await service.GetClientBatchAsync(1, new[] { "JOB-001", "JOB-002" });
      var completed = await service.GetClientBatchAsync(2, new[] { "JOB-001", "JOB-002" });

      Assert.Equal("JOB-001", firstClient.JobNumber);
      Assert.Equal(new[] { "JOB-001-Box 1-APP-41", "JOB-001-Box-1" }, firstClient.AssetIds);

      Assert.Equal("JOB-002", secondClient.JobNumber);
      Assert.Equal(new[] { "JOB-002-Box-2" }, secondClient.AssetIds);
      Assert.Null(completed);
    }

    [Fact]
    public async Task GetClientBatchAsync_WithNoKnownJobNumbers_DoesNotCallWasp()
    {
      var requestCount = 0;
      using var client = new HttpClient(new StubHttpMessageHandler(_ =>
      {
        requestCount++;
        return Task.FromResult(CreateJsonResponse(new WaspResult<List<AssetInfo>>()));
      }))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var batch = await service.GetClientBatchAsync(0, Array.Empty<string>());

      Assert.Null(batch);
      Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task GetClientBatchAsync_ReusesCachedSnapshotAcrossServiceInstances()
    {
      var requestCount = 0;

      using var client = new HttpClient(new StubHttpMessageHandler(_ =>
      {
        requestCount++;

        return Task.FromResult(CreateJsonResponse(
          new WaspResult<List<AssetInfo>>
          {
            Data = new List<AssetInfo>
            {
              new() { AssetTag = "JOB-001-Box-1" },
              new() { AssetTag = "JOB-002-Box-2" }
            },
            TotalRecordsLongCount = 2
          }));
      }))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      using var cache = new MemoryCache(new MemoryCacheOptions());

      var firstService = new WaspAssetService(client, cache);
      var secondService = new WaspAssetService(client, cache);

      var firstBatch = await firstService.GetClientBatchAsync(0, new[] { "JOB-001", "JOB-002" });
      var secondBatch = await secondService.GetClientBatchAsync(1, new[] { "JOB-001", "JOB-002" });

      Assert.Equal("JOB-001", firstBatch.JobNumber);
      Assert.Equal("JOB-002", secondBatch.JobNumber);
      Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task GetClientBatchAsync_RefreshesSnapshotWhenNewRunStarts()
    {
      var responses = new Queue<WaspResult<List<AssetInfo>>>(new[]
      {
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "JOB-001-Box-1" }
          },
          TotalRecordsLongCount = 1
        },
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>(),
          TotalRecordsLongCount = 0
        },
        new WaspResult<List<AssetInfo>>
        {
          Data = new List<AssetInfo>
          {
            new() { AssetTag = "JOB-002-Box-2" }
          },
          TotalRecordsLongCount = 1
        }
      });

      using var client = new HttpClient(new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(responses.Dequeue()))))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var firstRun = await service.GetClientBatchAsync(0, new[] { "JOB-001" });
      var secondRun = await service.GetClientBatchAsync(0, new[] { "JOB-001", "JOB-002" });

      Assert.Equal("JOB-001", firstRun.JobNumber);
      Assert.Equal("JOB-002", secondRun.JobNumber);
    }

    [Fact]
    public async Task GetClientBatchAsync_ReusesSnapshotForLaterClientPositionsInSameRun()
    {
      var requestCount = 0;

      using var client = new HttpClient(new StubHttpMessageHandler(_ =>
      {
        requestCount++;

        return Task.FromResult(CreateJsonResponse(
          new WaspResult<List<AssetInfo>>
          {
            Data = new List<AssetInfo>
            {
              new() { AssetTag = "JOB-001-Box-1" },
              new() { AssetTag = "JOB-002-Box-2" }
            },
            TotalRecordsLongCount = 2
          }));
      }))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var firstBatch = await service.GetClientBatchAsync(0, new[] { "JOB-001", "JOB-002" });
      var secondBatch = await service.GetClientBatchAsync(1, new[] { "JOB-001", "JOB-002" });

      Assert.Equal("JOB-001", firstBatch.JobNumber);
      Assert.Equal("JOB-002", secondBatch.JobNumber);
      Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task GetClientBatchAsync_DoesNotCacheFailedFetches()
    {
      var attempts = 0;

      using var client = new HttpClient(new StubHttpMessageHandler(_ =>
      {
        attempts++;

        if (attempts == 1)
        {
          return Task.FromResult(CreateJsonResponse(
            new WaspResult<List<AssetInfo>>
            {
              HasError = true,
              Messages = new List<WtResult>
              {
                new() { Message = "Invalid search." }
              }
            }));
        }

        return Task.FromResult(CreateJsonResponse(
          new WaspResult<List<AssetInfo>>
          {
            Data = new List<AssetInfo>
            {
              new() { AssetTag = "JOB-001-Box-1" }
            },
            TotalRecordsLongCount = 1
          }));
      }))
      {
        BaseAddress = new Uri("https://example.test/")
      };

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetClientBatchAsync(0, new[] { "JOB-001" }));
      var batch = await service.GetClientBatchAsync(0, new[] { "JOB-001" });

      Assert.Equal("Invalid search.", ex.Message);
      Assert.Equal("JOB-001", batch.JobNumber);
      Assert.Equal(2, attempts);
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

      using var cache = new MemoryCache(new MemoryCacheOptions());
      var service = new WaspAssetService(client, cache);

      var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetClientBatchAsync(0, new[] { "JOB-001" }));

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
