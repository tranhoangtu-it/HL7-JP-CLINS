using HL7_JP_CLINS_Core.FhirModels;
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
        public Reference ReferredToOrganization { get; set; } = new Reference();

        [JsonProperty("referredToPractitioner")]
        public Reference? ReferredToPractitioner { get; set; }

        [JsonProperty("encounter")]
        [Required]
        public Reference EncounterReference { get; set; } = new Reference();

        [JsonProperty("serviceRequested")]
        [Required]
        public List<CodeableConcept> ServiceRequested { get; set; } = new List<CodeableConcept>();

        [JsonProperty("clinicalNotes")]
        [MaxLength(5000)]
        public string? ClinicalNotes { get; set; }

        [JsonProperty("relevantHistory")]
        public string? RelevantHistory { get; set; }

        [JsonProperty("currentConditions")]
        public List<Reference> CurrentConditions { get; set; } = new List<Reference>();

        [JsonProperty("currentMedications")]
        public List<Reference> CurrentMedications { get; set; } = new List<Reference>();

        [JsonProperty("allergies")]
        public List<Reference> Allergies { get; set; } = new List<Reference>();

        [JsonProperty("vitalSigns")]
        public List<Reference> VitalSigns { get; set; } = new List<Reference>();

        [JsonProperty("attachments")]
        public List<DocumentReference> Attachments { get; set; } = new List<DocumentReference>();

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

        /// <summary>
        /// Gets the document type identifier
        /// </summary>
        public override string DocumentType => "eReferral";

        /// <summary>
        /// Gets the JP-CLINS profile URL
        /// </summary>
        public override string ProfileUrl => "http://jpfhir.jp/fhir/clins/StructureDefinition/JP_Bundle_eReferral";

        public EReferralDocument()
        {
            Id = FhirHelper.GenerateUniqueId("EReferral");
        }

        /// <summary>
        /// Validates eReferral specific requirements
        /// </summary>
        protected override void ValidateJpClinsRules(HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            base.ValidateJpClinsRules(result);

            // eReferral specific validation
            if (string.IsNullOrWhiteSpace(ReferralReason))
            {
                result.AddError("Referral reason is required for eReferral documents");
            }

            if (ServiceRequested == null || !ServiceRequested.Any())
            {
                result.AddError("At least one service requested is required for eReferral documents");
            }

            // Validate service requested coding
            ValidateServiceRequestedCoding(result);

            // Validate Japanese organization identifiers
            ValidateJapaneseOrganizationIdentifiers(result);

            // Validate Japanese referral requirements
            ValidateJapaneseReferralRequirements(result);

            // Validate urgency level format
            ValidateUrgencyLevelFormat(result);
        }

        /// <summary>
        /// Validates service requested coding against Japanese standards
        /// </summary>
        private void ValidateServiceRequestedCoding(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            foreach (var service in ServiceRequested)
            {
                if (service.Coding == null || !service.Coding.Any())
                {
                    result.AddWarning("Service requested should include proper coding");
                    continue;
                }

                foreach (var coding in service.Coding)
                {
                    if (!IsValidJapaneseServiceCode(coding))
                    {
                        result.AddWarning($"Service code '{coding.Code}' may not follow Japanese coding standards");
                    }
                }
            }
        }

        /// <summary>
        /// Validates Japanese organization identifiers
        /// </summary>
        private void ValidateJapaneseOrganizationIdentifiers(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Validate referring organization
            if (ReferredToOrganization?.Identifier?.Value != null)
            {
                var institutionId = ReferredToOrganization.Identifier.Value;
                if (!IsValidJapaneseMedicalInstitutionId(institutionId))
                {
                    result.AddWarning($"Referring organization ID '{institutionId}' may not be valid");
                }
            }
        }

        /// <summary>
        /// Validates Japanese referral requirements
        /// </summary>
        private void ValidateJapaneseReferralRequirements(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            // Check for Japanese text in referral reason
            if (!string.IsNullOrWhiteSpace(ReferralReason) && !ContainsJapaneseCharacters(ReferralReason))
            {
                result.AddWarning("Referral reason should include Japanese text for better understanding");
            }

            // Check for Japanese text in clinical notes
            if (!string.IsNullOrWhiteSpace(ClinicalNotes) && !ContainsJapaneseCharacters(ClinicalNotes))
            {
                result.AddWarning("Clinical notes should include Japanese text for better understanding");
            }

            // Validate that referral reason is not too short
            if (ReferralReason?.Length < 10)
            {
                result.AddWarning("Referral reason should be more descriptive (minimum 10 characters)");
            }
        }

        /// <summary>
        /// Validates urgency level format
        /// </summary>
        private void ValidateUrgencyLevelFormat(HL7_JP_CLINS_Core.Models.Base.ValidationResult result)
        {
            var validUrgencyLevels = new[] { "routine", "urgent", "asap", "stat" };
            if (!validUrgencyLevels.Contains(Urgency?.ToLowerInvariant()))
            {
                result.AddWarning($"Urgency level '{Urgency}' should be one of: {string.Join(", ", validUrgencyLevels)}");
            }
        }

        /// <summary>
        /// Checks if coding follows Japanese service code standards
        /// </summary>
        private bool IsValidJapaneseServiceCode(Coding coding)
        {
            if (coding == null || string.IsNullOrWhiteSpace(coding.Code))
                return false;

            // Check against Japanese coding systems
            var japaneseSystems = new[]
            {
                "http://jpfhir.jp/fhir/core/CodeSystem/JP_ProcedureCodes",
                "http://jpfhir.jp/fhir/core/CodeSystem/JP_ServiceCodes",
                "http://terminology.hl7.org/CodeSystem/v3-ActCode"
            };

            return japaneseSystems.Contains(coding.System?.ToString());
        }

        /// <summary>
        /// Validates Japanese medical institution ID format
        /// </summary>
        private bool IsValidJapaneseMedicalInstitutionId(string? institutionId)
        {
            if (string.IsNullOrWhiteSpace(institutionId))
                return false;

            // Japanese medical institution IDs are typically 7-10 digits
            return institutionId.Length >= 7 && institutionId.Length <= 10 && institutionId.All(char.IsDigit);
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
        public override Bundle ToFhirBundle()
        {
            var bundle = new Bundle
            {
                Id = FhirHelper.GenerateUniqueId("Bundle"),
                Type = "document",
                Timestamp = DateTimeOffset.UtcNow
            };

            // Create Composition
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Status = "final",
                Type = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri("http://loinc.org"),
                            Code = "18761-7",
                            Display = "Provider-unspecified referral note"
                        }
                    }
                },
                Subject = PatientReference,
                Author = new List<Reference> { AuthorReference },
                Title = "電子紹介状 (eReferral)"
            };

            bundle.Entry.Add(new BundleEntry { Resource = composition });

            // Add referenced resources
            AddReferencedResources(bundle);

            return bundle;
        }

        /// <summary>
        /// Adds all referenced resources to the bundle
        /// </summary>
        private void AddReferencedResources(Bundle bundle)
        {
            // Add Patient
            var patient = CreatePatientResource();
            bundle.Entry.Add(new BundleEntry { Resource = patient });

            // Add Author (Practitioner)
            var practitioner = CreatePractitionerResource();
            bundle.Entry.Add(new BundleEntry { Resource = practitioner });

            // Add Referring Organization
            var organization = CreateOrganizationResource();
            bundle.Entry.Add(new BundleEntry { Resource = organization });

            // Add Referred To Organization
            var referredToOrg = CreateReferredToOrganizationResource();
            bundle.Entry.Add(new BundleEntry { Resource = referredToOrg });

            // Add Encounter
            var encounter = CreateEncounterResource();
            bundle.Entry.Add(new BundleEntry { Resource = encounter });

            // Add Conditions from input models
            foreach (var conditionInput in ConditionsInput)
            {
                var condition = CreateConditionFromInput(conditionInput);
                bundle.Entry.Add(new BundleEntry { Resource = condition });
            }

            // Add Observations from input models
            foreach (var observationInput in ObservationsInput)
            {
                var observation = CreateObservationFromInput(observationInput);
                bundle.Entry.Add(new BundleEntry { Resource = observation });
            }

            // Add Medications from input models
            foreach (var medicationInput in MedicationsInput)
            {
                var medication = CreateMedicationRequestFromInput(medicationInput);
                bundle.Entry.Add(new BundleEntry { Resource = medication });
            }

            // Add Allergies from input models
            foreach (var allergyInput in AllergiesInput)
            {
                var allergy = CreateAllergyIntoleranceFromInput(allergyInput);
                bundle.Entry.Add(new BundleEntry { Resource = allergy });
            }

            // Add ServiceRequest
            var serviceRequest = CreateServiceRequestResource();
            bundle.Entry.Add(new BundleEntry { Resource = serviceRequest });
        }

        // Helper methods for creating FHIR resources
        private Patient CreatePatientResource()
        {
            return new Patient
            {
                Id = FhirHelper.GenerateUniqueId("Patient"),
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Family = "Patient",
                        Given = new List<string> { "Name" }
                    }
                }
            };
        }

        private Practitioner CreatePractitionerResource()
        {
            return new Practitioner
            {
                Id = FhirHelper.GenerateUniqueId("Practitioner"),
                Name = new List<HumanName>
                {
                    new HumanName
                    {
                        Family = "Practitioner",
                        Given = new List<string> { "Name" }
                    }
                }
            };
        }

        private Organization CreateOrganizationResource()
        {
            return new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = "Referring Organization"
            };
        }

        private Organization CreateReferredToOrganizationResource()
        {
            return new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = "Referred To Organization"
            };
        }

        private Encounter CreateEncounterResource()
        {
            return new Encounter
            {
                Id = FhirHelper.GenerateUniqueId("Encounter"),
                Status = "finished",
                Subject = PatientReference
            };
        }

        private Condition CreateConditionFromInput(ConditionInputModel input)
        {
            return new Condition
            {
                Id = FhirHelper.GenerateUniqueId("Condition"),
                Code = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri(input.CodeSystem ?? "http://snomed.info/sct"),
                            Code = input.Code,
                            Display = input.Display
                        }
                    }
                },
                Subject = PatientReference,
                ClinicalStatus = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri("http://terminology.hl7.org/CodeSystem/condition-clinical"),
                            Code = "active"
                        }
                    }
                }
            };
        }

        private Observation CreateObservationFromInput(ObservationInputModel input)
        {
            return new Observation
            {
                Id = FhirHelper.GenerateUniqueId("Observation"),
                Status = "final",
                Code = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri(input.CodeSystem ?? "http://loinc.org"),
                            Code = input.Code,
                            Display = input.Display
                        }
                    }
                },
                Subject = PatientReference,
                ValueString = input.Value
            };
        }

        private MedicationRequest CreateMedicationRequestFromInput(MedicationRequestInputModel input)
        {
            return new MedicationRequest
            {
                Id = FhirHelper.GenerateUniqueId("MedicationRequest"),
                Status = "active",
                Intent = "order",
                Medication = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri("http://jpfhir.jp/fhir/core/CodeSystem/JP_MedicationCode"),
                            Code = input.MedicationCode,
                            Display = input.MedicationName
                        }
                    }
                },
                Subject = PatientReference
            };
        }

        private AllergyIntolerance CreateAllergyIntoleranceFromInput(AllergyIntoleranceInputModel input)
        {
            return new AllergyIntolerance
            {
                Id = FhirHelper.GenerateUniqueId("AllergyIntolerance"),
                ClinicalStatus = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = new Uri("http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical"),
                            Code = "active"
                        }
                    }
                },
                Code = new CodeableConcept
                {
                    Text = input.SubstanceName
                },
                Patient = PatientReference
            };
        }

        private ServiceRequest CreateServiceRequestResource()
        {
            return new ServiceRequest
            {
                Id = FhirHelper.GenerateUniqueId("ServiceRequest"),
                Status = "active",
                Intent = "order",
                Subject = PatientReference,
                Requester = AuthorReference,
                Code = ServiceRequested.FirstOrDefault(),
                ReasonCode = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = ReferralReason }
                }
            };
        }
    }
}