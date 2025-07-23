using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Base;
using HL7_JP_CLINS_Tranforms.Interfaces;
using HL7_JP_CLINS_Tranforms.Mappers;
using HL7_JP_CLINS_Tranforms.Utilities;

namespace HL7_JP_CLINS_Tranforms.Transformers
{
    /// <summary>
    /// Transformer for converting hospital eCheckup data to JP-CLINS compliant FHIR Bundle
    /// Handles electronic health checkup documents according to JP-CLINS v1.11.0 implementation guide
    /// Supports both occupational health checkups (職場健診) and general health checkups (一般健診)
    /// </summary>
    public class ECheckupTransformer : BaseTransformer<dynamic>
    {
        /// <summary>
        /// JP-CLINS document type identifier for eCheckup
        /// </summary>
        public override string DocumentType => "eCheckup";

        /// <summary>
        /// JP-CLINS profile URL for eCheckup Bundle
        /// </summary>
        public override string ProfileUrl => JpClinsConstants.DocumentProfiles.ECheckup;

        /// <summary>
        /// Creates the main Composition resource for eCheckup document
        /// </summary>
        /// <param name="input">eCheckup input data from health checkup systems</param>
        /// <returns>FHIR Composition resource</returns>
        protected override Composition CreateComposition(dynamic input)
        {
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Condition }, // eCheckup composition profile
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Status = CompositionStatus.Final,
                Type = CreateJapaneseCodeableConcept(
                    code: "11502-2",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Laboratory report"),

                // Patient subject
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input)),

                // Encounter context (checkup session)
                Encounter = input.encounterId != null
                    ? FhirHelper.CreateReference("Encounter", input.encounterId.ToString())
                    : null,

                // Checkup date
                DateElement = new FhirDateTime(input.checkupDate ?? DateTime.UtcNow),

                // Author (examining practitioner)
                Author = new List<ResourceReference>
                {
                    FhirHelper.CreateReference("Practitioner", GetExaminingPhysicianId(input))
                },

