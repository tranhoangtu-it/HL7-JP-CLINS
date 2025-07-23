using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.Utilities;
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
        public Reference PatientReference { get; set; } = new Reference();

        [JsonProperty("authorReference")]
        [Required]
        public Reference AuthorReference { get; set; } = new Reference();

        [JsonProperty("organizationReference")]
        [Required]
        public Reference OrganizationReference { get; set; } = new Reference();

        /// <summary>
        /// Common validation logic for all documents
        /// Override in derived classes for specific validation rules
        /// </summary>
        public virtual HL7_JP_CLINS_Core.Utilities.ValidationResult Validate()
        {
            var result = new HL7_JP_CLINS_Core.Utilities.ValidationResult();
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
        protected virtual void ValidateJpClinsRules(HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Validate document ID format according to JP-CLINS
            if (!string.IsNullOrWhiteSpace(Id) && !IsValidJpClinsDocumentId(Id))
            {
                result.AddError("Document ID must follow JP-CLINS format: prefix-timestamp-guid");
            }

            // Validate version format
            if (!string.IsNullOrWhiteSpace(Version) && !IsValidVersionFormat(Version))
            {
                result.AddError("Version must follow semantic versioning format (e.g., 1.0, 1.1.0)");
            }

            // Validate status according to JP-CLINS allowed values
            if (!IsValidJpClinsStatus(Status))
            {
                result.AddError($"Status '{Status}' is not valid for JP-CLINS documents");
            }

            // Validate patient reference format
            if (PatientReference != null && !string.IsNullOrWhiteSpace(PatientReference.ReferenceValue))
            {
                if (!IsValidFhirReference(PatientReference.ReferenceValue, "Patient"))
                {
                    result.AddError("Patient reference must follow FHIR format: Patient/[id]");
                }
            }

            // Validate author reference format
            if (AuthorReference != null && !string.IsNullOrWhiteSpace(AuthorReference.ReferenceValue))
            {
                if (!IsValidFhirReference(AuthorReference.ReferenceValue, "Practitioner", "Organization"))
                {
                    result.AddError("Author reference must follow FHIR format: Practitioner/[id] or Organization/[id]");
                }
            }

            // Validate organization reference format
            if (OrganizationReference != null && !string.IsNullOrWhiteSpace(OrganizationReference.ReferenceValue))
            {
                if (!IsValidFhirReference(OrganizationReference.ReferenceValue, "Organization"))
                {
                    result.AddError("Organization reference must follow FHIR format: Organization/[id]");
                }
            }

            // Validate Japanese business context requirements
            ValidateJapaneseBusinessContext(result);
        }

        /// <summary>
        /// Validates JP-CLINS document ID format
        /// </summary>
        private bool IsValidJpClinsDocumentId(string id)
        {
            // JP-CLINS document ID format: prefix-timestamp-guid
            // Example: eReferral-20231201-12345678-90ab-cdef-1234-567890abcdef
            var parts = id.Split('-');
            return parts.Length >= 4 && parts[0].Length > 0 && parts[1].Length == 8;
        }

        /// <summary>
        /// Validates version format
        /// </summary>
        private bool IsValidVersionFormat(string version)
        {
            // Semantic versioning: major.minor.patch
            return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+(\.\d+)?$");
        }

        /// <summary>
        /// Validates JP-CLINS status values
        /// </summary>
        private bool IsValidJpClinsStatus(string status)
        {
            var validStatuses = new[] { "draft", "active", "suspended", "cancelled", "completed" };
            return validStatuses.Contains(status.ToLowerInvariant());
        }

        /// <summary>
        /// Validates FHIR reference format
        /// </summary>
        private bool IsValidFhirReference(string reference, params string[] allowedResourceTypes)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return false;

            var parts = reference.Split('/');
            if (parts.Length != 2)
                return false;

            return allowedResourceTypes.Contains(parts[0]);
        }

        /// <summary>
        /// Validates Japanese business context requirements
        /// </summary>
        private void ValidateJapaneseBusinessContext(HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Check for Japanese text in document
            if (!string.IsNullOrWhiteSpace(Id) && !ContainsJapaneseCharacters(Id))
            {
                result.AddWarning("Document ID should include Japanese characters for better identification");
            }

            // Validate created date is not in the future
            if (CreatedDate > DateTime.UtcNow)
            {
                result.AddError("Created date cannot be in the future");
            }

            // Validate last modified date is not before created date
            if (LastModifiedDate.HasValue && LastModifiedDate.Value < CreatedDate)
            {
                result.AddError("Last modified date cannot be before created date");
            }
        }

        /// <summary>
        /// Checks if text contains Japanese characters
        /// </summary>
        private bool ContainsJapaneseCharacters(string text)
        {
            return text.Any(c => (c >= 0x3040 && c <= 0x309F) || // Hiragana
                                (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                                (c >= 0x4E00 && c <= 0x9FAF));  // Kanji
        }

        /// <summary>
        /// Converts document to FHIR Bundle
        /// </summary>
        public abstract Bundle ToFhirBundle();

        /// <summary>
        /// Updates the last modified date
        /// </summary>
        public void UpdateLastModified()
        {
            LastModifiedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the document type identifier
        /// </summary>
        public abstract string DocumentType { get; }

        /// <summary>
        /// Gets the JP-CLINS profile URL
        /// </summary>
        public abstract string ProfileUrl { get; }
    }
}
}