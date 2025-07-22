using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Base;
using HL7_JP_CLINS_Tranforms.Interfaces;
using HL7_JP_CLINS_Tranforms.Mappers;
using Newtonsoft.Json.Linq;

namespace HL7_JP_CLINS_Tranforms.Transformers
{
    /// <summary>
    /// Transformer for converting hospital eReferral data to JP-CLINS compliant FHIR Bundle
    /// Handles electronic referral documents according to JP-CLINS v1.11.0 implementation guide
    /// </summary>
    public class EReferralTransformer : BaseTransformer<dynamic>
    {
        /// <summary>
        /// JP-CLINS document type identifier for eReferral
        /// </summary>
        public override string DocumentType => "eReferral";

        /// <summary>
        /// JP-CLINS profile URL for eReferral Bundle
        /// </summary>
        public override string ProfileUrl => JpClinsConstants.DocumentProfiles.EReferral;

        /// <summary>
        /// Creates the main Composition resource for eReferral document
        /// </summary>
        /// <param name="input">eReferral input data from hospital systems</param>
        /// <returns>FHIR Composition resource</returns>
        protected override Composition CreateComposition(dynamic input)
        {
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Condition }, // eReferral composition profile
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Status = CompositionStatus.Final,
                Type = CreateJapaneseCodeableConcept(
                    code: "18761-7",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Provider-unspecified referral note"),

                // Patient subject
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input)),

                // Encounter context
                Encounter = input.encounterId != null
                    ? FhirHelper.CreateReference("Encounter", input.encounterId.ToString())
                    : null,

                // Document date
                DateElement = new FhirDateTime(input.referralDate ?? DateTime.UtcNow),

                // Author (referring practitioner)
                Author = new List<ResourceReference>
                {
                    FhirHelper.CreateReference("Practitioner", GetPractitionerId(input))
                },