                // Document title based on checkup type
                Title = GetCheckupTitle(input.checkupType?.ToString())
            };

            // Create composition sections for eCheckup
            composition.Section = CreateCheckupSections(input);

            return composition;
        }

        /// <summary>
        /// Transforms input data into all required FHIR resources
        /// </summary>
        /// <param name="input">eCheckup input data</param>
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

                // 2. Transform examining Practitioner
                if (input.examiningPhysician != null)
                {
                    var practitioner = PractitionerMapper.MapToPractitioner(input.examiningPhysician);
                    resources.Add(practitioner);
                }

                // 3. Transform healthcare Organization
                if (input.organization != null)
                {
                    var organization = TransformHealthcareOrganization(input.organization);
                    resources.Add(organization);
                }

                // 4. Transform Encounter (checkup session)
                if (input.encounter != null)
                {
                    var encounter = TransformCheckupEncounter(input.encounter, input);
                    resources.Add(encounter);
                }

                // 5. Transform checkup Observations (lab results, vital signs, etc.)
                resources.AddRange(TransformCheckupObservations(input));

                // 6. Transform Conditions found during checkup
                if (input.findings != null)
                {
                    var conditions = TransformCheckupFindings(input.findings);
                    resources.AddRange(conditions);
                }

                // 7. Transform checkup-specific resources
                resources.AddRange(TransformCheckupSpecificResources(input));

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform eCheckup resources: {ex.Message}", ex);
            }

            return resources;
        }

        /// <summary>
        /// Creates composition sections specific to eCheckup documents
        /// </summary>
        /// <param name="input">eCheckup input data</param>
        /// <returns>List of composition sections</returns>
        private List<Composition.SectionComponent> CreateCheckupSections(dynamic input)
        {
            var sections = new List<Composition.SectionComponent>();

            // Vital signs section (バイタルサイン)
            if (input.vitalSigns != null && HasVitalSigns(input.vitalSigns))
            {
                var vitalSignsSection = new Composition.SectionComponent
                {
                    Title = "バイタルサイン (Vital Signs)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "8716-3",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Vital signs"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(vitalSignsSection);
            }

            // Laboratory results section (検査結果)
            if (input.labResults != null && HasLabResults(input.labResults))
            {
                var labSection = new Composition.SectionComponent
                {
                    Title = "検査結果 (Laboratory Results)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "18725-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Microbiology studies"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(labSection);
            }

            // Physical examination section (身体診察)
            if (input.physicalExam != null)
            {
                var physicalExamSection = new Composition.SectionComponent
                {
                    Title = "身体診察 (Physical Examination)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "29545-1",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Physical findings"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.physicalExam.findings ?? "身体診察所見が記載されています"}</div>"
                    }
                };
                sections.Add(physicalExamSection);
            }

            // Imaging studies section (画像検査) - if applicable for checkup type
            if (input.imagingStudies != null && HasImagingStudies(input.imagingStudies))
            {
                var imagingSection = new Composition.SectionComponent
                {
                    Title = "画像検査 (Imaging Studies)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "18748-4",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Diagnostic imaging study"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(imagingSection);
            }

            // Checkup assessment section (健診判定)
            var assessmentSection = new Composition.SectionComponent
            {
                Title = "健診判定 (Checkup Assessment)",
                Code = CreateJapaneseCodeableConcept(
                    code: "51847-2",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Evaluation + plan note"),
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.assessment ?? "健診総合判定が記載されています"}</div>"
                }
            };
            sections.Add(assessmentSection);

            // Recommendations section (指導事項)
            if (input.recommendations != null)
            {
                var recommendationsSection = new Composition.SectionComponent
                {
                    Title = "指導事項 (Recommendations)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "18776-5",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Plan of care"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.recommendations ?? "指導内容が記載されています"}</div>"
                    }
                };
                sections.Add(recommendationsSection);
            }

            // Occupational health specific sections (for 職場健診)
            if (IsOccupationalCheckup(input.checkupType?.ToString()))
            {
                sections.AddRange(CreateOccupationalHealthSections(input));
            }

            return sections;
        }

        /// <summary>
        /// Creates occupational health specific sections for workplace checkups
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of occupational health sections</returns>
        private List<Composition.SectionComponent> CreateOccupationalHealthSections(dynamic input)
        {
            var sections = new List<Composition.SectionComponent>();

            // Work environment exposure section (作業環境)
            if (input.workEnvironment != null)
            {
                var workEnvSection = new Composition.SectionComponent
                {
                    Title = "作業環境 (Work Environment)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "11369-6",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of Immunization"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.workEnvironment.description ?? "作業環境の情報が記載されています"}</div>"
                    }
                };
                sections.Add(workEnvSection);
            }

            // Occupational history section (職歴)
            if (input.occupationalHistory != null)
            {
                var occupationalSection = new Composition.SectionComponent
                {
                    Title = "職歴 (Occupational History)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "11348-0",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of past illness"),
                    Text = new Narrative
                    {
                        Status = Narrative.NarrativeStatus.Generated,
                        Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.occupationalHistory.description ?? "職歴が記載されています"}</div>"
                    }
                };
                sections.Add(occupationalSection);
            }

            return sections;
        }

        /// <summary>
        /// Transforms checkup observations (lab results, vital signs, measurements)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of Observation resources</returns>
        private List<Resource> TransformCheckupObservations(dynamic input)
        {
            var observations = new List<Resource>();

            try
            {
                // Transform vital signs
                if (input.vitalSigns != null)
                {
                    var vitalSignsArray = input.vitalSigns as System.Collections.IEnumerable;
                    if (vitalSignsArray != null)
                    {
                        foreach (var vitalSign in vitalSignsArray)
                        {
                            var observationInput = CreateVitalSignObservationInputModel(vitalSign);
                            var observation = ResourceTransformHelper.TransformObservation(observationInput);
                            observations.Add(observation);
                        }
                    }
                }

                // Transform laboratory results
                if (input.labResults != null)
                {
                    var labResultsArray = input.labResults as System.Collections.IEnumerable;
                    if (labResultsArray != null)
                    {
                        foreach (var labResult in labResultsArray)
                        {
                            var observationInput = CreateLabObservationInputModel(labResult);
                            var observation = ResourceTransformHelper.TransformObservation(observationInput);
                            observations.Add(observation);
                        }
                    }
                }

                // Transform body measurements (身体計測)
                if (input.bodyMeasurements != null)
                {
                    var measurementsArray = input.bodyMeasurements as System.Collections.IEnumerable;
                    if (measurementsArray != null)
                    {
                        foreach (var measurement in measurementsArray)
                        {
                            var observationInput = CreateBodyMeasurementObservationInputModel(measurement);
                            var observation = ResourceTransformHelper.TransformObservation(observationInput);
                            observations.Add(observation);
                        }
                    }
                }

                // Transform specific occupational health tests
                if (IsOccupationalCheckup(input.checkupType?.ToString()) && input.occupationalTests != null)
                {
                    var occupationalTests = TransformOccupationalHealthTests(input.occupationalTests);
                    observations.AddRange(occupationalTests);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform checkup observations: {ex.Message}", ex);
            }

            return observations;
        }

        /// <summary>
        /// Transforms checkup encounter with Japanese health checkup context
        /// </summary>
        /// <param name="encounterData">Encounter data</param>
        /// <param name="input">Full input data</param>
        /// <returns>Encounter resource</returns>
        private Encounter TransformCheckupEncounter(dynamic encounterData, dynamic input)
        {
            var encounter = new Encounter
            {
                Id = FhirHelper.GenerateUniqueId("Encounter"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Encounter },
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Status = Encounter.EncounterStatus.Finished,
                Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input))
            };

            // Checkup type coding
            if (input.checkupType != null)
            {
                encounter.Type = new List<CodeableConcept>
                {
                    CreateJapaneseCheckupTypeCoding(input.checkupType.ToString())
                };
            }

            // Checkup date
            if (input.checkupDate != null)
            {
                var checkupDate = DateTime.Parse(input.checkupDate.ToString());
                encounter.Period = FhirHelper.CreatePeriod(start: checkupDate, end: checkupDate.AddHours(4)); // Typical checkup duration
            }

            // Service provider (checkup organization)
            if (input.organization != null)
            {
                encounter.ServiceProvider = FhirHelper.CreateReference("Organization", input.organization.organizationId?.ToString() ?? "checkup-org");
            }

            return encounter;
        }

        /// <summary>
        /// Transforms checkup findings into Condition resources
        /// </summary>
        /// <param name="findingsData">Findings data</param>
        /// <returns>List of Condition resources</returns>
        private List<Condition> TransformCheckupFindings(dynamic findingsData)
        {
            var conditions = new List<Condition>();

            var findingsArray = findingsData as System.Collections.IEnumerable;
            if (findingsArray != null)
            {
                foreach (var finding in findingsArray)
                {
                    var conditionInput = CreateFindingConditionInputModel(finding);
                    var condition = ResourceTransformHelper.TransformCondition(conditionInput);
                    conditions.Add(condition);
                }
            }

            return conditions;
        }

        /// <summary>
        /// Transforms occupational health specific tests
        /// </summary>
        /// <param name="occupationalTestsData">Occupational tests data</param>
        /// <returns>List of Observation resources</returns>
        private List<Resource> TransformOccupationalHealthTests(dynamic occupationalTestsData)
        {
            var observations = new List<Resource>();

            var testsArray = occupationalTestsData as System.Collections.IEnumerable;
            if (testsArray != null)
            {
                foreach (var test in testsArray)
                {
                    var observationInput = CreateOccupationalTestObservationInputModel(test);
                    var observation = ResourceTransformHelper.TransformObservation(observationInput);
                    observations.Add(observation);
                }
            }

            return observations;
        }

        /// <summary>
        /// Document-specific input validation for eCheckup
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <param name="result">Validation result to update</param>
        protected override void ValidateSpecificInput(dynamic input, ValidationResult result)
        {
            // Validate required eCheckup fields
            if (input.patient == null)
            {
                result.AddError("Patient information is required for eCheckup");
            }

            if (input.examiningPhysician == null)
            {
                result.AddError("Examining physician information is required");
            }

            if (input.checkupDate == null)
            {
                result.AddError("Checkup date is required");
            }

            if (string.IsNullOrWhiteSpace(input.checkupType?.ToString()))
            {
                result.AddError("Checkup type must be specified");
            }

            // Validate checkup type specific requirements
            ValidateCheckupTypeRequirements(input, result);

            // JP-CLINS specific validations
            ValidateJapaneseCheckupRequirements(input, result);
        }

        /// <summary>
        /// Validates requirements specific to different types of checkups
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateCheckupTypeRequirements(dynamic input, ValidationResult result)
        {
            var checkupType = input.checkupType?.ToString()?.ToLower();

            switch (checkupType)
            {
                case "occupational":
                case "職場健診":
                    // Occupational checkup requirements
                    if (input.workEnvironment == null)
                    {
                        result.AddWarning("Work environment information recommended for occupational checkups");
                    }

                    // Validate mandatory occupational health tests
                    ValidateOccupationalHealthTests(input, result);
                    break;

                case "general":
                case "一般健診":
                    // General checkup requirements
                    if (input.vitalSigns == null)
                    {
                        result.AddError("Vital signs are required for general health checkups");
                    }
                    break;

                case "specific":
                case "特定健診":
                    // Specific health checkup (metabolic syndrome screening)
                    ValidateSpecificHealthCheckupRequirements(input, result);
                    break;
            }
        }

        /// <summary>
        /// Validates mandatory occupational health tests per Japanese regulations
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateOccupationalHealthTests(dynamic input, ValidationResult result)
        {
            // Required tests for occupational health checkups in Japan
            var requiredTests = new[]
            {
                "胸部X線", "chest-xray",
                "聴力検査", "hearing-test",
                "視力検査", "vision-test",
                "血圧測定", "blood-pressure",
                "尿検査", "urine-test"
            };

            // Check if required tests are included
            if (input.occupationalTests != null)
            {
                var testsArray = input.occupationalTests as System.Collections.IEnumerable;
                if (testsArray != null)
                {
                    var performedTests = testsArray.Cast<dynamic>()
                        .Select(test => test.testType?.ToString()?.ToLower())
                        .Where(testType => !string.IsNullOrWhiteSpace(testType))
                        .ToList();

                    foreach (var requiredTest in requiredTests)
                    {
                        if (!performedTests.Any(pt => pt.Contains(requiredTest.ToLower())))
                        {
                            result.AddWarning($"Recommended occupational health test missing: {requiredTest}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates specific health checkup requirements (特定健診)
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateSpecificHealthCheckupRequirements(dynamic input, ValidationResult result)
        {
            // Specific health checkup focuses on metabolic syndrome screening
            var requiredMetabolicTests = new[]
            {
                "BMI", "腹囲", "血圧", "血糖", "HbA1c", "脂質"
            };

            // Check for metabolic syndrome screening components
            if (input.labResults != null)
            {
                var labResultsArray = input.labResults as System.Collections.IEnumerable;
                if (labResultsArray != null)
                {
                    var performedTests = labResultsArray.Cast<dynamic>()
                        .Select(test => test.testName?.ToString())
                        .Where(testName => !string.IsNullOrWhiteSpace(testName))
                        .ToList();

                    foreach (var requiredTest in requiredMetabolicTests)
                    {
                        if (!performedTests.Any(pt => pt.Contains(requiredTest)))
                        {
                            result.AddWarning($"Specific health checkup component missing: {requiredTest}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates JP-CLINS specific requirements for eCheckup
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateJapaneseCheckupRequirements(dynamic input, ValidationResult result)
        {
            // Validate Japanese medical license for examining physician
            if (input.examiningPhysician?.medicalLicenseNumber != null)
            {
                var licenseNumber = input.examiningPhysician.medicalLicenseNumber.ToString();
                if (!FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    result.AddError($"Invalid Japanese medical license number: {licenseNumber}");
                }
            }

            // Validate JLAC10 codes for lab results
            if (input.labResults != null)
            {
                var labResultsArray = input.labResults as System.Collections.IEnumerable;
                if (labResultsArray != null)
                {
                    foreach (var labResult in labResultsArray)
                    {
                        var testCode = TransformHelper.SafeGetString(labResult, "testCode");
                        if (!string.IsNullOrWhiteSpace(testCode))
                        {
                            if (!FhirHelper.ValidateJLAC10Code(testCode))
                            {
                                result.AddWarning($"Lab test code should follow JLAC10 format: {testCode}");
                            }
                        }
                    }
                }
            }

            // Validate Japanese vital signs ranges
            if (input.vitalSigns != null)
            {
                var vitalSignsArray = input.vitalSigns as System.Collections.IEnumerable;
                if (vitalSignsArray != null)
                {
                    foreach (var vitalSign in vitalSignsArray)
                    {
                        var vitalType = TransformHelper.SafeGetString(vitalSign, "type");
                        var value = TransformHelper.SafeGetString(vitalSign, "value");
                        if (!string.IsNullOrWhiteSpace(vitalType) && !string.IsNullOrWhiteSpace(value))
                        {
                            if (decimal.TryParse(value, out decimal numericValue))
                            {
                                var unit = TransformHelper.SafeGetString(vitalSign, "unit", "");
                                if (!FhirHelper.ValidateJapaneseVitalSigns(vitalType, numericValue, unit))
                                {
                                    result.AddWarning($"Vital sign value outside Japanese population ranges: {vitalType} = {value}");
                                }
                            }
                        }
                    }
                }
            }
        }

        // Helper methods for creating input models
        private ObservationInputModel CreateVitalSignObservationInputModel(dynamic vitalSignData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = GetVitalSignCode(vitalSignData.type?.ToString() ?? ""),
                ObservationCodeSystem = JpClinsConstants.CodingSystems.LOINC,
                ObservationName = vitalSignData.type?.ToString() ?? "",
                Status = "final",
                ValueQuantity = vitalSignData.value?.ToString(),
                Unit = vitalSignData.unit?.ToString(),
                Category = "vital-signs"
            };

        private ObservationInputModel CreateLabObservationInputModel(dynamic labData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = labData.testCode?.ToString() ?? "",
                ObservationCodeSystem = "urn:oid:1.2.392.200119.4.504", // JLAC10
                ObservationName = labData.testName?.ToString() ?? "",
                Status = "final",
                ValueQuantity = TransformHelper.SafeGetString(labData, "value"),
                Unit = TransformHelper.SafeGetString(labData, "unit"),
                Category = "laboratory"
            };

        private ObservationInputModel CreateBodyMeasurementObservationInputModel(dynamic measurementData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = GetBodyMeasurementCode(measurementData.type?.ToString() ?? ""),
                ObservationCodeSystem = JpClinsConstants.CodingSystems.LOINC,
                ObservationName = measurementData.type?.ToString() ?? "",
                Status = "final",
                ValueQuantity = measurementData.value?.ToString(),
                Unit = measurementData.unit?.ToString(),
                Category = "survey"
            };

        private ConditionInputModel CreateFindingConditionInputModel(dynamic findingData) =>
            new ConditionInputModel
            {
                PatientId = GetPatientId(null),
                ConditionName = findingData.name?.ToString() ?? "",
                ConditionCode = findingData.code?.ToString(),
                ConditionCodeSystem = findingData.codeSystem?.ToString() ?? JpClinsConstants.CodingSystems.ICD10JP,
                ClinicalStatus = "active",
                Category = "encounter-diagnosis",
                Severity = findingData.severity?.ToString()
            };

        private ObservationInputModel CreateOccupationalTestObservationInputModel(dynamic testData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = GetOccupationalTestCode(testData.testType?.ToString() ?? ""),
                ObservationCodeSystem = JpClinsConstants.CodingSystems.LOINC,
                ObservationName = testData.testType?.ToString() ?? "",
                Status = "final",
                ValueString = testData.result?.ToString(),
                Category = "exam"
            };

        // Helper methods for extracting IDs and values
        private string GetPatientId(dynamic input) =>
            input?.patient?.patientId?.ToString() ?? FhirHelper.GenerateUniqueId("Patient");

        private string GetExaminingPhysicianId(dynamic input) =>
            input?.examiningPhysician?.practitionerId?.ToString() ?? FhirHelper.GenerateUniqueId("Practitioner");

        private string GetCheckupTitle(string checkupType) => checkupType?.ToLower() switch
        {
            "occupational" or "職場健診" => "職場健診結果 (Occupational Health Checkup)",
            "general" or "一般健診" => "一般健診結果 (General Health Checkup)",
            "specific" or "特定健診" => "特定健診結果 (Specific Health Checkup)",
            _ => "健診結果 (Health Checkup Results)"
        };

        private bool IsOccupationalCheckup(string checkupType) =>
            checkupType?.ToLower() is "occupational" or "職場健診";

        private CodeableConcept CreateJapaneseCheckupTypeCoding(string checkupType) => checkupType?.ToLower() switch
        {
            "occupational" or "職場健診" => CreateJapaneseCodeableConcept("occupational", "http://jpfhir.jp/fhir/core/CodeSystem/JP_CheckupType", "職場健診"),
            "general" or "一般健診" => CreateJapaneseCodeableConcept("general", "http://jpfhir.jp/fhir/core/CodeSystem/JP_CheckupType", "一般健診"),
            "specific" or "特定健診" => CreateJapaneseCodeableConcept("specific", "http://jpfhir.jp/fhir/core/CodeSystem/JP_CheckupType", "特定健診"),
            _ => CreateJapaneseCodeableConcept("checkup", "http://jpfhir.jp/fhir/core/CodeSystem/JP_CheckupType", "健診")
        };

        // Helper methods for checking data existence
        private bool HasVitalSigns(dynamic vitalSigns) =>
            vitalSigns is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasLabResults(dynamic labResults) =>
            labResults is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasImagingStudies(dynamic imagingStudies) =>
            imagingStudies is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        // Code mapping methods
        private string GetVitalSignCode(string vitalType) => vitalType.ToLower() switch
        {
            "血圧" or "blood-pressure" or "systolic-bp" => "8480-6",
            "拡張期血圧" or "diastolic-bp" => "8462-4",
            "心拍数" or "heart-rate" or "pulse" => "8867-4",
            "体温" or "body-temperature" or "temperature" => "8310-5",
            "呼吸数" or "respiratory-rate" => "9279-1",
            "身長" or "height" => "8302-2",
            "体重" or "weight" => "29463-7",
            "BMI" => "39156-5",
            _ => "8716-3" // General vital signs
        };

        private string GetBodyMeasurementCode(string measurementType) => measurementType.ToLower() switch
        {
            "身長" or "height" => "8302-2",
            "体重" or "weight" => "29463-7",
            "BMI" => "39156-5",
            "腹囲" or "waist-circumference" => "8280-0",
            "胸囲" or "chest-circumference" => "11337-4",
            _ => "9843-4" // Body measurements
        };

        private string GetOccupationalTestCode(string testType) => testType.ToLower() switch
        {
            "胸部x線" or "chest-xray" => "36643-5",
            "聴力検査" or "hearing-test" => "28615-3",
            "視力検査" or "vision-test" => "70936-0",
            "心電図" or "ecg" or "ekg" => "11524-6",
            "血液検査" or "blood-test" => "33743-4",
            "尿検査" or "urine-test" => "33746-7",
            _ => "67504-6" // Occupational health examination
        };

        // Placeholder methods
        private Organization TransformHealthcareOrganization(dynamic orgData) =>
            new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = orgData.name?.ToString() ?? "Healthcare Organization"
            };

        private List<Resource> TransformCheckupSpecificResources(dynamic input) =>
            new List<Resource>();

        // TODO: JP-CLINS eCheckup Implementation Notes:
        // 1. Implement Japanese health checkup regulation compliance (健康増進法)
        // 2. Add support for metabolic syndrome screening (特定健診)
        // 3. Include Japanese occupational health law compliance (労働安全衛生法)
        // 4. Support for Japanese cancer screening programs
        // 5. Add validation for Japanese health checkup intervals
        // 6. Implement Japanese health insurance integration
        // 7. Support for Japanese workplace health promotion
        // 8. Include Japanese environmental health assessments
        // 9. Add support for Japanese elderly health checkups
        // 10. Implement Japanese public health reporting requirements
    }
}