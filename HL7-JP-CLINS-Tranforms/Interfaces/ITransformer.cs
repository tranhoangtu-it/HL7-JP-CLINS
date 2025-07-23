using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.FhirModels.Base;
using HL7_JP_CLINS_Core.Utilities;

namespace HL7_JP_CLINS_Tranforms.Interfaces
{
    /// <summary>
    /// Interface defining the contract for all JP-CLINS data transformers
    /// Ensures consistent transformation behavior across different document types
    /// </summary>
    /// <typeparam name="TInput">Type of input data (hospital source data)</typeparam>
    /// <typeparam name="TOutput">Type of output FHIR resource</typeparam>
    public interface ITransformer<in TInput, out TOutput> where TOutput : FhirResource
    {
        /// <summary>
        /// Transforms hospital source data into JP-CLINS compliant FHIR resources
        /// </summary>
        /// <param name="input">Source data from hospital systems</param>
        /// <returns>FHIR resource compliant with JP-CLINS v1.11.0</returns>
        TOutput Transform(TInput input);

        /// <summary>
        /// Validates input data before transformation
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <returns>Validation result with any errors</returns>
        ValidationResult ValidateInput(TInput input);
    }

    /// <summary>
    /// Specialized interface for document transformers that produce FHIR Bundles
    /// Used for eReferral, eDischargeSummary, and eCheckup documents
    /// </summary>
    /// <typeparam name="TInput">Type of input data</typeparam>
    public interface IDocumentTransformer<in TInput> : ITransformer<TInput, Bundle>
    {
        /// <summary>
        /// Gets the JP-CLINS document type identifier
        /// </summary>
        string DocumentType { get; }

        /// <summary>
        /// Gets the JP-CLINS profile URL for this document type
        /// </summary>
        string ProfileUrl { get; }

        /// <summary>
        /// Transforms input data into a complete FHIR Bundle with Composition
        /// </summary>
        /// <param name="input">Source hospital data</param>
        /// <returns>FHIR Bundle containing Composition and all referenced resources</returns>
        new Bundle Transform(TInput input);
    }
}