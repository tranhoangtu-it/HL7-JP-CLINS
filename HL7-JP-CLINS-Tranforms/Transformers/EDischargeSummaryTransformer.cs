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
    /// Transformer for converting hospital eDischargeSummary data to JP-CLINS compliant FHIR Bundle
    /// Handles electronic discharge summary documents according to JP-CLINS v1.11.0 implementation guide
    /// </summary>
    public class EDischargeSummaryTransformer : BaseTransformer<dynamic>
    {
        /// <summary>
        /// JP-CLINS document type identifier for eDischargeSummary
        /// </summary>
        public override string DocumentType => "eDischargeSummary";

        /// <summary>
        /// JP-CLINS profile URL for eDischargeSummary Bundle
        /// </summary>
        public override string ProfileUrl => JpClinsConstants.DocumentProfiles.EDischargeSummary;

        /// <summary>
        /// Creates the main Composition resource for eDischargeSummary document
        /// </summary>
        /// <param name="input">eDischargeSummary input data from hospital systems</param>
        /// <returns>FHIR Composition resource</returns>
        protected override Composition CreateComposition(dynamic input)
        {
            var composition = new Composition
            {
                Id = FhirHelper.GenerateUniqueId("Composition"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Condition }, // eDischargeSummary composition profile
                    LastUpdated = DateTimeOffset.UtcNow
                },
                Status = CompositionStatus.Final,
                Type = CreateJapaneseCodeableConcept(
                    code: "18842-5",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Discharge summary"),

                // Patient subject
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input)),

                // Encounter context
                Encounter = input.encounterId != null
                    ? FhirHelper.CreateReference("Encounter", input.encounterId.ToString())
                    : null,

                // Document date (discharge date)
                DateElement = new FhirDateTime(input.dischargeDate ?? DateTime.UtcNow),

                // Author (attending physician)
                Author = new List<ResourceReference>
                {
                    FhirHelper.CreateReference("Practitioner", GetAttendingPhysicianId(input))
                },

                // Document title
                Title = "退院時サマリー (eDischargeSummary)"
            };

            // Create composition sections for eDischargeSummary
            composition.Section = CreateDischargeSummarySections(input);

            return composition;
        }

        /// <summary>
        /// Transforms input data into all required FHIR resources
        /// </summary>
        /// <param name="input">eDischargeSummary input data</param>
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

                // 2. Transform attending Practitioner
                if (input.attendingPhysician != null)
                {
                    var practitioner = PractitionerMapper.MapToPractitioner(input.attendingPhysician);
                    resources.Add(practitioner);
                }

                // 3. Transform Organization (hospital)
                if (input.organization != null)
                {
                    var organization = TransformOrganization(input.organization);
                    resources.Add(organization);
                }

                // 4. Transform Encounter (hospitalization)
                if (input.encounter != null)
                {
                    var encounter = TransformHospitalizationEncounter(input.encounter, input);
                    resources.Add(encounter);
                }

                // 5. Transform Clinical Information Resources (Core 5 Information)
                resources.AddRange(TransformClinicalResources(input));

                // 6. Transform Procedures performed during hospitalization
                if (input.procedures != null)
                {
                    var procedures = TransformProcedures(input.procedures);
                    resources.AddRange(procedures);
                }

                // 7. Transform discharge-specific resources
                resources.AddRange(TransformDischargeResources(input));

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to transform eDischargeSummary resources: {ex.Message}", ex);
            }

            return resources;
        }

        /// <summary>
        /// Creates composition sections specific to eDischargeSummary documents
        /// </summary>
        /// <param name="input">eDischargeSummary input data</param>
        /// <returns>List of composition sections</returns>
        private List<Composition.SectionComponent> CreateDischargeSummarySections(dynamic input)
        {
            var sections = new List<Composition.SectionComponent>();

            // Admission reason section (入院理由)
            var admissionSection = new Composition.SectionComponent
            {
                Title = "入院理由 (Admission Reason)",
                Code = CreateJapaneseCodeableConcept(
                    code: "46241-6",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Hospital admission reason"),
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.admissionReason ?? "入院理由が記載されています"}</div>"
                }
            };
            sections.Add(admissionSection);

            // Hospital course section (入院経過)
            var courseSection = new Composition.SectionComponent
            {
                Title = "入院経過 (Hospital Course)",
                Code = CreateJapaneseCodeableConcept(
                    code: "8648-8",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Hospital course"),
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.hospitalCourse ?? "入院経過が記載されています"}</div>"
                }
            };
            sections.Add(courseSection);

            // Discharge diagnosis section (退院時診断)
            var diagnosisSection = new Composition.SectionComponent
            {
                Title = "退院時診断 (Discharge Diagnosis)",
                Code = CreateJapaneseCodeableConcept(
                    code: "11535-2",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Hospital discharge diagnosis"),
                Entry = new List<ResourceReference>()
            };
            sections.Add(diagnosisSection);

            // Procedures section (実施手技・処置)
            if (input.procedures != null && HasProcedures(input.procedures))
            {
                var proceduresSection = new Composition.SectionComponent
                {
                    Title = "実施手技・処置 (Procedures)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "47519-4",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "History of Procedures"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(proceduresSection);
            }

            // Discharge medications section (退院時処方)
            if (input.dischargeMedications != null && HasMedications(input.dischargeMedications))
            {
                var medicationsSection = new Composition.SectionComponent
                {
                    Title = "退院時処方 (Discharge Medications)",
                    Code = CreateJapaneseCodeableConcept(
                        code: "10183-2",
                        system: JpClinsConstants.CodingSystems.LOINC,
                        display: "Hospital discharge medications"),
                    Entry = new List<ResourceReference>()
                };
                sections.Add(medicationsSection);
            }

            // Follow-up instructions section (退院後指導)
            var followUpSection = new Composition.SectionComponent
            {
                Title = "退院後指導 (Follow-up Instructions)",
                Code = CreateJapaneseCodeableConcept(
                    code: "18776-5",
                    system: JpClinsConstants.CodingSystems.LOINC,
                    display: "Plan of care"),
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated,
                    Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{input.followUpInstructions ?? "退院後の指導内容が記載されています"}</div>"
                }
            };
            sections.Add(followUpSection);

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
                // Transform discharge diagnoses (primary and secondary)
                if (input.dischargeDiagnoses != null)
                {
                    var diagnosesArray = input.dischargeDiagnoses as System.Collections.IEnumerable;
                    if (diagnosesArray != null)
                    {
                        foreach (var diagnosisData in diagnosesArray)
                        {
                            var conditionInput = CreateDischargeConditionInputModel(diagnosisData);
                            var conditionResource = ResourceTransformHelper.TransformCondition(conditionInput);
                            resources.Add(conditionResource);
                        }
                    }
                }

                // Transform hospital allergies discovered during stay
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

                // Transform lab results during hospitalization
                if (input.labResults != null)
                {
                    var labResultsArray = input.labResults as System.Collections.IEnumerable;
                    if (labResultsArray != null)
                    {
                        foreach (var labData in labResultsArray)
                        {
                            var observationInput = CreateObservationInputModel(labData);
                            var observationResource = ResourceTransformHelper.TransformObservation(observationInput);
                            resources.Add(observationResource);
                        }
                    }
                }

                // Transform discharge medications
                if (input.dischargeMedications != null)
                {
                    var medicationsArray = input.dischargeMedications as System.Collections.IEnumerable;
                    if (medicationsArray != null)
                    {
                        foreach (var medicationData in medicationsArray)
                        {
                            var medicationInput = CreateDischargeMedicationInputModel(medicationData);
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
        /// Transforms hospitalization encounter with Japanese context
        /// </summary>
        /// <param name="encounterData">Encounter data</param>
        /// <param name="input">Full input data</param>
        /// <returns>Encounter resource</returns>
        private Encounter TransformHospitalizationEncounter(dynamic encounterData, dynamic input)
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
                Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "IMP", "inpatient encounter"),
                Subject = FhirHelper.CreateReference("Patient", GetPatientId(input))
            };

            // Admission and discharge dates
            if (input.admissionDate != null || input.dischargeDate != null)
            {
                encounter.Period = FhirHelper.CreatePeriod(
                    start: input.admissionDate != null ? DateTime.Parse(input.admissionDate.ToString()) : null,
                    end: input.dischargeDate != null ? DateTime.Parse(input.dischargeDate.ToString()) : null);
            }

            // Admission reason
            if (input.admissionReason != null)
            {
                encounter.ReasonCode = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = input.admissionReason.ToString() }
                };
            }

            // Discharge disposition (退院先)
            if (input.dischargeDisposition != null)
            {
                encounter.Hospitalization = new Encounter.HospitalizationComponent
                {
                    DischargeDisposition = CreateJapaneseCodeableConcept(
                        code: input.dischargeDisposition.code?.ToString() ?? "home",
                        system: "http://jpfhir.jp/fhir/core/CodeSystem/JP_DischargeDisposition",
                        display: input.dischargeDisposition.display?.ToString() ?? "自宅")
                };
            }

            return encounter;
        }

        /// <summary>
        /// Transforms procedures performed during hospitalization
        /// </summary>
        /// <param name="proceduresData">Procedures data</param>
        /// <returns>List of Procedure resources</returns>
        private List<Procedure> TransformProcedures(dynamic proceduresData)
        {
            var procedures = new List<Procedure>();

            var proceduresArray = proceduresData as System.Collections.IEnumerable;
            if (proceduresArray != null)
            {
                foreach (var procedureData in proceduresArray)
                {
                    var procedure = new Procedure
                    {
                        Id = FhirHelper.GenerateUniqueId("Procedure"),
                        Status = EventStatus.Completed,
                        Subject = FhirHelper.CreateReference("Patient", GetPatientId(null)),
                        Code = CreateJapaneseCodeableConcept(
                            code: TransformHelper.SafeGetString(procedureData, "code", "procedure"),
                            system: JpClinsConstants.CodingSystems.JapanProcedure,
                            display: TransformHelper.SafeGetString(procedureData, "name", "処置"))
                    };

                    // Procedure date
                    var performedDate = TransformHelper.SafeGetDateTime(procedureData, "performedDate");
                    if (performedDate.HasValue)
                    {
                        procedure.Performed = new FhirDateTime(performedDate.Value);
                    }

                    procedures.Add(procedure);
                }
            }

            return procedures;
        }

        /// <summary>
        /// Transforms discharge-specific resources
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>List of discharge-related resources</returns>
        private List<Resource> TransformDischargeResources(dynamic input)
        {
            var resources = new List<Resource>();

            // Transform discharge plan/care plan
            if (input.dischargePlan != null)
            {
                var carePlan = TransformDischargePlan(input.dischargePlan);
                resources.Add(carePlan);
            }

            // Transform follow-up appointments
            if (input.followUpAppointments != null)
            {
                var appointments = TransformFollowUpAppointments(input.followUpAppointments);
                resources.AddRange(appointments);
            }

            return resources;
        }

        /// <summary>
        /// Document-specific input validation for eDischargeSummary
        /// </summary>
        /// <param name="input">Input data to validate</param>
        /// <param name="result">Validation result to update</param>
        protected override void ValidateSpecificInput(dynamic input, ValidationResult result)
        {
            // Validate required eDischargeSummary fields
            if (input.patient == null)
            {
                result.AddError("Patient information is required for eDischargeSummary");
            }

            if (input.attendingPhysician == null)
            {
                result.AddError("Attending physician information is required");
            }

            if (input.admissionDate == null)
            {
                result.AddError("Admission date is required");
            }

            if (input.dischargeDate == null)
            {
                result.AddError("Discharge date is required");
            }

            // Validate date logic
            if (input.admissionDate != null && input.dischargeDate != null)
            {
                var admissionDate = DateTime.Parse(input.admissionDate.ToString());
                var dischargeDate = DateTime.Parse(input.dischargeDate.ToString());

                if (admissionDate >= dischargeDate)
                {
                    result.AddError("Admission date must be before discharge date");
                }
            }

            if (input.dischargeDiagnoses == null)
            {
                result.AddError("At least one discharge diagnosis is required");
            }

            // JP-CLINS specific validations
            ValidateJapaneseDischargeRequirements(input, result);
        }

        /// <summary>
        /// Validates JP-CLINS specific requirements for eDischargeSummary
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="result">Validation result</param>
        private void ValidateJapaneseDischargeRequirements(dynamic input, ValidationResult result)
        {
            // Validate Japanese hospital stay requirements
            if (input.admissionDate != null && input.dischargeDate != null)
            {
                var admissionDate = DateTime.Parse(input.admissionDate.ToString());
                var dischargeDate = DateTime.Parse(input.dischargeDate.ToString());
                var lengthOfStay = (dischargeDate - admissionDate).Days;

                // Japanese healthcare system considerations
                if (lengthOfStay > 365)
                {
                    result.AddWarning("Hospital stay longer than 1 year requires special documentation in Japan");
                }
            }

            // Validate Japanese discharge diagnosis coding
            if (input.dischargeDiagnoses != null)
            {
                var diagnosesArray = input.dischargeDiagnoses as System.Collections.IEnumerable;
                if (diagnosesArray != null)
                {
                    foreach (var diagnosis in diagnosesArray)
                    {
                        var diagnosisCode = TransformHelper.SafeGetString(diagnosis, "code");
                        if (!string.IsNullOrWhiteSpace(diagnosisCode))
                        {
                            var codeSystem = TransformHelper.SafeGetString(diagnosis, "codeSystem");
                            if (!FhirHelper.ValidateJapaneseDiagnosisCode(diagnosisCode, codeSystem))
                            {
                                result.AddWarning($"Diagnosis code should follow Japanese ICD-10-CM-JP format: {diagnosisCode}");
                            }
                        }
                    }
                }
            }
        }

        // Helper methods for creating input models
        private ConditionInputModel CreateDischargeConditionInputModel(dynamic conditionData) =>
            new ConditionInputModel
            {
                PatientId = GetPatientId(null),
                ConditionName = conditionData.name?.ToString() ?? "",
                ConditionCode = conditionData.code?.ToString(),
                ConditionCodeSystem = conditionData.codeSystem?.ToString() ?? JpClinsConstants.CodingSystems.ICD10JP,
                ClinicalStatus = "inactive", // Discharge diagnosis
                Category = "encounter-diagnosis",
                RecordedDate = DateTime.UtcNow
            };

        private MedicationRequestInputModel CreateDischargeMedicationInputModel(dynamic medicationData) =>
            new MedicationRequestInputModel
            {
                PatientId = GetPatientId(null),
                MedicationName = medicationData.name?.ToString() ?? "",
                MedicationCode = medicationData.code?.ToString(),
                MedicationCodeSystem = medicationData.codeSystem?.ToString() ?? JpClinsConstants.CodingSystems.YakuzaiCode,
                RequesterId = GetAttendingPhysicianId(null),
                Category = "discharge",
                Status = "active"
            };

        // Helper methods for extracting IDs
        private string GetPatientId(dynamic input) =>
            input?.patient?.patientId?.ToString() ?? FhirHelper.GenerateUniqueId("Patient");

        private string GetAttendingPhysicianId(dynamic input) =>
            input?.attendingPhysician?.practitionerId?.ToString() ?? FhirHelper.GenerateUniqueId("Practitioner");

        // Helper methods for checking data existence
        private bool HasProcedures(dynamic procedures) =>
            procedures is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        private bool HasMedications(dynamic medications) =>
            medications is System.Collections.IEnumerable enumerable && enumerable.Cast<object>().Any();

        // Placeholder methods (to be implemented)
        private Organization TransformOrganization(dynamic orgData) =>
            new Organization
            {
                Id = FhirHelper.GenerateUniqueId("Organization"),
                Name = orgData.name?.ToString() ?? "Hospital Organization"
            };

        private AllergyIntoleranceInputModel CreateAllergyInputModel(dynamic allergyData) =>
            new AllergyIntoleranceInputModel
            {
                PatientId = GetPatientId(null),
                SubstanceName = allergyData.substanceName?.ToString() ?? "",
                Category = allergyData.category?.ToString() ?? "medication"
            };

        private ObservationInputModel CreateObservationInputModel(dynamic observationData) =>
            new ObservationInputModel
            {
                PatientId = GetPatientId(null),
                ObservationCode = observationData.code?.ToString() ?? "",
                ObservationCodeSystem = observationData.codeSystem?.ToString() ?? "urn:oid:1.2.392.200119.4.504", // JLAC10
                ObservationName = observationData.name?.ToString() ?? "",
                Status = "final"
            };

        private CarePlan TransformDischargePlan(dynamic dischargePlan) =>
            new CarePlan
            {
                Id = FhirHelper.GenerateUniqueId("CarePlan"),
                Status = RequestStatus.Active,
                Intent = CarePlan.CarePlanIntent.Plan
            };

        private List<Appointment> TransformFollowUpAppointments(dynamic appointments) =>
            new List<Appointment>();

        // TODO: JP-CLINS eDischargeSummary Implementation Notes:
        // 1. Implement Japanese DPC (Diagnosis Procedure Combination) coding
        // 2. Add support for Japanese insurance claim integration
        // 3. Include Japanese pharmaceutical coding (YJ codes) for discharge medications
        // 4. Support for Japanese nursing care insurance (介護保険) workflows
        // 5. Add validation for Japanese hospital discharge criteria
        // 6. Implement Japanese quality indicators for hospital care
        // 7. Support for Japanese medical device coding and implants
        // 8. Include Japanese rehabilitation and follow-up care pathways
        // 9. Add support for Japanese home healthcare referrals
        // 10. Implement Japanese medication reconciliation standards
    }
}