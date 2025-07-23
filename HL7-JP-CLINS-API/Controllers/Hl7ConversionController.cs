using Microsoft.AspNetCore.Mvc;
using HL7_JP_CLINS_API.Models;
using HL7_JP_CLINS_API.Services;
using HL7_JP_CLINS_Tranforms.Transformers;
using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.Utilities;
using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_API.Controllers
{
    /// <summary>
    /// Controller for converting hospital data to HL7 FHIR R4 format according to JP-CLINS v1.11.0
    /// Provides endpoints for eReferral, eDischargeSummary, and eCheckup document conversions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class Hl7ConversionController : ControllerBase
    {
        private readonly ILogger<Hl7ConversionController> _logger;
        private readonly IFhirSerializationService _fhirSerializationService;
        private readonly EReferralTransformer _eReferralTransformer;
        private readonly EDischargeSummaryTransformer _eDischargeSummaryTransformer;
        private readonly ECheckupTransformer _eCheckupTransformer;

        public Hl7ConversionController(
            ILogger<Hl7ConversionController> logger,
            IFhirSerializationService fhirSerializationService,
            EReferralTransformer eReferralTransformer,
            EDischargeSummaryTransformer eDischargeSummaryTransformer,
            ECheckupTransformer eCheckupTransformer)
        {
            _logger = logger;
            _fhirSerializationService = fhirSerializationService;
            _eReferralTransformer = eReferralTransformer;
            _eDischargeSummaryTransformer = eDischargeSummaryTransformer;
            _eCheckupTransformer = eCheckupTransformer;
        }

        /// <summary>
        /// Converts eReferral data to HL7 FHIR R4 Bundle format
        /// </summary>
        /// <param name="document">eReferral document data</param>
        /// <param name="format">Output format (json or xml)</param>
        /// <param name="prettyFormat">Whether to format output for readability</param>
        /// <returns>HL7 FHIR Bundle in requested format</returns>
        [HttpPost("ereferral")]
        [ProducesResponseType(typeof(ApiResponse<FhirConversionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public IActionResult ConvertEReferral(
            [FromBody] dynamic document,
            [FromQuery] string format = "json",
            [FromQuery] bool prettyFormat = true)
        {
            try
            {
                _logger.LogInformation("Starting eReferral conversion");

                // Validate input
                if (document == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Document data is required"));
                }

                if (!_fhirSerializationService.IsValidFormat(format))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Unsupported format: {format}. Supported formats are: json, xml"));
                }

                // Transform to FHIR Bundle
                var fhirBundle = _eReferralTransformer.Transform(document);

                // Serialize to requested format
                var conversionResponse = _fhirSerializationService.SerializeBundle(fhirBundle, format, prettyFormat);

                _logger.LogInformation("Successfully converted eReferral to {Format}. Bundle ID: {BundleId}",
                    format.ToUpper(), conversionResponse.BundleId);

                // Return appropriate content type
                return Content(conversionResponse.FhirData, conversionResponse.ContentType);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during eReferral conversion");
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation error", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting eReferral");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error during conversion"));
            }
        }

        /// <summary>
        /// Converts eDischargeSummary data to HL7 FHIR R4 Bundle format
        /// </summary>
        /// <param name="document">eDischargeSummary document data</param>
        /// <param name="format">Output format (json or xml)</param>
        /// <param name="prettyFormat">Whether to format output for readability</param>
        /// <returns>HL7 FHIR Bundle in requested format</returns>
        [HttpPost("dischargesummary")]
        [ProducesResponseType(typeof(ApiResponse<FhirConversionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public IActionResult ConvertDischargeSummary(
            [FromBody] dynamic document,
            [FromQuery] string format = "json",
            [FromQuery] bool prettyFormat = true)
        {
            try
            {
                _logger.LogInformation("Starting eDischargeSummary conversion");

                // Validate input
                if (document == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Document data is required"));
                }

                if (!_fhirSerializationService.IsValidFormat(format))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Unsupported format: {format}. Supported formats are: json, xml"));
                }

                // Transform to FHIR Bundle
                var fhirBundle = _eDischargeSummaryTransformer.Transform(document);

                // Serialize to requested format
                var conversionResponse = _fhirSerializationService.SerializeBundle(fhirBundle, format, prettyFormat);

                _logger.LogInformation("Successfully converted eDischargeSummary to {Format}. Bundle ID: {BundleId}",
                    format.ToUpper(), conversionResponse.BundleId);

                // Return appropriate content type
                return Content(conversionResponse.FhirData, conversionResponse.ContentType);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during eDischargeSummary conversion");
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation error", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting eDischargeSummary");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error during conversion"));
            }
        }

        /// <summary>
        /// Converts eCheckup data to HL7 FHIR R4 Bundle format
        /// </summary>
        /// <param name="document">eCheckup document data</param>
        /// <param name="format">Output format (json or xml)</param>
        /// <param name="prettyFormat">Whether to format output for readability</param>
        /// <returns>HL7 FHIR Bundle in requested format</returns>
        [HttpPost("checkup")]
        [ProducesResponseType(typeof(ApiResponse<FhirConversionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public IActionResult ConvertCheckup(
            [FromBody] dynamic document,
            [FromQuery] string format = "json",
            [FromQuery] bool prettyFormat = true)
        {
            try
            {
                _logger.LogInformation("Starting eCheckup conversion");

                // Validate input
                if (document == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Document data is required"));
                }

                if (!_fhirSerializationService.IsValidFormat(format))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"Unsupported format: {format}. Supported formats are: json, xml"));
                }

                // Transform to FHIR Bundle
                var fhirBundle = _eCheckupTransformer.Transform(document);

                // Serialize to requested format
                var conversionResponse = _fhirSerializationService.SerializeBundle(fhirBundle, format, prettyFormat);

                _logger.LogInformation("Successfully converted eCheckup to {Format}. Bundle ID: {BundleId}",
                    format.ToUpper(), conversionResponse.BundleId);

                // Return appropriate content type
                return Content(conversionResponse.FhirData, conversionResponse.ContentType);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during eCheckup conversion");
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation error", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting eCheckup");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error during conversion"));
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "HL7-JP-CLINS-API",
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Gets API capabilities and supported formats
        /// </summary>
        /// <returns>API capabilities information</returns>
        [HttpGet("capabilities")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetCapabilities()
        {
            return Ok(new
            {
                service = "HL7-JP-CLINS-API",
                version = "1.0.0",
                description = "HL7 FHIR R4 conversion service for JP-CLINS v1.11.0",
                supportedFormats = new[] { "json", "xml" },
                supportedDocumentTypes = new[]
                {
                    "eReferral",
                    "eDischargeSummary",
                    "eCheckup"
                },
                compliance = "JP-CLINS v1.11.0",
                fhirVersion = "R4"
            });
        }
    }
}