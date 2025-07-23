using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_API.Models;

namespace HL7_JP_CLINS_API.Services
{
    /// <summary>
    /// Service for serializing FHIR resources to JSON and XML formats
    /// Handles JP-CLINS specific serialization requirements
    /// </summary>
    public interface IFhirSerializationService
    {
        /// <summary>
        /// Serializes a FHIR Bundle to the specified format
        /// </summary>
        /// <param name="bundle">FHIR Bundle to serialize</param>
        /// <param name="format">Output format (json or xml)</param>
        /// <param name="prettyFormat">Whether to format output for readability</param>
        /// <returns>Serialization result</returns>
        FhirConversionResponse SerializeBundle(Bundle bundle, string format, bool prettyFormat = true);

        /// <summary>
        /// Gets the appropriate Content-Type header for the format
        /// </summary>
        /// <param name="format">Format (json or xml)</param>
        /// <returns>MIME type string</returns>
        string GetContentType(string format);

        /// <summary>
        /// Validates if the format is supported
        /// </summary>
        /// <param name="format">Format to validate</param>
        /// <returns>True if supported</returns>
        bool IsValidFormat(string format);
    }

    /// <summary>
    /// Implementation of FHIR serialization service
    /// </summary>
    public class FhirSerializationService : IFhirSerializationService
    {
        private readonly ILogger<FhirSerializationService> _logger;

        public FhirSerializationService(ILogger<FhirSerializationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Serializes a FHIR Bundle to the specified format
        /// </summary>
        public FhirConversionResponse SerializeBundle(Bundle bundle, string format, bool prettyFormat = true)
        {
            try
            {
                if (bundle == null)
                    throw new ArgumentNullException(nameof(bundle));

                var normalizedFormat = format.ToLowerInvariant();

                if (!IsValidFormat(normalizedFormat))
                    throw new ArgumentException($"Unsupported format: {format}. Supported formats are: json, xml");

                var response = new FhirConversionResponse
                {
                    Format = normalizedFormat,
                    ContentType = GetContentType(normalizedFormat),
                    ResourceCount = bundle.Entry?.Count ?? 0,
                    BundleId = bundle.Id,
                    DocumentType = GetDocumentType(bundle)
                };

                switch (normalizedFormat)
                {
                    case "json":
                        response.FhirData = FhirSerializer.SerializeToJson(bundle);
                        break;

                    case "xml":
                        response.FhirData = FhirSerializer.SerializeToXml(bundle);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported format: {format}");
                }

                _logger.LogInformation("Successfully serialized FHIR Bundle to {Format}. Bundle ID: {BundleId}, Resources: {ResourceCount}",
                    normalizedFormat.ToUpper(), response.BundleId, response.ResourceCount);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize FHIR Bundle to {Format}", format);
                throw new InvalidOperationException($"Failed to serialize FHIR Bundle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the appropriate Content-Type header for the format
        /// </summary>
        public string GetContentType(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "json" => "application/fhir+json; charset=utf-8",
                "xml" => "application/fhir+xml; charset=utf-8",
                _ => "application/json; charset=utf-8"
            };
        }

        /// <summary>
        /// Validates if the format is supported
        /// </summary>
        public bool IsValidFormat(string format)
        {
            var supportedFormats = new[] { "json", "xml" };
            return supportedFormats.Contains(format.ToLowerInvariant());
        }

        /// <summary>
        /// Extracts document type from Bundle
        /// </summary>
        private string? GetDocumentType(Bundle bundle)
        {
            try
            {
                // Try to determine from Bundle type
                if (bundle.Type == "document")
                {
                    // Try to determine from Composition type
                    var composition = bundle.Entry?.FirstOrDefault()?.Resource as Composition;
                    if (composition?.Type?.Coding?.Any() == true)
                    {
                        var typeCode = composition.Type.Coding.First().Code;
                        return typeCode switch
                        {
                            "18761-7" => "eReferral",
                            "18842-5" => "eDischargeSummary",
                            "11502-2" => "eCheckup",
                            _ => "unknown"
                        };
                    }
                }

                return "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine document type from Bundle");
                return "unknown";
            }
        }
    }
}