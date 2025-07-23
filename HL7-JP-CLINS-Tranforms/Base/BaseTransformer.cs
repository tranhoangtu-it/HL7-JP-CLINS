using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.FhirModels.Base;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Interfaces;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Tranforms.Base
{
    /// <summary>
    /// Abstract base class providing common functionality for JP-CLINS document transformers
    /// Implements shared logic for Bundle creation, resource referencing, and JP-CLINS compliance
    /// </summary>
    /// <typeparam name="TInput">Type of input data from hospital systems</typeparam>
    public abstract class BaseTransformer<TInput> : IDocumentTransformer<TInput>
    {
        /// <summary>
        /// Gets the JP-CLINS document type identifier
        /// </summary>
        public abstract string DocumentType { get; }

        /// <summary>
        /// Gets the JP-CLINS profile URL for this document type
        /// </summary>
        public abstract string ProfileUrl { get; }

        /// <summary>
        /// Transforms input data into JP-CLINS compliant FHIR Bundle
        /// </summary>
        /// <param name="input">Hospital source data</param>
        /// <returns>FHIR Bundle with Composition and referenced resources</returns>
        public virtual Bundle Transform(TInput input)
        {
            // Validate input data first
            var validation = ValidateInput(input);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"Invalid input data: {string.Join(", ", validation.Errors)}");
            }

            try
            {
                // Create the main Bundle
                var bundle = CreateBundle();

                // Create the main Composition resource
                var composition = CreateComposition(input);
                bundle.Entry.Add(new BundleEntry { Resource = composition });

                // Transform and add all related resources
                var resources = TransformToResources(input);
                foreach (var resource in resources)
                {
                    bundle.Entry.Add(new BundleEntry { Resource = resource });
                }

                // Update Composition references to point to created resources
                UpdateCompositionReferences(composition, resources);

                // Validate final Bundle against JP-CLINS rules
                ValidateBundle(bundle);

                return bundle;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform {DocumentType}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates input data before transformation
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <returns>Validation result</returns>
        public virtual ValidationResult ValidateInput(TInput input)
        {
            var result = new ValidationResult();

            if (input == null)
            {
                result.AddError("Input data cannot be null");
                return result;
            }

            // Perform document-specific validation
            ValidateSpecificInput(input, result);

            return result;
        }

        /// <summary>
        /// Creates a new FHIR Bundle with JP-CLINS metadata
        /// </summary>
        /// <returns>Initialized Bundle with JP-CLINS profile</returns>
        protected virtual Bundle CreateBundle()
        {
            var bundle = new Bundle
            {
                Id = FhirHelper.GenerateUniqueId("Bundle"),
                Type = "document",
                Timestamp = DateTimeOffset.UtcNow
            };

            return bundle;
        }

        /// <summary>
        /// Creates the main Composition resource for this document type
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>Composition resource</returns>
        protected abstract Composition CreateComposition(TInput input);

        /// <summary>
        /// Transforms input data into a list of FHIR resources
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of FHIR resources</returns>
        protected abstract List<FhirResource> TransformToResources(TInput input);

        /// <summary>
        /// Updates Composition references to point to created resources
        /// </summary>
        /// <param name="composition">Composition to update</param>
        /// <param name="resources">List of created resources</param>
        protected virtual void UpdateCompositionReferences(Composition composition, List<FhirResource> resources)
        {
            // Create lookup dictionary for easy resource finding
            var resourceLookup = resources.ToDictionary(r => r.Id, r => r);

            // Update section references if sections exist
            if (composition.Section != null)
            {
                foreach (var section in composition.Section)
                {
                    UpdateSectionReferences(section, resourceLookup);
                }
            }
        }

        /// <summary>
        /// Updates section references within a Composition section
        /// </summary>
        /// <param name="section">Section to update</param>
        /// <param name="resourceLookup">Dictionary of available resources</param>
        protected virtual void UpdateSectionReferences(CompositionSection section, Dictionary<string, FhirResource> resourceLookup)
        {
            // TODO: Implement section reference updates based on JP-CLINS requirements
            // This will depend on the specific document type and section structure
        }

        /// <summary>
        /// Validates input data specific to this document type
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <param name="result">Validation result to populate</param>
        protected abstract void ValidateSpecificInput(TInput input, ValidationResult result);

        /// <summary>
        /// Validates the final Bundle against JP-CLINS rules
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateBundle(Bundle bundle)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            // Validate required resources are present
            ValidateRequiredResources(bundle);

            // Validate JP-CLINS specific compliance
            ValidateJpClinsCompliance(bundle);
        }

        /// <summary>
        /// Validates that required resources are present in the Bundle
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateRequiredResources(Bundle bundle)
        {
            // Ensure Bundle has at least one entry
            if (bundle.Entry == null || bundle.Entry.Count == 0)
            {
                throw new InvalidOperationException("Bundle must contain at least one resource");
            }

            // Ensure Composition is present
            var composition = bundle.Entry.FirstOrDefault(e => e.Resource is Composition);
            if (composition == null)
            {
                throw new InvalidOperationException("Bundle must contain a Composition resource");
            }
        }

        /// <summary>
        /// Validates JP-CLINS specific compliance rules
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateJpClinsCompliance(Bundle bundle)
        {
            // TODO: Implement JP-CLINS specific validation rules
            // This should check for required Japanese coding systems, identifiers, etc.
        }

        /// <summary>
        /// Creates a resource reference for a given resource
        /// </summary>
        /// <param name="resource">Resource to reference</param>
        /// <returns>Resource reference</returns>
        protected static Reference CreateResourceReference(FhirResource resource)
        {
            return new Reference
            {
                ReferenceValue = $"{resource.ResourceType}/{resource.Id}",
                Display = $"{resource.ResourceType} {resource.Id}"
            };
        }

        /// <summary>
        /// Creates a Japanese-specific CodeableConcept with proper coding system
        /// </summary>
        /// <param name="code">Code value</param>
        /// <param name="system">Coding system</param>
        /// <param name="display">Display text (optional)</param>
        /// <returns>CodeableConcept with Japanese coding</returns>
        protected static CodeableConcept CreateJapaneseCodeableConcept(string code, string system, string? display = null)
        {
            return new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = new Uri(system),
                        Code = code,
                        Display = display
                    }
                },
                Text = display ?? code
            };
        }
    }
}