using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp;

public class WaspHttpClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WaspHttpClient(HttpClient http)
    {
        _http = http;
    }

    protected async Task<WaspResult<T>> PostAsync<T>(string endpoint, object body)
    {
        var response = await _http.PostAsJsonAsync(endpoint, body, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WaspResult<T>>(JsonOptions);
    }

    protected async Task<WaspResult<T>> GetAsync<T>(string endpoint)
    {
        var response = await _http.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WaspResult<T>>(JsonOptions);
    }

    protected async Task<Stream> PostStreamAsync(string endpoint, object body)
    {
        var response = await _http.PostAsJsonAsync(endpoint, body, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    protected async Task<Stream> PostDownloadAsync(string endpoint)
    {
        var response = await _http.PostAsync(endpoint, null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    protected static SearchPatternRequest CreateSearchPatternRequest(string searchText) =>
        new()
        {
            SearchPattern = searchText ?? string.Empty
        };

    /// <summary>
    /// Splits a list into batches of the specified size (default 500) and executes the operation on each batch,
    /// combining results into a single WaspResult.
    /// </summary>
    protected async Task<WaspResult<List<TResult>>> BatchAsync<TItem, TResult>(
        IReadOnlyList<TItem> items,
        Func<IReadOnlyList<TItem>, Task<WaspResult<List<TResult>>>> operation,
        int batchSize = 500)
    {
        if (items.Count <= batchSize)
            return await operation(items);

        var combined = new WaspResult<List<TResult>>
        {
            Data = new List<TResult>()
        };

        for (var i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var result = await operation(batch);

            if (result.Data != null)
                combined.Data.AddRange(result.Data);

            if (result.HasError)
            {
                combined.HasError = true;
                combined.Messages.AddRange(result.Messages);
            }

            combined.TotalRecordsLongCount += result.TotalRecordsLongCount;
        }

        return combined;
    }

    /// <summary>
    /// Splits a list into batches and executes a void-result operation on each batch.
    /// </summary>
    protected async Task<WaspResult<List<WtResult>>> BatchVoidAsync<TItem>(
        IReadOnlyList<TItem> items,
        Func<IReadOnlyList<TItem>, Task<WaspResult<List<WtResult>>>> operation,
        int batchSize = 500)
    {
        if (items.Count <= batchSize)
            return await operation(items);

        var combined = new WaspResult<List<WtResult>>
        {
            Data = new List<WtResult>()
        };

        for (var i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var result = await operation(batch);

            if (result.Data != null)
                combined.Data.AddRange(result.Data);

            if (result.HasError)
            {
                combined.HasError = true;
                combined.Messages.AddRange(result.Messages);
            }
        }

        return combined;
    }
}
