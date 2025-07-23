using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_API.Models
{
    /// <summary>
    /// Standard API response wrapper for HL7 FHIR conversion operations
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The converted FHIR data (when successful)
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Error message (when unsuccessful)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Detailed validation errors (if any)
        /// </summary>
        public List<string>? ValidationErrors { get; set; }

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful response
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data
            };
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string errorMessage, List<string>? validationErrors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                ErrorMessage = errorMessage,
                ValidationErrors = validationErrors
            };
        }
    }

    /// <summary>
    /// Request model for specifying output format preferences
    /// </summary>
    public class ConversionRequest
    {
        /// <summary>
        /// Desired output format: json or xml
        /// </summary>
        [Required]
        public string Format { get; set; } = "json";

        /// <summary>
        /// Whether to pretty-format the output
        /// </summary>
        public bool PrettyFormat { get; set; } = true;

        /// <summary>
        /// Whether to include narrative text in the FHIR output
        /// </summary>
        public bool IncludeNarrative { get; set; } = true;
    }

    /// <summary>
    /// Response model containing the converted FHIR data
    /// </summary>
    public class FhirConversionResponse
    {
        /// <summary>
        /// The converted FHIR Bundle as string (JSON or XML)
        /// </summary>
        public string FhirData { get; set; } = string.Empty;

        /// <summary>
        /// The format of the returned data (json/xml)
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// MIME content type for the response
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Number of resources in the Bundle
        /// </summary>
        public int ResourceCount { get; set; }

        /// <summary>
        /// Bundle ID
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// JP-CLINS document type
        /// </summary>
        public string? DocumentType { get; set; }
    }
}