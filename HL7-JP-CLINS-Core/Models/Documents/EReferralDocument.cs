using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Models.Base;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_Core.Models.Documents
{
    /// <summary>
    /// eReferral document model according to JP-CLINS implementation guide
    /// Represents electronic referral information for patient care coordination
    /// </summary>
    public class EReferralDocument : ClinsDocumentBase
    {
        [JsonProperty("referralReason")]
        [Required]
        [MaxLength(1000)]
        public string ReferralReason { get; set; } = string.Empty;

        [JsonProperty("urgency")]
        public string Urgency { get; set; } = "routine"; // routine, urgent, asap, stat

        [JsonProperty("referredToOrganization")]
        [Required]
        public ResourceReference ReferredToOrganization { get; set; } = new ResourceReference();

        [JsonProperty("referredToPractitioner")]
        public ResourceReference? ReferredToPractitioner { get; set; }

        [JsonProperty("encounter")]
        [Required]
        public ResourceReference EncounterReference { get; set; } = new ResourceReference();

        [JsonProperty("serviceRequested")]
        [Required]
        public List<CodeableConcept> ServiceRequested { get; set; } = new List<CodeableConcept>();

        [JsonProperty("clinicalNotes")]
        [MaxLength(5000)]
        public string? ClinicalNotes { get; set; }

        [JsonProperty("relevantHistory")]
        public string? RelevantHistory { get; set; }

        [JsonProperty("currentConditions")]
        public List<ResourceReference> CurrentConditions { get; set; } = new List<ResourceReference>();

        [JsonProperty("currentMedications")]
        public List<ResourceReference> CurrentMedications { get; set; } = new List<ResourceReference>();

        [JsonProperty("allergies")]
        public List<ResourceReference> Allergies { get; set; } = new List<ResourceReference>();

        [JsonProperty("vitalSigns")]
        public List<ResourceReference> VitalSigns { get; set; } = new List<ResourceReference>();

        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        // Core 5 Information Resources commonly used in JP-CLINS documents
        /// <summary>
        /// Current conditions and diagnoses for the patient
        /// </summary>
        [JsonProperty("conditionsInput")]
        public List<ConditionInputModel> ConditionsInput { get; set; } = new List<ConditionInputModel>();

        /// <summary>
        /// Laboratory results and vital signs observations input
        /// </summary>
        [JsonProperty("observationsInput")]
        public List<ObservationInputModel> ObservationsInput { get; set; } = new List<ObservationInputModel>();

        /// <summary>
        /// Current medications and prescriptions input
        /// </summary>
        [JsonProperty("medicationsInput")]
        public List<MedicationRequestInputModel> MedicationsInput { get; set; } = new List<MedicationRequestInputModel>();

        /// <summary>
        /// Allergy and intolerance information input
        /// </summary>
        [JsonProperty("allergiesInput")]
        public List<AllergyIntoleranceInputModel> AllergiesInput { get; set; } = new List<AllergyIntoleranceInputModel>();

        public EReferralDocument()
        {
            Id = FhirHelper.GenerateUniqueId("EReferral");
        }

        /// <summary>
        /// Validates eReferral specific requirements
        /// </summary>
        protected override void ValidateJpClinsRules(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            base.ValidateJpClinsRules(result);

            // eReferral specific validation
            if (string.IsNullOrWhiteSpace(ReferralReason))
            {
                result.IsValid = false;
                result.Errors.Add("Referral reason is required for eReferral documents");
            }

            if (ServiceRequested == null || !ServiceRequested.Any())
            {
                result.IsValid = false;
                result.Errors.Add("At least one service must be requested");
            }

            // JP-CLINS specific eReferral validation rules
            ValidateServiceRequestedCoding(result);
            ValidateJapaneseOrganizationIdentifiers(result);
            ValidateJapaneseReferralRequirements(result);
            ValidateUrgencyLevelFormat(result);
        }

        /// <summary>
        /// Validates service requested codes against Japanese coding systems
        /// </summary>
        private void ValidateServiceRequestedCoding(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            foreach (var service in ServiceRequested)
            {
                if (service.Coding == null || !service.Coding.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add("Service requested must have at least one coding");
                    continue;
                }

                bool hasValidJapaneseCoding = false;
                foreach (var coding in service.Coding)
                {
                    if (IsValidJapaneseServiceCode(coding))
                    {
                        hasValidJapaneseCoding = true;
                        break;
                    }
                }

                if (!hasValidJapaneseCoding)
                {
                    result.Errors.Add($"Service '{service.Text}' should include Japanese standard coding (JLAC10, MEDIS-DC, or JJ1017)");
                }
            }
        }

        /// <summary>
        /// Validates Japanese organization identifiers
        /// </summary>
        private void ValidateJapaneseOrganizationIdentifiers(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate referred to organization
            if (ReferredToOrganization?.Display != null)
            {
                if (string.IsNullOrWhiteSpace(ReferredToOrganization.Display))
                {
                    result.IsValid = false;
                    result.Errors.Add("Referred to organization must have display name in Japanese");
                }
            }

            // Check for proper organization identifier format (Japanese medical institution code)
            var orgRef = ReferredToOrganization?.Reference;
            if (!string.IsNullOrWhiteSpace(orgRef))
            {
                var orgId = orgRef.Split('/').LastOrDefault();
                if (!IsValidJapaneseMedicalInstitutionId(orgId))
                {
                    result.Errors.Add("Organization identifier should follow Japanese medical institution code format");
                }
            }
        }

        /// <summary>
        /// Validates Japanese specific referral requirements
        /// </summary>
        private void ValidateJapaneseReferralRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Check for required Japanese referral information
            if (string.IsNullOrWhiteSpace(ReferralReason))
            {
                result.IsValid = false;
                result.Errors.Add("Referral reason is mandatory for JP-CLINS eReferral");
            }

            // Validate referral reason length for Japanese text
            if (!string.IsNullOrWhiteSpace(ReferralReason))
            {
                if (ContainsJapaneseCharacters(ReferralReason) && ReferralReason.Length > 500)
                {
                    result.Errors.Add("Japanese referral reason should not exceed 500 characters for readability");
                }
            }

            // Check for insurance information (important in Japanese healthcare)
            if (CurrentConditions.Any() && string.IsNullOrWhiteSpace(ClinicalNotes))
            {
                result.Errors.Add("Clinical notes are recommended when current conditions are present");
            }

            // Validate medication references format
            foreach (var medication in CurrentMedications)
            {
                if (!string.IsNullOrWhiteSpace(medication.Reference) &&
                    !medication.Reference.StartsWith("MedicationRequest/") &&
                    !medication.Reference.StartsWith("Medication/"))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Medication reference must be MedicationRequest or Medication resource");
                }
            }
        }

        /// <summary>
        /// Validates urgency level format
        /// </summary>
        private void ValidateUrgencyLevelFormat(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(Urgency))
            {
                var validUrgencies = new[] { "routine", "urgent", "asap", "stat" };
                if (!validUrgencies.Contains(Urgency.ToLower()))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Urgency '{Urgency}' is not a valid JP-CLINS urgency level");
                }

                // For Japanese healthcare context
                if (Urgency.ToLower() == "stat" && string.IsNullOrWhiteSpace(ClinicalNotes))
                {
                    result.Errors.Add("STAT urgency referrals should include detailed clinical notes");
                }
            }
        }

        /// <summary>
        /// Checks if coding uses valid Japanese service codes
        /// </summary>
        private bool IsValidJapaneseServiceCode(Coding coding)
        {
            if (coding?.System == null) return false;

            var japaneseCodeSystems = new[]
            {
                Constants.JpClinsConstants.CodingSystems.JapanProcedure,
                "http://jpfhir.jp/fhir/core/CodeSystem/JLAC10", // Japanese Lab Code
                "http://jpfhir.jp/fhir/core/CodeSystem/MEDIS-DC", // MEDIS master
                "http://jpfhir.jp/fhir/core/CodeSystem/JJ1017", // Japanese procedure codes
                Constants.JpClinsConstants.CodingSystems.LOINC, // International but commonly used
                Constants.JpClinsConstants.CodingSystems.SNOMED // International but commonly used
            };

            return japaneseCodeSystems.Contains(coding.System);
        }

        /// <summary>
        /// Validates Japanese medical institution identifier format
        /// </summary>
        private bool IsValidJapaneseMedicalInstitutionId(string? institutionId)
        {
            if (string.IsNullOrWhiteSpace(institutionId)) return false;

            // Japanese medical institution codes are typically 10 digits
            // Format: PPNNNNNNNN (PP = prefecture code, NNNNNNNN = institution number)
            return institutionId.Length == 10 && institutionId.All(char.IsDigit);
        }

        /// <summary>
        /// Checks if text contains Japanese characters
        /// </summary>
        private bool ContainsJapaneseCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            return text.Any(c =>
                (c >= 0x3040 && c <= 0x309F) || // Hiragana
                (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                (c >= 0x4E00 && c <= 0x9FAF));  // Kanji
        }

        /// <summary>
        /// Creates FHIR Bundle for eReferral document
        /// </summary>
        public override Bundle ToFhirBundle()
        {
            var bundle = new Bundle
            {
                Id = Id,
                Type = Bundle.BundleType.Document,
                Timestamp = CreatedDate,
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/clins/StructureDefinition/JP_Bundle_eReferral" }
                }
            };

            // Create Composition resource as the first entry
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Status = CompositionStatus.Final,
                Type = new CodeableConcept("http://loinc.org", "57133-1", "Referral note"),
                Subject = PatientReference,
                Date = CreatedDate.ToString("yyyy-MM-dd"),
                Author = new List<ResourceReference> { AuthorReference },
                Title = "eReferral Document"
            };

            // Add composition sections
            composition.Section = new List<Composition.SectionComponent>
            {
                new Composition.SectionComponent
                {
                    Title = "Referral Details",
                    Code = new CodeableConcept("http://loinc.org", "42349-1", "Reason for referral"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{ReferralReason}</div>"
                    }
                }
            };

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{composition.Id}",
                Resource = composition
            });

            // TODO: Add other FHIR resources (Patient, Practitioner, Organization, etc.)
            // based on the references in this document

            return bundle;
        }
    }
}