using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services;

namespace idc.pefindo.pbk.Controllers;

/// <summary>
/// Main API controller for individual credit assessment operations
/// </summary>
[ApiController]
[Route("idcpefindo")]
[Produces("application/json")]
public class IndividualController : ControllerBase
{
    private readonly IIndividualProcessingService _processingService;
    private readonly ILogger<IndividualController> _logger;

    public IndividualController(
        IIndividualProcessingService processingService,
        ILogger<IndividualController> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Processes individual credit bureau request through complete workflow
    /// </summary>
    /// <param name="request">Individual request data</param>
    /// <returns>Individual credit summary response</returns>
    /// <response code="200">Request processed successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("individual")]
    [ProducesResponseType(typeof(IndividualResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndividualResponse>> ProcessIndividual(
        [FromBody] IndividualRequest request)
    {
        try
        {
            _logger.LogInformation("Received individual request for app_no: {AppNo}", request.CfLosAppNo);

            var response = await _processingService.ProcessIndividualRequestAsync(request);

            _logger.LogInformation("Successfully processed individual request for app_no: {AppNo}", request.CfLosAppNo);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error for app_no: {AppNo}", request.CfLosAppNo);
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation for app_no: {AppNo}", request.CfLosAppNo);
            return BadRequest(new ProblemDetails
            {
                Title = "Business Rule Violation", 
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing individual request for app_no: {AppNo}", request.CfLosAppNo);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing the request",
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring service availability
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        var healthData = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return Ok(healthData);
    }
}
