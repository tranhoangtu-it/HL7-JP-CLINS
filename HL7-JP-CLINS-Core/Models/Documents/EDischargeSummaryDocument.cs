using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Models.Base;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_Core.Models.Documents
{
    /// <summary>
    /// eDischargeSummary document model according to JP-CLINS implementation guide
    /// Represents electronic discharge summary for continuity of care
    /// </summary>
    public class EDischargeSummaryDocument : ClinsDocumentBase
    {
        [JsonProperty("admissionDate")]
        [Required]
        public DateTime AdmissionDate { get; set; }

        [JsonProperty("dischargeDate")]
        [Required]
        public DateTime DischargeDate { get; set; }

        [JsonProperty("lengthOfStay")]
        public int LengthOfStay => (DischargeDate - AdmissionDate).Days;

        [JsonProperty("admissionReason")]
        [Required]
        [MaxLength(1000)]
        public string AdmissionReason { get; set; } = string.Empty;

        [JsonProperty("principalDiagnosis")]
        [Required]
        public CodeableConcept PrincipalDiagnosis { get; set; } = new CodeableConcept();

        [JsonProperty("secondaryDiagnoses")]
        public List<CodeableConcept> SecondaryDiagnoses { get; set; } = new List<CodeableConcept>();

        [JsonProperty("procedures")]
        public List<ResourceReference> Procedures { get; set; } = new List<ResourceReference>();

        [JsonProperty("hospitalCourse")]
        [MaxLength(5000)]
        public string? HospitalCourse { get; set; }

        [JsonProperty("dischargeCondition")]
        [Required]
        public string DischargeCondition { get; set; } = string.Empty; // improved, stable, worsened, deceased

        [JsonProperty("dischargeInstructions")]
        [MaxLength(3000)]
        public string? DischargeInstructions { get; set; }

        [JsonProperty("followUpInstructions")]
        [MaxLength(2000)]
        public string? FollowUpInstructions { get; set; }

        [JsonProperty("dischargeMedications")]
        public List<ResourceReference> DischargeMedications { get; set; } = new List<ResourceReference>();

        [JsonProperty("allergies")]
        public List<ResourceReference> Allergies { get; set; } = new List<ResourceReference>();

        [JsonProperty("vitalSignsAtDischarge")]
        public List<ResourceReference> VitalSignsAtDischarge { get; set; } = new List<ResourceReference>();

        [JsonProperty("labResults")]
        public List<ResourceReference> LabResults { get; set; } = new List<ResourceReference>();

        [JsonProperty("imagingStudies")]
        public List<ResourceReference> ImagingStudies { get; set; } = new List<ResourceReference>();

        // Core 5 Information Resources commonly used in JP-CLINS discharge summaries
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

        [JsonProperty("dischargeDestination")]
        public CodeableConcept? DischargeDestination { get; set; }

        [JsonProperty("attendingPhysician")]
        [Required]
        public ResourceReference AttendingPhysician { get; set; } = new ResourceReference();

        public EDischargeSummaryDocument()
        {
            Id = FhirHelper.GenerateUniqueId("EDischargeSummary");
        }

        /// <summary>
        /// Validates eDischargeSummary specific requirements
        /// </summary>
        protected override void ValidateJpClinsRules(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            base.ValidateJpClinsRules(result);

            // eDischargeSummary specific validation
            if (DischargeDate <= AdmissionDate)
            {
                result.IsValid = false;
                result.Errors.Add("Discharge date must be after admission date");
            }

            if (PrincipalDiagnosis == null || string.IsNullOrWhiteSpace(PrincipalDiagnosis.Text))
            {
                result.IsValid = false;
                result.Errors.Add("Principal diagnosis is required");
            }

            if (DischargeDate > DateTime.UtcNow)
            {
                result.IsValid = false;
                result.Errors.Add("Discharge date cannot be in the future");
            }

            // JP-CLINS specific eDischargeSummary validation rules
            ValidateJapaneseDiagnosisCoding(result);
            ValidateJapaneseMedicationCoding(result);
            ValidateJapaneseDischargeRequirements(result);
            ValidateJapaneseHospitalStay(result);
        }

        /// <summary>
        /// Validates Japanese diagnosis coding systems
        /// </summary>
        private void ValidateJapaneseDiagnosisCoding(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate principal diagnosis coding
            if (PrincipalDiagnosis != null)
            {
                bool hasValidJapaneseDiagnosisCoding = false;
                if (PrincipalDiagnosis.Coding != null)
                {
                    foreach (var coding in PrincipalDiagnosis.Coding)
                    {
                        if (IsValidJapaneseDiagnosisCode(coding))
                        {
                            hasValidJapaneseDiagnosisCoding = true;
                            break;
                        }
                    }
                }

                if (!hasValidJapaneseDiagnosisCoding && PrincipalDiagnosis.Coding?.Any() == true)
                {
                    result.Errors.Add("Principal diagnosis should include Japanese standard coding (ICD-10-CM-JP or ICD-11-MMS-JP)");
                }

                // Validate Japanese text description
                if (!string.IsNullOrWhiteSpace(PrincipalDiagnosis.Text) &&
                    ContainsJapaneseCharacters(PrincipalDiagnosis.Text) &&
                    PrincipalDiagnosis.Text.Length > 200)
                {
                    result.Errors.Add("Japanese diagnosis description should not exceed 200 characters");
                }
            }

            // Validate secondary diagnoses
            foreach (var diagnosis in SecondaryDiagnoses)
            {
                if (diagnosis.Coding?.Any() == true)
                {
                    bool hasValidCoding = diagnosis.Coding.Any(IsValidJapaneseDiagnosisCode);
                    if (!hasValidCoding)
                    {
                        result.Errors.Add($"Secondary diagnosis '{diagnosis.Text}' should include Japanese standard coding");
                    }
                }
            }
        }

        /// <summary>
        /// Validates Japanese medication coding standards
        /// </summary>
        private void ValidateJapaneseMedicationCoding(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate discharge medications
            foreach (var medicationRef in DischargeMedications)
            {
                if (!string.IsNullOrWhiteSpace(medicationRef.Reference))
                {
                    if (!medicationRef.Reference.StartsWith("MedicationRequest/") &&
                        !medicationRef.Reference.StartsWith("Medication/"))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Discharge medication references must be MedicationRequest or Medication resources");
                    }
                }

                // Check for Japanese medication display names
                if (!string.IsNullOrWhiteSpace(medicationRef.Display))
                {
                    if (ContainsJapaneseCharacters(medicationRef.Display) && medicationRef.Display.Length > 100)
                    {
                        result.Errors.Add("Japanese medication names should not exceed 100 characters for clarity");
                    }
                }
            }
        }

        /// <summary>
        /// Validates Japanese discharge summary requirements
        /// </summary>
        private void ValidateJapaneseDischargeRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Check for required Japanese discharge information
            if (string.IsNullOrWhiteSpace(AdmissionReason))
            {
                result.IsValid = false;
                result.Errors.Add("Admission reason is mandatory for JP-CLINS discharge summary");
            }

            // Validate discharge condition with Japanese standards
            var validJapaneseDischargeConditions = new[]
            {
                "improved", "stable", "worsened", "deceased", "transferred",
                "治癒", "軽快", "不変", "悪化", "死亡", "転院" // Japanese equivalents
            };

            if (!string.IsNullOrWhiteSpace(DischargeCondition) &&
                !validJapaneseDischargeConditions.Contains(DischargeCondition.ToLower()))
            {
                result.Errors.Add($"Discharge condition '{DischargeCondition}' should use standard Japanese or English terms");
            }

            // Validate hospital course documentation
            if (!string.IsNullOrWhiteSpace(HospitalCourse))
            {
                if (ContainsJapaneseCharacters(HospitalCourse) && HospitalCourse.Length > 2000)
                {
                    result.Errors.Add("Japanese hospital course description should not exceed 2000 characters");
                }
            }

            // Check for follow-up instructions in Japanese context
            if (LengthOfStay > 30 && string.IsNullOrWhiteSpace(FollowUpInstructions))
            {
                result.Errors.Add("Follow-up instructions are recommended for long-term stays (>30 days)");
            }

            // Validate discharge instructions
            if (!string.IsNullOrWhiteSpace(DischargeInstructions))
            {
                if (ContainsJapaneseCharacters(DischargeInstructions) && DischargeInstructions.Length > 1500)
                {
                    result.Errors.Add("Japanese discharge instructions should not exceed 1500 characters");
                }
            }
        }

        /// <summary>
        /// Validates Japanese hospital stay specifics
        /// </summary>
        private void ValidateJapaneseHospitalStay(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Check reasonable length of stay
            if (LengthOfStay < 0)
            {
                result.IsValid = false;
                result.Errors.Add("Length of stay cannot be negative");
            }

            if (LengthOfStay > 365)
            {
                result.Errors.Add("Warning: Length of stay exceeds 1 year, please verify dates");
            }

            // Validate admission/discharge times for Japanese context
            var japanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            var admissionJapanTime = TimeZoneInfo.ConvertTimeFromUtc(AdmissionDate.ToUniversalTime(), japanTimeZone);
            var dischargeJapanTime = TimeZoneInfo.ConvertTimeFromUtc(DischargeDate.ToUniversalTime(), japanTimeZone);

            // Check for reasonable discharge times (Japanese hospitals typically discharge in morning)
            if (dischargeJapanTime.Hour > 18)
            {
                result.Errors.Add("Warning: Late discharge time may indicate administrative delays");
            }

            // Validate procedures during stay
            foreach (var procedureRef in Procedures)
            {
                if (!string.IsNullOrWhiteSpace(procedureRef.Reference) &&
                    !procedureRef.Reference.StartsWith("Procedure/"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Procedure references must reference Procedure resources");
                }
            }

            // Check attending physician reference
            if (AttendingPhysician != null && !string.IsNullOrWhiteSpace(AttendingPhysician.Reference))
            {
                if (!AttendingPhysician.Reference.StartsWith("Practitioner/") &&
                    !AttendingPhysician.Reference.StartsWith("PractitionerRole/"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Attending physician must reference Practitioner or PractitionerRole");
                }
            }
        }

        /// <summary>
        /// Validates Japanese diagnosis coding systems
        /// </summary>
        private bool IsValidJapaneseDiagnosisCode(Coding coding)
        {
            if (coding?.System == null) return false;

            var japaneseDiagnosisCodeSystems = new[]
            {
                Constants.JpClinsConstants.CodingSystems.ICD10CMJP,
                Constants.JpClinsConstants.CodingSystems.ICD10JP,
                "http://jpfhir.jp/fhir/core/CodeSystem/ICD-11-MMS-JP", // ICD-11 Japanese version
                Constants.JpClinsConstants.CodingSystems.ICD10, // International ICD-10
                Constants.JpClinsConstants.CodingSystems.SNOMED // SNOMED CT
            };

            return japaneseDiagnosisCodeSystems.Contains(coding.System);
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
        /// Creates FHIR Bundle for eDischargeSummary document
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
                    Profile = new[] { "http://jpfhir.jp/fhir/clins/StructureDefinition/JP_Bundle_eDischargeSummary" }
                }
            };

            // Create Composition resource
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Status = CompositionStatus.Final,
                Type = new CodeableConcept("http://loinc.org", "18842-5", "Discharge summary"),
                Subject = PatientReference,
                Date = DischargeDate.ToString("yyyy-MM-dd"),
                Author = new List<ResourceReference> { AttendingPhysician },
                Title = "Discharge Summary"
            };

            // Add composition sections
            var sections = new List<Composition.SectionComponent>
            {
                new Composition.SectionComponent
                {
                    Title = "Admission Information",
                    Code = new CodeableConcept("http://loinc.org", "46240-8", "History of encounters"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Admission Date: {AdmissionDate:yyyy-MM-dd}<br/>Reason: {AdmissionReason}</div>"
                    }
                },
                new Composition.SectionComponent
                {
                    Title = "Discharge Information",
                    Code = new CodeableConcept("http://loinc.org", "18842-5", "Discharge summary"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Discharge Date: {DischargeDate:yyyy-MM-dd}<br/>Condition: {DischargeCondition}</div>"
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(HospitalCourse))
            {
                sections.Add(new Composition.SectionComponent
                {
                    Title = "Hospital Course",
                    Code = new CodeableConcept("http://loinc.org", "8648-8", "Hospital course"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{HospitalCourse}</div>"
                    }
                });
            }

            composition.Section = sections;

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{composition.Id}",
                Resource = composition
            });

            // TODO: Add other FHIR resources based on the document content

            return bundle;
        }
    }
}