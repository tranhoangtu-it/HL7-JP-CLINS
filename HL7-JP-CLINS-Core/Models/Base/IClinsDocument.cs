using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.Utilities;
using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_Core.Models.Base
{
    /// <summary>
    /// Base interface for all JP-CLINS document types
    /// Defines common properties and behaviors required by the JP-CLINS implementation guide
    /// </summary>
    public interface IClinsDocument
    {
        /// <summary>
        /// Unique identifier for the document
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Document version for tracking changes
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Document creation timestamp
        /// </summary>
        DateTime CreatedDate { get; set; }

        /// <summary>
        /// Document last modified timestamp
        /// </summary>
        DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Document status (draft, final, amended, etc.)
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Patient reference for this document
        /// </summary>
        Reference PatientReference { get; set; }

        /// <summary>
        /// Practitioner who created/authored the document
        /// </summary>
        Reference AuthorReference { get; set; }

        /// <summary>
        /// Organization where the document was created
        /// </summary>
        Reference OrganizationReference { get; set; }

        /// <summary>
        /// Validates the document according to JP-CLINS rules
        /// </summary>
        /// <returns>Validation result with any errors</returns>
        HL7_JP_CLINS_Core.Utilities.ValidationResult Validate();

        /// <summary>
        /// Converts the document to FHIR Bundle format
        /// </summary>
        /// <returns>FHIR Bundle representation</returns>
        Bundle ToFhirBundle();

        /// <summary>
        /// Gets the document type identifier
        /// </summary>
        string DocumentType { get; }

        /// <summary>
        /// Gets the JP-CLINS profile URL
        /// </summary>
        string ProfileUrl { get; }
    }
}