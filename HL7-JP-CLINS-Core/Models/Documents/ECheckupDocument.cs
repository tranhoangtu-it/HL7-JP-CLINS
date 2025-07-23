using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Models.Base;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace HL7_JP_CLINS_Core.Models.Documents
{
    /// <summary>
    /// eCheckup document model according to JP-CLINS implementation guide
    /// Represents electronic health checkup/examination results
    /// </summary>
    public class ECheckupDocument : ClinsDocumentBase
    {
        [JsonProperty("checkupDate")]
        [Required]
        public DateTime CheckupDate { get; set; }

        [JsonProperty("checkupType")]
        [Required]
        public CodeableConcept CheckupType { get; set; } = new CodeableConcept();

        [JsonProperty("checkupPurpose")]
        [Required]
        [MaxLength(500)]
        public string CheckupPurpose { get; set; } = string.Empty; // annual, pre-employment, follow-up, etc.

        [JsonProperty("examiningPhysician")]
        [Required]
        public ResourceReference ExaminingPhysician { get; set; } = new ResourceReference();

        [JsonProperty("encounter")]
        [Required]
        public ResourceReference EncounterReference { get; set; } = new ResourceReference();

        [JsonProperty("vitalSigns")]
        public List<ResourceReference> VitalSigns { get; set; } = new List<ResourceReference>();

        [JsonProperty("physicalExamFindings")]
        public List<ResourceReference> PhysicalExamFindings { get; set; } = new List<ResourceReference>();

        [JsonProperty("laboratoryResults")]
        public List<ResourceReference> LaboratoryResults { get; set; } = new List<ResourceReference>();

        [JsonProperty("imagingResults")]
        public List<ResourceReference> ImagingResults { get; set; } = new List<ResourceReference>();

        [JsonProperty("cardiologyTests")]
        public List<ResourceReference> CardiologyTests { get; set; } = new List<ResourceReference>(); // ECG, etc.

        [JsonProperty("visionHearingTests")]
        public List<ResourceReference> VisionHearingTests { get; set; } = new List<ResourceReference>();

        [JsonProperty("mentalHealthAssessment")]
        public ResourceReference? MentalHealthAssessment { get; set; }

        [JsonProperty("riskFactors")]
        public List<CodeableConcept> RiskFactors { get; set; } = new List<CodeableConcept>();

        [JsonProperty("recommendations")]
        [MaxLength(3000)]
        public string? Recommendations { get; set; }

        [JsonProperty("followUpRequired")]
        public bool FollowUpRequired { get; set; }

        [JsonProperty("followUpDate")]
        public DateTime? FollowUpDate { get; set; }

        [JsonProperty("overallAssessment")]
        [Required]
        public CodeableConcept OverallAssessment { get; set; } = new CodeableConcept(); // normal, abnormal, requires attention

        [JsonProperty("certificationStatus")]
        public string? CertificationStatus { get; set; } // fit for work, restricted, unfit

        [JsonProperty("restrictions")]
        public List<string> Restrictions { get; set; } = new List<string>();

        [JsonProperty("vaccinations")]
        public List<ResourceReference> Vaccinations { get; set; } = new List<ResourceReference>();

        // Core 5 Information Resources commonly used in JP-CLINS health checkup documents
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

        public ECheckupDocument()
        {
            Id = FhirHelper.GenerateUniqueId("ECheckup");
            CheckupDate = DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Validates eCheckup specific requirements
        /// </summary>
        protected override void ValidateJpClinsRules(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            base.ValidateJpClinsRules(result);

            // eCheckup specific validation
            if (CheckupDate > DateTime.UtcNow.Date)
            {
                result.IsValid = false;
                result.Errors.Add("Checkup date cannot be in the future");
            }

            if (CheckupType == null || string.IsNullOrWhiteSpace(CheckupType.Text))
            {
                result.IsValid = false;
                result.Errors.Add("Checkup type is required");
            }

            if (FollowUpRequired && !FollowUpDate.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add("Follow-up date is required when follow-up is indicated");
            }

            if (FollowUpDate.HasValue && FollowUpDate <= CheckupDate)
            {
                result.IsValid = false;
                result.Errors.Add("Follow-up date must be after checkup date");
            }

            // JP-CLINS specific eCheckup validation rules
            ValidateJapaneseHealthCheckupStandards(result);
            ValidateRequiredExaminationsByType(result);
            ValidateHealthAssessmentCoding(result);
            ValidateOccupationalHealthCompliance(result);
        }

        /// <summary>
        /// Validates Japanese health checkup standards compliance
        /// </summary>
        private void ValidateJapaneseHealthCheckupStandards(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate checkup type coding with Japanese standards
            if (CheckupType != null && CheckupType.Coding?.Any() == true)
            {
                bool hasValidJapaneseCheckupType = false;
                foreach (var coding in CheckupType.Coding)
                {
                    if (IsValidJapaneseCheckupTypeCode(coding))
                    {
                        hasValidJapaneseCheckupType = true;
                        break;
                    }
                }

                if (!hasValidJapaneseCheckupType)
                {
                    result.Errors.Add("Checkup type should include Japanese standard coding (JLAC10 or MEDIS-DC)");
                }
            }

            // Validate overall assessment coding
            if (OverallAssessment != null && OverallAssessment.Coding?.Any() == true)
            {
                bool hasValidAssessmentCoding = false;
                foreach (var coding in OverallAssessment.Coding)
                {
                    if (IsValidJapaneseAssessmentCode(coding))
                    {
                        hasValidAssessmentCoding = true;
                        break;
                    }
                }

                if (!hasValidAssessmentCoding)
                {
                    result.Errors.Add("Overall assessment should include Japanese standard health assessment coding");
                }
            }

            // Validate Japanese text lengths
            if (!string.IsNullOrWhiteSpace(CheckupPurpose) && ContainsJapaneseCharacters(CheckupPurpose))
            {
                if (CheckupPurpose.Length > 300)
                {
                    result.Errors.Add("Japanese checkup purpose should not exceed 300 characters");
                }
            }

            if (!string.IsNullOrWhiteSpace(Recommendations) && ContainsJapaneseCharacters(Recommendations))
            {
                if (Recommendations.Length > 1500)
                {
                    result.Errors.Add("Japanese recommendations should not exceed 1500 characters");
                }
            }
        }

        /// <summary>
        /// Validates required examinations based on checkup type
        /// </summary>
        private void ValidateRequiredExaminationsByType(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            var checkupTypeText = CheckupType?.Text?.ToLower() ?? "";

            // Annual health checkup requirements (法定健診)
            if (checkupTypeText.Contains("annual") || checkupTypeText.Contains("定期") || checkupTypeText.Contains("年次"))
            {
                ValidateAnnualCheckupRequirements(result);
            }

            // Pre-employment checkup requirements (雇入時健診)
            if (checkupTypeText.Contains("pre-employment") || checkupTypeText.Contains("雇入") || checkupTypeText.Contains("採用時"))
            {
                ValidatePreEmploymentCheckupRequirements(result);
            }

            // Occupational health checkup requirements (職業性健診)
            if (checkupTypeText.Contains("occupational") || checkupTypeText.Contains("職業性") || checkupTypeText.Contains("特殊"))
            {
                ValidateOccupationalCheckupRequirements(result);
            }

            // Executive checkup requirements (人間ドック)
            if (checkupTypeText.Contains("executive") || checkupTypeText.Contains("人間ドック") || checkupTypeText.Contains("総合"))
            {
                ValidateExecutiveCheckupRequirements(result);
            }
        }

        /// <summary>
        /// Validates health assessment coding systems
        /// </summary>
        private void ValidateHealthAssessmentCoding(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate vital signs coding
            foreach (var vitalSignRef in VitalSigns)
            {
                if (!string.IsNullOrWhiteSpace(vitalSignRef.Reference) &&
                    !vitalSignRef.Reference.StartsWith("Observation/"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Vital signs must reference Observation resources");
                }
            }

            // Validate laboratory results coding
            foreach (var labRef in LaboratoryResults)
            {
                if (!string.IsNullOrWhiteSpace(labRef.Reference) &&
                    !labRef.Reference.StartsWith("Observation/") &&
                    !labRef.Reference.StartsWith("DiagnosticReport/"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Laboratory results must reference Observation or DiagnosticReport resources");
                }
            }

            // Validate risk factors coding
            foreach (var riskFactor in RiskFactors)
            {
                if (riskFactor.Coding?.Any() == true)
                {
                    bool hasValidRiskFactorCoding = riskFactor.Coding.Any(IsValidJapaneseRiskFactorCode);
                    if (!hasValidRiskFactorCoding)
                    {
                        result.Errors.Add($"Risk factor '{riskFactor.Text}' should include Japanese standard coding");
                    }
                }
            }
        }

        /// <summary>
        /// Validates occupational health compliance
        /// </summary>
        private void ValidateOccupationalHealthCompliance(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Check certification status validity
            if (!string.IsNullOrWhiteSpace(CertificationStatus))
            {
                var validCertificationStatuses = new[]
                {
                    "fit for work", "restricted", "unfit", "requires evaluation",
                    "就業可", "就業制限", "就業不可", "要精査" // Japanese equivalents
                };

                if (!validCertificationStatuses.Contains(CertificationStatus.ToLower()))
                {
                    result.Errors.Add($"Certification status '{CertificationStatus}' should use standard Japanese or English terms");
                }
            }

            // Validate restrictions format
            foreach (var restriction in Restrictions)
            {
                if (ContainsJapaneseCharacters(restriction) && restriction.Length > 200)
                {
                    result.Errors.Add("Japanese restriction descriptions should not exceed 200 characters");
                }
            }

            // Check follow-up requirements compliance
            if (FollowUpRequired && !FollowUpDate.HasValue)
            {
                result.IsValid = false;
                result.Errors.Add("Follow-up date is required when follow-up is indicated");
            }

            // Validate examining physician reference
            if (ExaminingPhysician != null && !string.IsNullOrWhiteSpace(ExaminingPhysician.Reference))
            {
                if (!ExaminingPhysician.Reference.StartsWith("Practitioner/") &&
                    !ExaminingPhysician.Reference.StartsWith("PractitionerRole/"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Examining physician must reference Practitioner or PractitionerRole");
                }
            }
        }

        // Required examinations validation methods
        private void ValidateAnnualCheckupRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Japanese annual checkup must include basic vital signs
            if (!VitalSigns.Any())
            {
                result.Errors.Add("Annual checkup must include vital signs measurements");
            }

            // Should include basic laboratory tests
            if (!LaboratoryResults.Any())
            {
                result.Errors.Add("Annual checkup should include basic laboratory tests");
            }

            // Physical examination is required
            if (!PhysicalExamFindings.Any())
            {
                result.Errors.Add("Annual checkup must include physical examination findings");
            }
        }

        private void ValidatePreEmploymentCheckupRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Pre-employment checkup must have overall assessment
            if (OverallAssessment == null || string.IsNullOrWhiteSpace(OverallAssessment.Text))
            {
                result.IsValid = false;
                result.Errors.Add("Pre-employment checkup must include overall health assessment");
            }

            // Must include fitness for work certification
            if (string.IsNullOrWhiteSpace(CertificationStatus))
            {
                result.Errors.Add("Pre-employment checkup must include work fitness certification");
            }
        }

        private void ValidateOccupationalCheckupRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Occupational checkup must include specific examinations based on workplace hazards
            if (!PhysicalExamFindings.Any() && !LaboratoryResults.Any())
            {
                result.Errors.Add("Occupational checkup must include relevant health examinations");
            }

            // Must document any occupational health restrictions
            if (string.IsNullOrWhiteSpace(CertificationStatus))
            {
                result.Errors.Add("Occupational checkup must include work capability assessment");
            }
        }

        private void ValidateExecutiveCheckupRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Executive checkup should be comprehensive
            if (!VitalSigns.Any() || !LaboratoryResults.Any() || !ImagingResults.Any())
            {
                result.Errors.Add("Executive checkup should include comprehensive examinations (vitals, labs, imaging)");
            }

            // Should include lifestyle recommendations
            if (string.IsNullOrWhiteSpace(Recommendations))
            {
                result.Errors.Add("Executive checkup should include health recommendations");
            }
        }

        // Validation helper methods
        private bool IsValidJapaneseCheckupTypeCode(Coding coding)
        {
            if (coding?.System == null) return false;

            var japaneseCheckupCodeSystems = new[]
            {
                "http://jpfhir.jp/fhir/core/CodeSystem/JLAC10",
                "http://jpfhir.jp/fhir/core/CodeSystem/MEDIS-DC",
                "http://jpfhir.jp/fhir/core/CodeSystem/checkup-type-jp",
                Constants.JpClinsConstants.CodingSystems.LOINC,
                Constants.JpClinsConstants.CodingSystems.SNOMED
            };

            return japaneseCheckupCodeSystems.Contains(coding.System);
        }

        private bool IsValidJapaneseAssessmentCode(Coding coding)
        {
            if (coding?.System == null) return false;

            var japaneseAssessmentCodeSystems = new[]
            {
                "http://jpfhir.jp/fhir/core/CodeSystem/health-assessment-jp",
                "http://jpfhir.jp/fhir/core/CodeSystem/MEDIS-DC",
                Constants.JpClinsConstants.CodingSystems.LOINC,
                Constants.JpClinsConstants.CodingSystems.SNOMED
            };

            return japaneseAssessmentCodeSystems.Contains(coding.System);
        }

        private bool IsValidJapaneseRiskFactorCode(Coding coding)
        {
            if (coding?.System == null) return false;

            var japaneseRiskFactorCodeSystems = new[]
            {
                "http://jpfhir.jp/fhir/core/CodeSystem/risk-factor-jp",
                Constants.JpClinsConstants.CodingSystems.SNOMED,
                Constants.JpClinsConstants.CodingSystems.LOINC
            };

            return japaneseRiskFactorCodeSystems.Contains(coding.System);
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
        /// Creates FHIR Bundle for eCheckup document
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
                    Profile = new[] { "http://jpfhir.jp/fhir/clins/StructureDefinition/JP_Bundle_eCheckup" }
                }
            };

            // Create Composition resource
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Status = CompositionStatus.Final,
                Type = new CodeableConcept("http://loinc.org", "68604-8", "Health assessment and plan of care"),
                Subject = PatientReference,
                Date = CheckupDate.ToString("yyyy-MM-dd"),
                Author = new List<ResourceReference> { ExaminingPhysician },
                Title = "Health Checkup Report"
            };

            // Add composition sections
            var sections = new List<Composition.SectionComponent>
            {
                new Composition.SectionComponent
                {
                    Title = "Checkup Overview",
                    Code = new CodeableConcept("http://loinc.org", "68604-8", "Health assessment"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Checkup Date: {CheckupDate:yyyy-MM-dd}<br/>Type: {CheckupType.Text}<br/>Purpose: {CheckupPurpose}</div>"
                    }
                },
                new Composition.SectionComponent
                {
                    Title = "Overall Assessment",
                    Code = new CodeableConcept("http://loinc.org", "51848-0", "Assessment plan"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">Overall Assessment: {OverallAssessment.Text}</div>"
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(Recommendations))
            {
                sections.Add(new Composition.SectionComponent
                {
                    Title = "Recommendations",
                    Code = new CodeableConcept("http://loinc.org", "18776-5", "Plan of care"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{Recommendations}</div>"
                    }
                });
            }

            composition.Section = sections;

            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:{composition.Id}",
                Resource = composition
            });

            // Add referenced resources to the bundle
            AddReferencedResources(bundle);

            return bundle;
        }

        /// <summary>
        /// Adds referenced FHIR resources to the bundle based on document content
        /// </summary>
        private void AddReferencedResources(Bundle bundle)
        {
            // Add Patient resource
            if (PatientReference?.Reference != null)
            {
                var patient = CreatePatientResource();
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = PatientReference.Reference.StartsWith("urn:uuid:") ? PatientReference.Reference : $"urn:uuid:{patient.Id}",
                    Resource = patient
                });
            }

            // Add examining physician
            if (ExaminingPhysician?.Reference != null)
            {
                var practitioner = CreatePractitionerResource();
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = ExaminingPhysician.Reference.StartsWith("urn:uuid:") ? ExaminingPhysician.Reference : $"urn:uuid:{practitioner.Id}",
                    Resource = practitioner
                });
            }

            // Add organization
            if (OrganizationReference?.Reference != null)
            {
                var organization = CreateOrganizationResource();
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = OrganizationReference.Reference.StartsWith("urn:uuid:") ? OrganizationReference.Reference : $"urn:uuid:{organization.Id}",
                    Resource = organization
                });
            }

            // Add checkup encounter
            if (EncounterReference?.Reference != null)
            {
                var encounter = CreateCheckupEncounter();
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = EncounterReference.Reference.StartsWith("urn:uuid:") ? EncounterReference.Reference : $"urn:uuid:{encounter.Id}",
                    Resource = encounter
                });
            }

            // Add input model resources
            foreach (var conditionInput in ConditionsInput)
            {
                var condition = CreateConditionFromInput(conditionInput);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{condition.Id}",
                    Resource = condition
                });
            }

            foreach (var observationInput in ObservationsInput)
            {
                var observation = CreateObservationFromInput(observationInput);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{observation.Id}",
                    Resource = observation
                });
            }

            foreach (var medicationInput in MedicationsInput)
            {
                var medicationRequest = CreateMedicationRequestFromInput(medicationInput);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{medicationRequest.Id}",
                    Resource = medicationRequest
                });
            }

            foreach (var allergyInput in AllergiesInput)
            {
                var allergy = CreateAllergyIntoleranceFromInput(allergyInput);
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{allergy.Id}",
                    Resource = allergy
                });
            }

            // Add overall assessment as Observation
            if (OverallAssessment != null)
            {
                var assessmentObs = CreateOverallAssessmentObservation();
                bundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:{assessmentObs.Id}",
                    Resource = assessmentObs
                });
            }
        }

        // Helper methods to create FHIR resources
        private Patient CreatePatientResource()
        {
            return new Patient
            {
                Id = FhirHelper.GenerateUniqueId("Patient"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Patient" }
                },
                Active = true,
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Text = PatientReference?.Display ?? "Patient Name"
                    }
                }
            };
        }

        private Practitioner CreatePractitionerResource()
        {
            return new Practitioner
            {
                Id = FhirHelper.GenerateUniqueId("Practitioner"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Practitioner" }
                },
                Active = true,
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Text = ExaminingPhysician?.Display ?? "Examining Physician"
                    }
                }
            };
        }

        private Organization CreateOrganizationResource()
        {
            return new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Organization" }
                },
                Active = true,
                Name = OrganizationReference?.Display ?? "Healthcare Organization"
            };
        }

        private Encounter CreateCheckupEncounter()
        {
            return new Encounter
            {
                Id = FhirHelper.GenerateUniqueId("Encounter"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Encounter" }
                },
                Status = Encounter.EncounterStatus.Finished,
                Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
                Type = new List<CodeableConcept> { CheckupType },
                Subject = PatientReference,
                Period = new Period
                {
                    Start = CheckupDate.ToString("yyyy-MM-dd")
                }
            };
        }

        private Condition CreateConditionFromInput(ConditionInputModel input)
        {
            return new Condition
            {
                Id = FhirHelper.GenerateUniqueId("Condition"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Condition" }
                },
                ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", input.ClinicalStatus),
                VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-ver-status", input.VerificationStatus),
                Code = new CodeableConcept
                {
                    Text = input.ConditionName
                },
                Subject = PatientReference
            };
        }

        private Observation CreateObservationFromInput(ObservationInputModel input)
        {
            return new Observation
            {
                Id = FhirHelper.GenerateUniqueId("Observation"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Observation_VitalSigns" }
                },
                Status = ObservationStatus.Final,
                Category = new List<CodeableConcept>
                {
                    new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "vital-signs")
                },
                Code = new CodeableConcept
                {
                    Text = input.ObservationName
                },
                Subject = PatientReference,
                Effective = new FhirDateTime(CheckupDate),
                Value = new FhirString(input.ValueString ?? input.ValueQuantity?.ToString())
            };
        }

        private MedicationRequest CreateMedicationRequestFromInput(MedicationRequestInputModel input)
        {
            return new MedicationRequest
            {
                Id = FhirHelper.GenerateUniqueId("MedicationRequest"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_MedicationRequest" }
                },
                Status = MedicationRequest.MedicationrequestStatus.Active,
                Intent = MedicationRequest.MedicationRequestIntent.Order,
                Subject = PatientReference,
                Medication = new CodeableConcept
                {
                    Text = input.MedicationName
                }
            };
        }

        private AllergyIntolerance CreateAllergyIntoleranceFromInput(AllergyIntoleranceInputModel input)
        {
            return new AllergyIntolerance
            {
                Id = FhirHelper.GenerateUniqueId("AllergyIntolerance"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_AllergyIntolerance" }
                },
                ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical", input.ClinicalStatus),
                VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/allergyintolerance-verification", input.VerificationStatus),
                Patient = PatientReference,
                Code = new CodeableConcept
                {
                    Text = input.SubstanceName
                }
            };
        }

        private Observation CreateOverallAssessmentObservation()
        {
            return new Observation
            {
                Id = FhirHelper.GenerateUniqueId("Observation"),
                Meta = new Meta
                {
                    Profile = new[] { "http://jpfhir.jp/fhir/core/StructureDefinition/JP_Observation_Common" }
                },
                Status = ObservationStatus.Final,
                Category = new List<CodeableConcept>
                {
                    new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "exam")
                },
                Code = new CodeableConcept("http://loinc.org", "72133-2", "Health assessment"),
                Subject = PatientReference,
                Effective = new FhirDateTime(CheckupDate),
                Value = OverallAssessment,
                Note = !string.IsNullOrWhiteSpace(Recommendations)
                    ? new List<Annotation> { new Annotation { Text = Recommendations } }
                    : null
            };
        }
    }
}