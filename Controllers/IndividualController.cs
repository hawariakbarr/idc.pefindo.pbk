using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;

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
    private readonly IValidator<IndividualRequest> _validator;

    public IndividualController(
        IIndividualProcessingService processingService,
        ILogger<IndividualController> logger,
        IValidator<IndividualRequest> validator)
    {
        _processingService = processingService;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Processes individual credit bureau request using JSON object handling for better flexibility
    /// </summary>
    /// <param name="request">Individual request data</param>
    /// <returns>Individual credit summary response</returns>
    /// <response code="200">Request processed successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("individual/json")]
    [ProducesResponseType(typeof(IndividualResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndividualResponse>> ProcessIndividualWithJson(
        [FromBody] IndividualRequest request)
    {
        _logger.LogInformation("Received individual JSON request for app_no: {AppNo}", request.CfLosAppNo);

        // Validate request
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for app_no: {AppNo}, errors: {Errors}",
                request.CfLosAppNo,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            throw new ValidationException(validationResult.Errors);
        }

        var response = await _processingService.ProcessIndividualRequestWithJsonAsync(request);

        _logger.LogInformation("Successfully processed individual JSON request for app_no: {AppNo}", request.CfLosAppNo);
        return Ok(response);
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
