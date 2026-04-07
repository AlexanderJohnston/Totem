using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantum.Wasp.Models.Common;

namespace Quantum.Web.Wasp.Extensions;

public static class WaspResultExtensions
{
    /// <summary>
    /// Throws if the result has an error.
    /// </summary>
    public static WaspResult<T> EnsureSuccess<T>(this WaspResult<T> result)
    {
        if (result.HasError)
        {
            var messages = string.Join("; ", result.Messages.ConvertAll(m => m.Message));
            throw new WaspApiException(messages, result.Messages);
        }
        return result;
    }

    /// <summary>
    /// Auto-pages through an advanced search endpoint, collecting all results.
    /// </summary>
    public static async Task<List<T>> GetAllPagesAsync<T>(
        Func<AdvancedSearchParameters, Task<WaspResult<List<T>>>> searchFunc,
        AdvancedSearchParameters baseParams = null,
        int pageSize = 100)
    {
        baseParams ??= new AdvancedSearchParameters();
        baseParams.PageSize = pageSize;
        baseParams.PageNumber = 1;

        var allResults = new List<T>();
        long? totalCount = null;

        while (true)
        {
            baseParams.TotalCountFromPriorFetch = totalCount;
            var page = await searchFunc(baseParams);

            if (page.Data != null)
                allResults.AddRange(page.Data);

            totalCount = page.TotalRecordsLongCount;

            if (!ShouldContinuePaging(baseParams.PageNumber, pageSize, page.Data?.Count ?? 0, totalCount ?? 0))
                break;

            baseParams.PageNumber++;
        }

        return allResults;
    }

    static bool ShouldContinuePaging(int pageNumber, int pageSize, int fetchedCount, long totalCount)
    {
        if (fetchedCount == 0)
            return false;

        if (totalCount > 0)
            return (long)pageNumber * pageSize < totalCount;

        return fetchedCount >= pageSize;
    }
}

public class WaspApiException : Exception
{
    public List<WtResult> Errors { get; }

    public WaspApiException(string message, List<WtResult> errors)
        : base(message)
    {
        Errors = errors ?? new List<WtResult>();
    }
}
