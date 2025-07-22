using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
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
                bundle.Entry.Add(new Bundle.EntryComponent { Resource = composition });

                // Transform and add all related resources
                var resources = TransformToResources(input);
                foreach (var resource in resources)
                {
                    bundle.Entry.Add(new Bundle.EntryComponent { Resource = resource });
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
                Meta = new Meta
                {
                    Profile = new[] { ProfileUrl },
                    Tag = new List<Coding>
                    {
                        new Coding("http://jpfhir.jp/fhir/clins/CodeSystem/jp-clins-document-codes", DocumentType, $"JP-CLINS {DocumentType} Document")
                    },
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Type = Bundle.BundleType.Document,
                Timestamp = DateTimeOffset.UtcNow,
                Entry = new List<Bundle.EntryComponent>()
            };

            return bundle;
        }

        /// <summary>
        /// Creates the main Composition resource for the document
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>Composition resource</returns>
        protected abstract Composition CreateComposition(TInput input);

        /// <summary>
        /// Transforms input data into all required FHIR resources (Patient, Practitioner, Organization, etc.)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of FHIR resources</returns>
        protected abstract List<Resource> TransformToResources(TInput input);

        /// <summary>
        /// Updates Composition section references to point to created resources
        /// </summary>
        /// <param name="composition">The composition to update</param>
        /// <param name="resources">List of created resources</param>
        protected virtual void UpdateCompositionReferences(Composition composition, List<Resource> resources)
        {
            // Create a lookup dictionary for quick resource access
            var resourceLookup = resources.ToDictionary(r => r.Id, r => r);

            // Update section references
            if (composition.Section != null)
            {
                foreach (var section in composition.Section)
                {
                    UpdateSectionReferences(section, resourceLookup);
                }
            }
        }

        /// <summary>
        /// Updates references in a specific composition section
        /// </summary>
        /// <param name="section">Section to update</param>
        /// <param name="resourceLookup">Dictionary of available resources</param>
        protected virtual void UpdateSectionReferences(Composition.SectionComponent section, Dictionary<string, Resource> resourceLookup)
        {
            if (section.Entry != null)
            {
                for (int i = 0; i < section.Entry.Count; i++)
                {
                    var reference = section.Entry[i];
                    if (!string.IsNullOrWhiteSpace(reference.Reference))
                    {
                        // Ensure reference format is correct
                        var resourceId = reference.Reference.Split('/').LastOrDefault();
                        if (!string.IsNullOrWhiteSpace(resourceId) && resourceLookup.ContainsKey(resourceId))
                        {
                            var resource = resourceLookup[resourceId];
                            section.Entry[i] = FhirHelper.CreateReference(resource.TypeName, resource.Id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Document-specific input validation (to be implemented by derived classes)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result to update</param>
        protected abstract void ValidateSpecificInput(TInput input, ValidationResult result);

        /// <summary>
        /// Validates the final Bundle against JP-CLINS requirements
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateBundle(Bundle bundle)
        {
            // Basic Bundle validation
            if (bundle.Entry == null || !bundle.Entry.Any())
            {
                throw new InvalidOperationException("Bundle must contain at least one entry");
            }

            // First entry must be Composition
            var firstEntry = bundle.Entry.First();
            if (firstEntry.Resource is not Composition)
            {
                throw new InvalidOperationException("First entry in JP-CLINS document Bundle must be a Composition");
            }

            // Check for required resource types
            ValidateRequiredResources(bundle);

            // JP-CLINS specific validation
            ValidateJpClinsCompliance(bundle);
        }

        /// <summary>
        /// Validates required resources are present in the Bundle
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateRequiredResources(Bundle bundle)
        {
            var resourceTypes = bundle.Entry.Select(e => e.Resource?.TypeName).Where(t => t != null).ToList();

            var requiredTypes = new[] { "Composition", "Patient" };
            foreach (var requiredType in requiredTypes)
            {
                if (!resourceTypes.Contains(requiredType))
                {
                    throw new InvalidOperationException($"Bundle must contain a {requiredType} resource");
                }
            }
        }

        /// <summary>
        /// Validates JP-CLINS specific compliance rules
        /// </summary>
        /// <param name="bundle">Bundle to validate</param>
        protected virtual void ValidateJpClinsCompliance(Bundle bundle)
        {
            // Check Bundle profile
            if (bundle.Meta?.Profile?.Any() != true || !bundle.Meta.Profile.Contains(ProfileUrl))
            {
                throw new InvalidOperationException($"Bundle must declare JP-CLINS profile: {ProfileUrl}");
            }

            // Validate document type tag
            var documentTypeTag = bundle.Meta?.Tag?.FirstOrDefault(t =>
                t.System == "http://jpfhir.jp/fhir/clins/CodeSystem/jp-clins-document-codes");

            if (documentTypeTag == null || documentTypeTag.Code != DocumentType)
            {
                throw new InvalidOperationException($"Bundle must have correct JP-CLINS document type tag: {DocumentType}");
            }
        }

        /// <summary>
        /// Helper method to create resource references with proper formatting
        /// </summary>
        /// <param name="resource">Resource to reference</param>
        /// <returns>ResourceReference</returns>
        protected static ResourceReference CreateResourceReference(Resource resource)
        {
            return FhirHelper.CreateReference(resource.TypeName, resource.Id);
        }

        /// <summary>
        /// Helper method to create Japanese-specific CodeableConcept
        /// </summary>
        /// <param name="code">Code value</param>
        /// <param name="system">Coding system</param>
        /// <param name="display">Display text</param>
        /// <returns>CodeableConcept with Japanese coding</returns>
        protected static CodeableConcept CreateJapaneseCodeableConcept(string code, string system, string? display = null)
        {
            return FhirHelper.CreateCodeableConcept(code: code, system: system, display: display);
        }

        // TODO: JP-CLINS Implementation Notes:
        // 1. Ensure all resources follow JP-CLINS profile constraints
        // 2. Use Japanese coding systems where specified (JLAC10, YJ codes, ICD-10-CM-JP)
        // 3. Apply Japanese healthcare workflow requirements
        // 4. Include mandatory extensions for Japanese healthcare context
        // 5. Validate against JP-CLINS terminology bindings
        // 6. Ensure proper resource linking and referencing
        // 7. Apply Japanese privacy and consent requirements
        // 8. Include appropriate metadata for Japanese healthcare systems
    }
}