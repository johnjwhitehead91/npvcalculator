
using Microsoft.AspNetCore.Mvc;
using Npv.Contracts.Models;
using Npv.Contracts.Requests;
using Npv.Contracts.Responses;
using Npv.Core;

namespace Npv.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NpvController : ControllerBase
    {
        private readonly INpvCalculatorService _npvCalculatorService;
        private readonly ILogger<NpvController> _logger;

        public NpvController(INpvCalculatorService npvCalculatorService, ILogger<NpvController> logger)
        {
            _npvCalculatorService = npvCalculatorService;
            _logger = logger;
        }

        /// <summary>
        /// Start NPV calculation for given cash flows and discount rate parameters
        /// </summary>
        /// <param name="request">NPV calculation parameters including cash flows and discount rate bounds</param>
        /// <returns>Calculation ID for tracking progress</returns>
        /// <response code="200">Calculation started successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(NpvCalculationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CalculateNpv([FromBody] NpvRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Enhanced validation (preserving your existing logic + adding more)
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.ErrorMessage);
                }

                // Your original validation (preserved)
                if (request.UpperBoundDiscountRate <= request.LowerBoundDiscountRate)
                {
                    return BadRequest("Upper bound discount rate must be greater than lower bound discount rate");
                }

                var calculationId = await _npvCalculatorService.StartCalculationAsync(request);

                _logger.LogInformation("Started NPV calculation with ID: {CalculationId}", calculationId);

                // Return structured response instead of anonymous object
                return Ok(new NpvCalculationResponse { CalculationId = calculationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting NPV calculation");
                return StatusCode(500, "An error occurred while starting the calculation");
            }
        }

        /// <summary>
        /// Get NPV calculation results
        /// </summary>
        /// <param name="calculationId">Unique calculation identifier</param>
        /// <returns>Complete NPV calculation results with all discount rates</returns>
        /// <response code="200">Results retrieved successfully</response>
        /// <response code="404">Calculation not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("results/{calculationId}")]
        [ProducesResponseType(typeof(NpvCalculationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetResults(string calculationId)
        {
            try
            {
                var results = await _npvCalculatorService.GetResultsAsync(calculationId);

                if (results == null)
                {
                    return NotFound($"No results found for calculation ID: {calculationId}");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving results for calculation ID: {CalculationId}", calculationId);
                return StatusCode(500, "An error occurred while retrieving results");
            }
        }

        /// <summary>
        /// Get NPV calculation status and progress
        /// </summary>
        /// <param name="calculationId">Unique calculation identifier</param>
        /// <returns>Current calculation status and progress information</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="404">Calculation not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("status/{calculationId}")]
        [ProducesResponseType(typeof(NpvCalculationStatus), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus(string calculationId)
        {
            try
            {
                var status = await _npvCalculatorService.GetStatusAsync(calculationId);

                if (status.Status == "NotFound")
                {
                    return NotFound($"No calculation found with ID: {calculationId}");
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status for calculation ID: {CalculationId}", calculationId);
                return StatusCode(500, "An error occurred while retrieving status");
            }
        }

        /// <summary>
        /// Get calculation history (optional enhancement)
        /// </summary>
        /// <returns>List of past calculations</returns>
        /// <response code="200">History retrieved successfully</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<NpvCalculationHistoryItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var history = await _npvCalculatorService.GetCalculationHistoryAsync();
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting calculation history");
                return StatusCode(500, "An error occurred while retrieving the calculation history");
            }
        }

        /// <summary>
        /// Enhanced validation method (addition to your existing validation)
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateRequest(NpvRequest request)
        {
            if (request.CashFlows == null || request.CashFlows.Count == 0)
            {
                return (false, "At least one cash flow is required");
            }

            if (request.LowerBoundDiscountRate < 0 || request.LowerBoundDiscountRate > 100)
            {
                return (false, "Lower bound discount rate must be between 0 and 100");
            }

            if (request.UpperBoundDiscountRate < 0 || request.UpperBoundDiscountRate > 100)
            {
                return (false, "Upper bound discount rate must be between 0 and 100");
            }

            if (request.DiscountRateIncrement <= 0 || request.DiscountRateIncrement > 10)
            {
                return (false, "Discount rate increment must be between 0.01 and 10");
            }

            if (request.CashFlows.Count > 50)
            {
                return (false, "Maximum 50 cash flow periods allowed");
            }

            // Calculate total number of calculations to ensure reasonable limits
            var totalCalculations = Math.Floor((request.UpperBoundDiscountRate - request.LowerBoundDiscountRate) /
                                               request.DiscountRateIncrement) + 1;
            if (totalCalculations > 1000)
            {
                return (false, "Too many calculations required. Please adjust your discount rate parameters");
            }

            return (true, string.Empty);
        }
    }
}