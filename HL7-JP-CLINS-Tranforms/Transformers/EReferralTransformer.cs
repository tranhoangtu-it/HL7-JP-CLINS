using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.FhirModels.Base;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Base;
using HL7_JP_CLINS_Tranforms.Interfaces;
using HL7_JP_CLINS_Tranforms.Mappers;
using HL7_JP_CLINS_Tranforms.Utilities;
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
                Status = "final",
                Type = CreateJapaneseCodeableConcept(
                    code: "18761-7",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Provider-unspecified referral note"),

                // Patient subject
                Subject = CreateResourceReference(new Patient { Id = GetPatientId(input) }),

                // Document date
                // TODO: Add date field to Composition model

                // Author (referring practitioner)
                Author = new List<Reference>
                {
                    CreateResourceReference(new Practitioner { Id = GetPractitionerId(input) })
                }
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
        protected override List<FhirResource> TransformToResources(dynamic input)
        {
            var resources = new List<FhirResource>();

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

                // 3. Transform clinical resources (conditions, allergies, medications, observations)
                var clinicalResources = TransformClinicalResources(input);
                resources.AddRange(clinicalResources);

                // 4. Transform ServiceRequest (referral request)
                var serviceRequest = TransformServiceRequest(input);
                if (serviceRequest != null)
                {
                    resources.Add(serviceRequest);
                }

                // 5. Transform organizations (referring and receiving facilities)
                if (input.referringOrganization != null)
                {
                    var referringOrg = TransformOrganization(input.referringOrganization, "referring");
                    resources.Add(referringOrg);
                }

                if (input.receivingOrganization != null)
                {
                    var receivingOrg = TransformOrganization(input.receivingOrganization, "receiving");
                    resources.Add(receivingOrg);
                }

                // 6. Transform encounter if present
                if (input.encounter != null)
                {
                    var encounter = TransformEncounter(input.encounter);
                    resources.Add(encounter);
                }

                // 7. Transform attachments if present
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
        /// Creates composition sections for eReferral document
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of composition sections</returns>
        private List<CompositionSection> CreateEReferralSections(dynamic input)
        {
            var sections = new List<CompositionSection>();

            // Chief complaint section
            if (input.chiefComplaint != null)
            {
                sections.Add(new CompositionSection
                {
                    Title = "主訴 (Chief Complaint)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "10154-3",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Chief complaint"),
                    Text = TransformHelper.SafeGetString(input, "chiefComplaint")
                });
            }

            // Present illness section
            if (input.presentIllness != null)
            {
                sections.Add(new CompositionSection
                {
                    Title = "現病歴 (Present Illness)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "10164-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of present illness"),
                    Text = TransformHelper.SafeGetString(input, "presentIllness")
                });
            }

            // Past medical history section
            if (input.pastMedicalHistory != null)
            {
                sections.Add(new CompositionSection
                {
                    Title = "既往歴 (Past Medical History)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "11348-0",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of past illness"),
                    Text = TransformHelper.SafeGetString(input, "pastMedicalHistory")
                });
            }

            // Medications section
            if (HasMedications(input.medications))
            {
                sections.Add(new CompositionSection
                {
                    Title = "投薬情報 (Medications)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "10160-0",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of medication use")
                });
            }

            // Allergies section
            if (HasAllergies(input.allergies))
            {
                sections.Add(new CompositionSection
                {
                    Title = "アレルギー情報 (Allergies)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "48765-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Allergies and adverse reactions")
                });
            }

            // Physical examination section
            if (input.physicalExamination != null)
            {
                sections.Add(new CompositionSection
                {
                    Title = "身体所見 (Physical Examination)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "29545-1",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Physical examination"),
                    Text = TransformHelper.SafeGetString(input, "physicalExamination")
                });
            }

            // Laboratory results section
            if (HasObservations(input.laboratoryResults))
            {
                sections.Add(new CompositionSection
                {
                    Title = "検査結果 (Laboratory Results)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "30954-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Relevant diagnostic tests/laboratory data")
                });
            }

            // Imaging results section
            if (HasObservations(input.imagingResults))
            {
                sections.Add(new CompositionSection
                {
                    Title = "画像検査結果 (Imaging Results)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "18748-4",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Diagnostic imaging study")
                });
            }

            // Assessment and plan section
            if (input.assessmentAndPlan != null)
            {
                sections.Add(new CompositionSection
                {
                    Title = "診断・治療計画 (Assessment and Plan)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "51847-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Assessment and plan"),
                    Text = TransformHelper.SafeGetString(input, "assessmentAndPlan")
                });
            }

            return sections;
        }

        /// <summary>
        /// Transforms clinical resources (conditions, allergies, medications, observations)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of clinical resources</returns>
        private List<FhirResource> TransformClinicalResources(dynamic input)
        {
            var resources = new List<FhirResource>();

            // Transform conditions
            if (HasConditions(input.conditions))
            {
                foreach (var conditionData in input.conditions)
                {
                    var conditionInput = CreateConditionInputModel(conditionData);
                    // TODO: Create Condition resource from input model
                    // var condition = new Condition { ... };
                    // resources.Add(condition);
                }
            }

            // Transform allergies
            if (HasAllergies(input.allergies))
            {
                foreach (var allergyData in input.allergies)
                {
                    var allergyInput = CreateAllergyInputModel(allergyData);
                    // TODO: Create AllergyIntolerance resource from input model
                    // var allergy = new AllergyIntolerance { ... };
                    // resources.Add(allergy);
                }
            }

            // Transform medications
            if (HasMedications(input.medications))
            {
                foreach (var medicationData in input.medications)
                {
                    var medicationInput = CreateMedicationInputModel(medicationData);
                    // TODO: Create MedicationRequest resource from input model
                    // var medication = new MedicationRequest { ... };
                    // resources.Add(medication);
                }
            }

            // Transform observations
            if (HasObservations(input.laboratoryResults))
            {
                foreach (var observationData in input.laboratoryResults)
                {
                    var observationInput = CreateObservationInputModel(observationData);
                    // TODO: Create Observation resource from input model
                    // var observation = new Observation { ... };
                    // resources.Add(observation);
                }
            }

            return resources;
        }

        /// <summary>
        /// Transforms ServiceRequest for referral
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>ServiceRequest resource</returns>
        private ServiceRequest TransformServiceRequest(dynamic input)
        {
            // TODO: Create ServiceRequest model and implement transformation
            // For now, return null as ServiceRequest is not yet implemented
            return null;
        }

        /// <summary>
        /// Validates input data specific to eReferral
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <param name="result">Validation result to populate</param>
        protected override void ValidateSpecificInput(dynamic input, ValidationResult result)
        {
            // Validate required fields for eReferral
            if (input.patient == null)
            {
                result.AddError("Patient information is required for eReferral");
            }

            if (input.referringPractitioner == null)
            {
                result.AddError("Referring practitioner information is required for eReferral");
            }

            if (input.referralDate == null)
            {
                result.AddError("Referral date is required for eReferral");
            }

            // Validate Japanese-specific fields
            ValidateJapaneseSpecificFields(input, result);

            // Validate service codes if present
            if (input.serviceCode != null)
            {
                ValidateJapaneseServiceCodes(TransformHelper.SafeGetString(input, "serviceCode"), result);
            }
        }

        /// <summary>
        /// Validates Japanese-specific fields for eReferral
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateJapaneseSpecificFields(dynamic input, ValidationResult result)
        {
            // Validate referring practitioner medical license number
            if (input.referringPractitioner?.medicalLicenseNumber != null)
            {
                var licenseNumber = TransformHelper.SafeGetString(input.referringPractitioner, "medicalLicenseNumber");
                if (!FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    result.AddWarning($"Invalid medical license number format: {licenseNumber}");
                }
            }

            // Validate patient insurance information
            if (input.patient?.insuranceNumber != null)
            {
                var insuranceNumber = TransformHelper.SafeGetString(input.patient, "insuranceNumber");
                if (string.IsNullOrWhiteSpace(insuranceNumber) || insuranceNumber.Length < 8)
                {
                    result.AddWarning("Insurance number should be at least 8 characters long");
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
                SubstanceName = TransformHelper.SafeGetString(allergyData, "substanceName"),
                Reaction = TransformHelper.SafeGetString(allergyData, "reaction"),
                Severity = TransformHelper.SafeGetString(allergyData, "severity")
            };

        private ConditionInputModel CreateConditionInputModel(dynamic conditionData) =>
            new ConditionInputModel
            {
                Code = TransformHelper.SafeGetString(conditionData, "code"),
                CodeSystem = TransformHelper.SafeGetString(conditionData, "codeSystem"),
                Display = TransformHelper.SafeGetString(conditionData, "display"),
                OnsetDate = TransformHelper.SafeGetDateTime(conditionData, "onsetDate")
            };

        private ObservationInputModel CreateObservationInputModel(dynamic observationData) =>
            new ObservationInputModel
            {
                Code = TransformHelper.SafeGetString(observationData, "code"),
                CodeSystem = TransformHelper.SafeGetString(observationData, "codeSystem"),
                Value = TransformHelper.SafeGetString(observationData, "value"),
                Unit = TransformHelper.SafeGetString(observationData, "unit"),
                EffectiveDate = TransformHelper.SafeGetDateTime(observationData, "effectiveDate")
            };

        private MedicationRequestInputModel CreateMedicationInputModel(dynamic medicationData) =>
            new MedicationRequestInputModel
            {
                MedicationCode = TransformHelper.SafeGetString(medicationData, "medicationCode"),
                MedicationName = TransformHelper.SafeGetString(medicationData, "medicationName"),
                Dosage = TransformHelper.SafeGetString(medicationData, "dosage"),
                Frequency = TransformHelper.SafeGetString(medicationData, "frequency"),
                StartDate = TransformHelper.SafeGetDateTime(medicationData, "startDate")
            };

        // Helper methods for checking data presence
        private bool HasMedications(dynamic medications) =>
            medications != null && ((IEnumerable<dynamic>)medications).Any();

        private bool HasAllergies(dynamic allergies) =>
            allergies != null && ((IEnumerable<dynamic>)allergies).Any();

        private bool HasConditions(dynamic conditions) =>
            conditions != null && ((IEnumerable<dynamic>)conditions).Any();

        private bool HasObservations(dynamic observations) =>
            observations != null && ((IEnumerable<dynamic>)observations).Any();

        // Helper methods for transforming organizations and other resources
        private Organization TransformOrganization(dynamic orgData, string type)
        {
            // TODO: Implement organization transformation
            return new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = TransformHelper.SafeGetString(orgData, "name")
            };
        }

        private Encounter TransformEncounter(dynamic encounterData)
        {
            // TODO: Create Encounter model and implement transformation
            return null;
        }

        private List<DocumentReference> TransformAttachments(dynamic attachments)
        {
            // TODO: Create DocumentReference model and implement transformation
            return new List<DocumentReference>();
        }

        /// <summary>
        /// Validates Japanese service codes for eReferral
        /// </summary>
        /// <param name="serviceCode">Service code to validate</param>
        /// <param name="result">Validation result</param>
        private void ValidateJapaneseServiceCodes(string serviceCode, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(serviceCode))
            {
                result.AddWarning("Service code is empty");
                return;
            }

            // Check various Japanese coding systems
            if (!IsValidMedisDcCode(serviceCode) &&
                !IsValidJJ1017Code(serviceCode) &&
                !IsValidJLAC10ServiceCode(serviceCode) &&
                !IsValidJapaneseDPCCode(serviceCode) &&
                !IsValidLoincCode(serviceCode) &&
                !IsValidSnomedCode(serviceCode))
            {
                result.AddWarning($"Service code '{serviceCode}' does not match known Japanese coding systems");
            }
        }

        private bool IsValidMedisDcCode(string code)
        {
            // MEDIS-DC codes: 7-digit numeric codes
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length == 7 &&
                   code.All(char.IsDigit);
        }

        private bool IsValidJJ1017Code(string code)
        {
            // JJ1017 codes: 7-digit numeric codes for medical procedures
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length == 7 &&
                   code.All(char.IsDigit);
        }

        private bool IsValidJLAC10ServiceCode(string code)
        {
            // JLAC10 codes: 10-digit alphanumeric codes for laboratory tests
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length == 10 &&
                   code.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        private bool IsValidJapaneseDPCCode(string code)
        {
            // DPC codes: 6-digit numeric codes for diagnosis procedure combinations
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length == 6 &&
                   code.All(char.IsDigit);
        }

        private bool IsValidLoincCode(string code)
        {
            // LOINC codes: 5-6 digit numeric codes
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length >= 5 &&
                   code.Length <= 6 &&
                   code.All(char.IsDigit);
        }

        private bool IsValidSnomedCode(string code)
        {
            // SNOMED CT codes: 6-18 digit numeric codes
            return !string.IsNullOrWhiteSpace(code) &&
                   code.Length >= 6 &&
                   code.Length <= 18 &&
                   code.All(char.IsDigit);
        }
    }
}