                // Document title
                Title = "電子紹介状 (eReferral)"
            };

            // Create composition sections for eReferral
            composition.Section = CreateEReferralSections(input);

            return composition;
        }

        /// <summary>
        /// Transforms input data into all required FHIR resources
        /// </summary>
        /// <param name="input">eReferral input data</param>
        /// <returns>List of FHIR resources</returns>
        protected override List<Resource> TransformToResources(dynamic input)
        {
            var resources = new List<Resource>();

            try
            {
                // 1. Transform Patient resource
                if (input.patient != null)
                {
                    var patient = PatientMapper.MapToPatient(input.patient);
                    resources.Add(patient);
                }

                // 2. Transform referring Practitioner
                if (input.referringPractitioner != null)
                {
                    var practitioner = PractitionerMapper.MapToPractitioner(input.referringPractitioner);
                    resources.Add(practitioner);
                }

                // 3. Transform referring Organization
                if (input.referringOrganization != null)
                {
                    var organization = TransformOrganization(input.referringOrganization, "referring");
                    resources.Add(organization);
                }

                // 4. Transform receiving Organization (if specified)
                if (input.receivingOrganization != null)
                {
                    var organization = TransformOrganization(input.receivingOrganization, "receiving");
                    resources.Add(organization);
                }

                // 5. Transform Encounter resource
                if (input.encounter != null)
                {
                    var encounter = TransformEncounter(input.encounter);
                    resources.Add(encounter);
                }

                // 6. Transform Clinical Information Resources (Core 5 Information)
                resources.AddRange(TransformClinicalResources(input));

                // 7. Transform ServiceRequest (what is being requested)
                if (input.serviceRequested != null)
                {
                    var serviceRequest = TransformServiceRequest(input);
                    resources.Add(serviceRequest);
                }

                // 8. Transform DocumentReference for attachments (if any)
                if (input.attachments != null)
                {
                    var attachments = TransformAttachments(input.attachments);
                    resources.AddRange(attachments);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform eReferral resources: {ex.Message}", ex);
            }

            return resources;
        }

        /// <summary>
        /// Creates composition sections specific to eReferral documents
        /// </summary>
        /// <param name="input">eReferral input data</param>
        /// <returns>List of composition sections</returns>
        private List<Composition.SectionComponent> CreateEReferralSections(dynamic input)
        {
            var sections = new List<Composition.SectionComponent>();

            // Referral reason section (紹介理由)
            var reasonSection = new Composition.SectionComponent
            {
                Title = "紹介理由 (Referral Reason)",
                Code = CreateJapaneseCodeableConcept(
                    code: "42349-1",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Reason for referral"),
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.referralReason ?? "紹介理由が記載されています"}</div>"
                }
            };
            sections.Add(reasonSection);

            // Current medications section (現在の処方)
            if (input.medications != null && HasMedications(input.medications))
            {
                var medicationsSection = new Composition.SectionComponent
                {
                    Title = "現在の処方 (Current Medications)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "10160-0",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of Medication use Narrative"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(medicationsSection);
            }

            // Allergies and adverse reactions section (アレルギー・副作用歴)
            if (input.allergies != null && HasAllergies(input.allergies))
            {
                var allergiesSection = new Composition.SectionComponent
                {
                    Title = "アレルギー・副作用歴 (Allergies and Adverse Reactions)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "48765-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Allergies and adverse reactions Document"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(allergiesSection);
            }

            // Problem list section (現在の問題リスト)
            if (input.conditions != null && HasConditions(input.conditions))
            {
                var problemsSection = new Composition.SectionComponent
                {
                    Title = "現在の問題リスト (Problem List)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "11450-4",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Problem list"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(problemsSection);
            }

            // Laboratory results section (検査結果)
            if (input.labResults != null && HasObservations(input.labResults))
            {
                var labSection = new Composition.SectionComponent
                {
                    Title = "検査結果 (Laboratory Results)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "30954-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Relevant diagnostic tests/laboratory data"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(labSection);
            }

            // Service request section (依頼サービス)
            var serviceSection = new Composition.SectionComponent
            {
                Title = "依頼サービス (Requested Services)",
                Code = CreateJapaneseCodeableConcept(
                    code: "62387-6",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Interventions provided"),
                Entry = new List<ResourceReference>()
            };
            sections.Add(serviceSection);

            return sections;
        }

        /// <summary>
        /// Transforms clinical resources (Core 5 Information) from input data
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of clinical FHIR resources</returns>
        private List<Resource> TransformClinicalResources(dynamic input)
        {
            var resources = new List<Resource>();

            try
            {
                // Transform Allergies/Intolerances
                if (input.allergies != null)
                {
                    var allergiesArray = input.allergies as System.Collections.IEnumerable;
                    if (allergiesArray != null)
                    {
                        foreach (var allergyData in allergiesArray)
                        {
                            var allergyInput = CreateAllergyInputModel(allergyData);
                            var allergyResource = ResourceTransformHelper.TransformAllergyIntolerance(allergyInput);
                            resources.Add(allergyResource);
                        }
                    }
                }

                // Transform Conditions/Diagnoses
                if (input.conditions != null)
                {
                    var conditionsArray = input.conditions as System.Collections.IEnumerable;
                    if (conditionsArray != null)
                    {
                        foreach (var conditionData in conditionsArray)
                        {
                            var conditionInput = CreateConditionInputModel(conditionData);
                            var conditionResource = ResourceTransformHelper.TransformCondition(conditionInput);
                            resources.Add(conditionResource);
                        }
                    }
                }

                // Transform Observations/Lab Results
                if (input.observations != null)
                {
                    var observationsArray = input.observations as System.Collections.IEnumerable;
                    if (observationsArray != null)
                    {
                        foreach (var observationData in observationsArray)
                        {
                            var observationInput = CreateObservationInputModel(observationData);
                            var observationResource = ResourceTransformHelper.TransformObservation(observationInput);
                            resources.Add(observationResource);
                        }
                    }
                }

                // Transform Medications
                if (input.medications != null)
                {
                    var medicationsArray = input.medications as System.Collections.IEnumerable;
                    if (medicationsArray != null)
                    {
                        foreach (var medicationData in medicationsArray)
                        {
                            var medicationInput = CreateMedicationInputModel(medicationData);
                            var medicationResource = ResourceTransformHelper.TransformMedicationRequest(medicationInput);
                            resources.Add(medicationResource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform clinical resources: {ex.Message}", ex);
            }

            return resources;
        }

        /// <summary>
        /// Transforms ServiceRequest for the referral
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>ServiceRequest resource</returns>
        private ServiceRequest TransformServiceRequest(dynamic input)
        {
            var serviceRequest = new ServiceRequest
            {
                Id = FhirHelper.GenerateUniqueId("ServiceRequest"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Procedure },
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Status = RequestStatus.Active,
                Intent = RequestIntent.Order,
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input)),
                Requester = FhirHelper.CreateReference("Practitioner", GetPractitionerId(input)),
                AuthoredOnElement = new FhirDateTime(input.referralDate ?? DateTime.UtcNow)
            };

            // Service being requested
            if (input.serviceRequested != null)
            {
                serviceRequest.Code = CreateJapaneseCodeableConcept(
                    code: input.serviceRequested.code?.ToString() ?? "consultation",
                    system: JpClinsConstants.CodingSystems.JapanProcedure,
                    display: input.serviceRequested.name?.ToString() ?? "医学的相談");
            }

            // Priority/urgency
            if (input.urgency != null)
            {
                serviceRequest.Priority = input.urgency.ToString().ToLower() switch
                {
                    "routine" => RequestPriority.Routine,
                    "urgent" => RequestPriority.Urgent,
                    "asap" => RequestPriority.Asap,
                    "stat" => RequestPriority.Stat,
                    _ => RequestPriority.Routine
                };
            }

            // Reason for referral
            if (input.referralReason != null)
            {
                serviceRequest.ReasonCode = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = input.referralReason.ToString() }
                };
            }

            return serviceRequest;
        }

        /// <summary>
        /// Document-specific input validation for eReferral
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <param name="result">Validation result to update</param>
        protected override void ValidateSpecificInput(dynamic input, ValidationResult result)
        {
            // Validate required eReferral fields
            if (input.patient == null)
            {
                result.AddError("Patient information is required for eReferral");
            }

            if (input.referringPractitioner == null)
            {
                result.AddError("Referring practitioner information is required");
            }

            if (string.IsNullOrWhiteSpace(input.referralReason?.ToString()))
            {
                result.AddError("Referral reason is required");
            }

            if (input.serviceRequested == null)
            {
                result.AddError("Service requested information is required");
            }

            // JP-CLINS specific validations
            ValidateJapaneseSpecificFields(input, result);
        }

        /// <summary>
        /// Validates JP-CLINS specific requirements for eReferral
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateJapaneseSpecificFields(dynamic input, ValidationResult result)
        {
            // Validate Japanese medical license numbers
            if (input.referringPractitioner?.medicalLicenseNumber != null)
            {
                var licenseNumber = input.referringPractitioner.medicalLicenseNumber.ToString();
                if (!FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    result.AddError($"Invalid Japanese medical license number: {licenseNumber}");
                }
            }

            // Validate Japanese organization codes
            if (input.referringOrganization?.facilityCode != null)
            {
                var facilityCode = input.referringOrganization.facilityCode.ToString();
                if (!FhirHelper.ValidateJapaneseIdentifier(facilityCode, "facility-code"))
                {
                    result.AddError($"Invalid Japanese facility code: {facilityCode}");
                }
            }

            // Validate service codes follow Japanese standards
            if (input.serviceRequested?.code != null)
            {
                var serviceCode = input.serviceRequested.code.ToString();
                // TODO: Validate against Japanese service code systems (MEDIS-DC, etc.)
                if (string.IsNullOrWhiteSpace(serviceCode))
                {
                    result.AddWarning("Service code should follow Japanese coding standards");
                }
            }
        }

        // Helper methods for extracting IDs
        private string GetPatientId(dynamic input) =>
            input.patient?.patientId?.ToString() ?? FhirHelper.GenerateUniqueId("Patient");

        private string GetPractitionerId(dynamic input) =>
            input.referringPractitioner?.practitionerId?.ToString() ?? FhirHelper.GenerateUniqueId("Practitioner");

        // Helper methods for creating input models
        private AllergyIntoleranceInputModel CreateAllergyInputModel(dynamic allergyData) =>
            new AllergyIntoleranceInputModel
            {
                PatientId = GetPatientId(null),
                SubstanceName = allergyData.substanceName?.ToString() ?? "",
                Category = allergyData.category?.ToString() ?? "medication",
                ReactionSeverity = allergyData.severity?.ToString(),
                // Map other fields as needed
            };

        private ConditionInputModel CreateConditionInputModel(dynamic conditionData) =>
            new ConditionInputModel
            {
                PatientId = GetPatientId(null),
                ConditionName = conditionData.name?.ToString() ?? "",
                ConditionCode = conditionData.code?.ToString(),
                ClinicalStatus = conditionData.status?.ToString() ?? "active",
                // Map other fields as needed
            };

        private ObservationInputModel CreateObservationInputModel(dynamic observationData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = observationData.code?.ToString() ?? "",
                ObservationCodeSystem = observationData.codeSystem?.ToString() ?? JpClinsConstants.CodingSystems.LOINC,
                ObservationName = observationData.name?.ToString() ?? "",
                Status = "final",
                // Map other fields as needed
            };

        private MedicationRequestInputModel CreateMedicationInputModel(dynamic medicationData) =>
            new MedicationRequestInputModel
            {
                PatientId = GetPatientId(null),
                MedicationName = medicationData.name?.ToString() ?? "",
                MedicationCode = medicationData.code?.ToString(),
                RequesterId = GetPractitionerId(null),
                // Map other fields as needed
            };

        // Helper methods for checking data existence
        private bool HasMedications(dynamic medications) =>
            medications is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasAllergies(dynamic allergies) =>
            allergies is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasConditions(dynamic conditions) =>
            conditions is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasObservations(dynamic observations) =>
            observations is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        // Placeholder methods (to be implemented)
        private Organization TransformOrganization(dynamic orgData, string type) =>
            new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = orgData.name?.ToString() ?? $"{type} Organization"
            };

        private Encounter TransformEncounter(dynamic encounterData) =>
            new Encounter
            {
                Id = FhirHelper.GenerateUniqueId("Encounter"),
                Status = Encounter.EncounterStatus.Finished
            };

        private List<DocumentReference> TransformAttachments(dynamic attachments) =>
            new List<DocumentReference>();

        // TODO: JP-CLINS eReferral Implementation Notes:
        // 1. Implement proper Japanese service code validation (MEDIS-DC, JJ1017)
        // 2. Add support for Japanese medical institution hierarchies
        // 3. Include Japanese-specific urgency classifications
        // 4. Support for Japanese insurance approval workflows
        // 5. Add validation for Japanese practitioner qualifications
        // 6. Implement Japanese privacy and consent requirements
        // 7. Support for Japanese diagnostic imaging referrals
        // 8. Include Japanese healthcare quality indicators
        // 9. Add support for Japanese telemedicine referrals
        // 10. Implement Japanese clinical pathway references
    }
}