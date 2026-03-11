using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quantum.Wasp.Models.Common;

namespace Outermind.Controllers.Wasp;

public abstract class WaspApiControllerBase : ControllerBase
{
    private readonly ILogger _logger;

    protected WaspApiControllerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected async Task<ActionResult<WaspResult<T>>> ExecuteAsync<T>(Func<Task<WaspResult<T>>> operation, string operationName)
    {
        try
        {
            return Ok(await operation());
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            var statusCode = (int)ex.StatusCode.Value;
            _logger.LogError(ex, "WASP request failed for {Operation} with status code {StatusCode}.", operationName, statusCode);
            return StatusCode(statusCode, CreateHttpErrorResult<T>(statusCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected WASP facade error for {Operation}.", operationName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateHttpErrorResult<T>(StatusCodes.Status500InternalServerError, ex.Message));
        }
    }

    private static WaspResult<T> CreateHttpErrorResult<T>(int statusCode, string message) =>
        new()
        {
            Data = default,
            HasError = true,
            HasHttpError = true,
            HasMessage = true,
            Messages =
            {
                new WtResult
                {
                    ResultCode = statusCode,
                    HttpStatusCode = statusCode,
                    Message = message,
                    FieldName = string.Empty
                }
            }
        };
}
