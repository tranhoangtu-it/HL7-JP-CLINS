using Hl7.Fhir.Model;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.Models.Base
{
    /// <summary>
    /// Abstract base class implementing common functionality for all JP-CLINS documents
    /// </summary>
    public abstract class ClinsDocumentBase : IClinsDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [JsonProperty("lastModifiedDate")]
        public DateTime? LastModifiedDate { get; set; }

        [JsonProperty("status")]
        [Required]
        public string Status { get; set; } = "draft";

        [JsonProperty("patientReference")]
        [Required]
        public ResourceReference PatientReference { get; set; } = new ResourceReference();

        [JsonProperty("authorReference")]
        [Required]
        public ResourceReference AuthorReference { get; set; } = new ResourceReference();

        [JsonProperty("organizationReference")]
        [Required]
        public ResourceReference OrganizationReference { get; set; } = new ResourceReference();

        /// <summary>
        /// Common validation logic for all documents
        /// Override in derived classes for specific validation rules
        /// </summary>
        public virtual ValidationResult Validate()
        {
            var result = new ValidationResult();
            var context = new ValidationContext(this);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(this, context, validationResults, true))
            {
                result.IsValid = false;
                result.Errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown error").ToList();
            }
            else
            {
                result.IsValid = true;
                result.Errors = new List<string>();
            }

            // Add JP-CLINS specific validation rules here
            ValidateJpClinsRules(result);

            return result;
        }

        /// <summary>
        /// JP-CLINS specific validation rules
        /// Based on JP-CLINS Implementation Guide v1.11.0
        /// </summary>
        protected virtual void ValidateJpClinsRules(ValidationResult result)
        {
            // Validate document ID format according to JP-CLINS
            if (!string.IsNullOrWhiteSpace(Id) && !IsValidJpClinsDocumentId(Id))
            {
                result.IsValid = false;
                result.Errors.Add("Document ID must follow JP-CLINS format: prefix-timestamp-guid");
            }

            // Validate version format
            if (!string.IsNullOrWhiteSpace(Version) && !IsValidVersionFormat(Version))
            {
                result.IsValid = false;
                result.Errors.Add("Version must follow semantic versioning format (e.g., 1.0, 1.1.0)");
            }

            // Validate status according to JP-CLINS allowed values
            if (!IsValidJpClinsStatus(Status))
            {
                result.IsValid = false;
                result.Errors.Add($"Status '{Status}' is not valid for JP-CLINS documents");
            }

            // Validate patient reference format
            if (PatientReference != null && !string.IsNullOrWhiteSpace(PatientReference.Reference))
            {
                if (!IsValidFhirReference(PatientReference.Reference, "Patient"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Patient reference must follow FHIR format: Patient/[id]");
                }
            }

            // Validate author reference format
            if (AuthorReference != null && !string.IsNullOrWhiteSpace(AuthorReference.Reference))
            {
                if (!IsValidFhirReference(AuthorReference.Reference, "Practitioner", "PractitionerRole", "Organization"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Author reference must be Practitioner, PractitionerRole, or Organization");
                }
            }

            // Validate organization reference format
            if (OrganizationReference != null && !string.IsNullOrWhiteSpace(OrganizationReference.Reference))
            {
                if (!IsValidFhirReference(OrganizationReference.Reference, "Organization"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Organization reference must follow FHIR format: Organization/[id]");
                }
            }

            // Validate Japanese business hours for document creation
            ValidateJapaneseBusinessContext(result);
        }

        private bool IsValidJpClinsDocumentId(string id)
        {
            // JP-CLINS document ID format: prefix-timestamp-guid
            var parts = id.Split('-');
            return parts.Length >= 3 &&
                   !string.IsNullOrWhiteSpace(parts[0]) &&
                   parts[1].All(char.IsDigit) &&
                   parts[1].Length >= 8;
        }

        private bool IsValidVersionFormat(string version)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"^\d+(\.\d+){0,2}$");
            return regex.IsMatch(version);
        }

        private bool IsValidJpClinsStatus(string status)
        {
            var validStatuses = new[] { "draft", "final", "amended", "cancelled", "replaced" };
            return validStatuses.Contains(status?.ToLower());
        }

        private bool IsValidFhirReference(string reference, params string[] allowedResourceTypes)
        {
            if (string.IsNullOrWhiteSpace(reference)) return false;

            var parts = reference.Split('/');
            if (parts.Length != 2) return false;

            return allowedResourceTypes.Contains(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]);
        }

        private void ValidateJapaneseBusinessContext(ValidationResult result)
        {
            // Convert to Japan time for validation
            var japanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            var japanTime = TimeZoneInfo.ConvertTimeFromUtc(CreatedDate.ToUniversalTime(), japanTimeZone);

            // Check if document is created during reasonable business hours (optional warning)
            if (japanTime.Hour < 6 || japanTime.Hour > 22)
            {
                // This is a warning, not an error - documents can be created outside business hours
                result.Errors.Add($"Warning: Document created outside typical business hours ({japanTime:HH:mm} JST)");
            }

            // Validate date format matches Japanese standards
            if (CreatedDate.Kind != DateTimeKind.Utc)
            {
                result.IsValid = false;
                result.Errors.Add("Document creation date must be in UTC format for international compatibility");
            }
        }

        /// <summary>
        /// Abstract method to be implemented by derived classes
        /// Each document type will have its own FHIR Bundle structure
        /// </summary>
        public abstract Bundle ToFhirBundle();

        /// <summary>
        /// Updates the last modified timestamp
        /// </summary>
        public void UpdateLastModified()
        {
            LastModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Validation result class
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }
}