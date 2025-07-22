using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Models.InputModels;
using HL7_JP_CLINS_Core.Constants;

namespace HL7_JP_CLINS_Core.Utilities
{
    /// <summary>
    /// Utility class for transforming input models to FHIR resources
    /// Provides methods to convert JP-CLINS input models to compliant FHIR resources
    /// </summary>
    public static class ResourceTransformHelper
    {
        /// <summary>
        /// Transforms AllergyIntoleranceInputModel to FHIR AllergyIntolerance resource
        /// </summary>
        /// <param name="input">Input model containing allergy/intolerance data</param>
        /// <returns>FHIR AllergyIntolerance resource</returns>
        public static AllergyIntolerance TransformAllergyIntolerance(AllergyIntoleranceInputModel input)
        {
            if (input == null || !input.IsValid())
                throw new ArgumentException("Invalid AllergyIntoleranceInputModel");

            var allergy = new AllergyIntolerance
            {
                Id = FhirHelper.GenerateUniqueId("AllergyIntolerance"),
                Meta = new Meta
                {
                    Profile = new[] { input.GetJpClinsProfile() }
                },
                Patient = FhirHelper.CreateReference("Patient", input.PatientId)
            };

            // Set clinical status
            allergy.ClinicalStatus = FhirHelper.CreateCodeableConcept(
                input.ClinicalStatus,
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical");

            // Set verification status  
            allergy.VerificationStatus = FhirHelper.CreateCodeableConcept(
                input.VerificationStatus,
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification");

            // Set category
            allergy.Category = input.Category?.ToLower() switch
            {
                "medication" => new List<AllergyIntolerance.AllergyIntoleranceCategory?> { AllergyIntolerance.AllergyIntoleranceCategory.Medication },
                "food" => new List<AllergyIntolerance.AllergyIntoleranceCategory?> { AllergyIntolerance.AllergyIntoleranceCategory.Food },
                "environment" => new List<AllergyIntolerance.AllergyIntoleranceCategory?> { AllergyIntolerance.AllergyIntoleranceCategory.Environment },
                "biologic" => new List<AllergyIntolerance.AllergyIntoleranceCategory?> { AllergyIntolerance.AllergyIntoleranceCategory.Biologic },
                _ => new List<AllergyIntolerance.AllergyIntoleranceCategory?> { AllergyIntolerance.AllergyIntoleranceCategory.Medication }
            };

            // Set code (substance) with JP-CLINS specific coding priorities
            if (!string.IsNullOrWhiteSpace(input.SubstanceCode) && !string.IsNullOrWhiteSpace(input.SubstanceCodeSystem))
            {
                // Create CodeableConcept with Japanese coding priority
                allergy.Code = FhirHelper.CreateCodeableConcept(code: input.SubstanceCode, system: input.SubstanceCodeSystem, display: input.SubstanceName);
            }
            else
            {
                allergy.Code = new CodeableConcept { Text = input.SubstanceName };
            }

            // Set recorded date
            if (input.RecordedDate.HasValue)
            {
                allergy.RecordedDateElement = new FhirDateTime(input.RecordedDate.Value);
            }

            // Set recorder
            if (!string.IsNullOrWhiteSpace(input.RecorderId))
            {
                allergy.Recorder = FhirHelper.CreateReference("Practitioner", input.RecorderId);
            }

            // Add reaction if specified
            if (!string.IsNullOrWhiteSpace(input.ReactionSeverity) || !string.IsNullOrWhiteSpace(input.ReactionDescription))
            {
                var reaction = new AllergyIntolerance.ReactionComponent();

                if (!string.IsNullOrWhiteSpace(input.Manifestation))
                {
                    reaction.Manifestation = new List<CodeableConcept>
                    {
                        new CodeableConcept { Text = input.Manifestation }
                    };
                }

                if (!string.IsNullOrWhiteSpace(input.ReactionDescription))
                {
                    reaction.Description = input.ReactionDescription;
                }

                if (!string.IsNullOrWhiteSpace(input.ReactionSeverity))
                {
                    reaction.Severity = input.ReactionSeverity.ToLower() switch
                    {
                        "mild" => AllergyIntolerance.AllergyIntoleranceSeverity.Mild,
                        "moderate" => AllergyIntolerance.AllergyIntoleranceSeverity.Moderate,
                        "severe" => AllergyIntolerance.AllergyIntoleranceSeverity.Severe,
                        _ => AllergyIntolerance.AllergyIntoleranceSeverity.Mild
                    };
                }

                if (input.LastOccurrence.HasValue)
                {
                    reaction.OnsetElement = new FhirDateTime(input.LastOccurrence.Value);
                }

                allergy.Reaction = new List<AllergyIntolerance.ReactionComponent> { reaction };
            }

            // Add notes
            if (!string.IsNullOrWhiteSpace(input.Notes))
            {
                allergy.Note = new List<Annotation>
                {
                    new Annotation { Text = input.Notes }
                };
            }

            return allergy;
        }

        /// <summary>
        /// Transforms ConditionInputModel to FHIR Condition resource
        /// </summary>
        /// <param name="input">Input model containing condition/diagnosis data</param>
        /// <returns>FHIR Condition resource</returns>
        public static Condition TransformCondition(ConditionInputModel input)
        {
            if (input == null || !input.IsValid())
                throw new ArgumentException("Invalid ConditionInputModel");

            var condition = new Condition
            {
                Id = FhirHelper.GenerateUniqueId("Condition"),
                Meta = new Meta
                {
                    Profile = new[] { input.GetJpClinsProfile() }
                },
                Subject = FhirHelper.CreateReference("Patient", input.PatientId)
            };

            // Set clinical status
            condition.ClinicalStatus = FhirHelper.CreateCodeableConcept(
                input.ClinicalStatus,
                "http://terminology.hl7.org/CodeSystem/condition-clinical");

            // Set verification status
            condition.VerificationStatus = FhirHelper.CreateCodeableConcept(
                input.VerificationStatus,
                "http://terminology.hl7.org/CodeSystem/condition-ver-status");

            // Set category
            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                condition.Category = new List<CodeableConcept>
                {
                    FhirHelper.CreateCodeableConcept(input.Category, "http://terminology.hl7.org/CodeSystem/condition-category")
                };
            }

            // Set severity
            if (!string.IsNullOrWhiteSpace(input.Severity))
            {
                condition.Severity = FhirHelper.CreateCodeableConcept(
                    input.Severity,
                    "http://snomed.info/sct");
            }

            // Set code
            if (!string.IsNullOrWhiteSpace(input.ConditionCode) && !string.IsNullOrWhiteSpace(input.ConditionCodeSystem))
            {
                condition.Code = FhirHelper.CreateCodeableConcept(code: input.ConditionCode, system: input.ConditionCodeSystem, display: input.ConditionName);
            }
            else
            {
                condition.Code = new CodeableConcept { Text = input.ConditionName };
            }

            // Set body site
            if (!string.IsNullOrWhiteSpace(input.BodySite))
            {
                condition.BodySite = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = input.BodySite }
                };
            }

            // Set onset
            if (input.OnsetDateTime.HasValue)
            {
                condition.Onset = new FhirDateTime(input.OnsetDateTime.Value);
            }

            // Set abatement
            if (input.AbatementDateTime.HasValue)
            {
                condition.Abatement = new FhirDateTime(input.AbatementDateTime.Value);
            }

            // Set recorded date
            condition.RecordedDateElement = new FhirDateTime(input.RecordedDate);

            // Set recorder
            if (!string.IsNullOrWhiteSpace(input.RecorderId))
            {
                condition.Recorder = FhirHelper.CreateReference("Practitioner", input.RecorderId);
            }

            // Set asserter
            if (!string.IsNullOrWhiteSpace(input.AsserterId))
            {
                condition.Asserter = FhirHelper.CreateReference("Practitioner", input.AsserterId);
            }

            // Set encounter
            if (!string.IsNullOrWhiteSpace(input.EncounterId))
            {
                condition.Encounter = FhirHelper.CreateReference("Encounter", input.EncounterId);
            }

            // Add notes
            if (!string.IsNullOrWhiteSpace(input.Notes))
            {
                condition.Note = new List<Annotation>
                {
                    new Annotation { Text = input.Notes }
                };
            }

            // Add stage if specified
            if (!string.IsNullOrWhiteSpace(input.Stage))
            {
                condition.Stage = new List<Condition.StageComponent>
                {
                    new Condition.StageComponent
                    {
                        Summary = new CodeableConcept { Text = input.Stage }
                    }
                };
            }

            return condition;
        }

        /// <summary>
        /// Transforms ObservationInputModel to FHIR Observation resource
        /// </summary>
        /// <param name="input">Input model containing observation/lab result data</param>
        /// <returns>FHIR Observation resource</returns>
        public static Observation TransformObservation(ObservationInputModel input)
        {
            if (input == null || !input.IsValid())
                throw new ArgumentException("Invalid ObservationInputModel");

            var observation = new Observation
            {
                Id = FhirHelper.GenerateUniqueId("Observation"),
                Meta = new Meta
                {
                    Profile = new[] { input.GetJpClinsProfile() }
                },
                Subject = FhirHelper.CreateReference("Patient", input.PatientId),
                Status = Enum.Parse<ObservationStatus>(input.Status, true)
            };

            // Set code
            observation.Code = FhirHelper.CreateCodeableConcept(
                code: input.ObservationCode,
                system: input.ObservationCodeSystem,
                display: input.ObservationName);

            // Set category
            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                observation.Category = new List<CodeableConcept>
                {
                    FhirHelper.CreateCodeableConcept(input.Category, "http://terminology.hl7.org/CodeSystem/observation-category")
                };
            }

            // Set effective date/time
            observation.Effective = new FhirDateTime(input.EffectiveDateTime);

            // Set issued
            if (input.Issued.HasValue)
            {
                observation.IssuedElement = new Instant(new DateTimeOffset(input.Issued.Value));
            }

            // Set performer
            if (!string.IsNullOrWhiteSpace(input.PerformerId))
            {
                observation.Performer = new List<ResourceReference>
                {
                    FhirHelper.CreateReference("Practitioner", input.PerformerId)
                };
            }

            // Set encounter
            if (!string.IsNullOrWhiteSpace(input.EncounterId))
            {
                observation.Encounter = FhirHelper.CreateReference("Encounter", input.EncounterId);
            }

            // Set value
            if (input.ValueQuantity.HasValue && !string.IsNullOrWhiteSpace(input.Unit))
            {
                observation.Value = FhirHelper.CreateQuantity(input.ValueQuantity.Value, input.Unit);
            }
            else if (!string.IsNullOrWhiteSpace(input.ValueString))
            {
                observation.Value = new FhirString(input.ValueString);
            }
            else if (!string.IsNullOrWhiteSpace(input.ValueCode))
            {
                observation.Value = new CodeableConcept { Text = input.ValueCode };
            }

            // Set interpretation
            if (!string.IsNullOrWhiteSpace(input.Interpretation))
            {
                observation.Interpretation = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = input.Interpretation }
                };
            }

            // Set reference range
            if (input.ReferenceRangeLow.HasValue || input.ReferenceRangeHigh.HasValue || !string.IsNullOrWhiteSpace(input.ReferenceRangeText))
            {
                var referenceRange = new Observation.ReferenceRangeComponent();

                if (input.ReferenceRangeLow.HasValue && !string.IsNullOrWhiteSpace(input.Unit))
                {
                    referenceRange.Low = FhirHelper.CreateQuantity(input.ReferenceRangeLow.Value, input.Unit);
                }

                if (input.ReferenceRangeHigh.HasValue && !string.IsNullOrWhiteSpace(input.Unit))
                {
                    referenceRange.High = FhirHelper.CreateQuantity(input.ReferenceRangeHigh.Value, input.Unit);
                }

                if (!string.IsNullOrWhiteSpace(input.ReferenceRangeText))
                {
                    referenceRange.Text = input.ReferenceRangeText;
                }

                observation.ReferenceRange = new List<Observation.ReferenceRangeComponent> { referenceRange };
            }

            // Set body site
            if (!string.IsNullOrWhiteSpace(input.BodySite))
            {
                observation.BodySite = new CodeableConcept { Text = input.BodySite };
            }

            // Set method
            if (!string.IsNullOrWhiteSpace(input.Method))
            {
                observation.Method = new CodeableConcept { Text = input.Method };
            }

            // Set specimen
            if (!string.IsNullOrWhiteSpace(input.SpecimenId))
            {
                observation.Specimen = FhirHelper.CreateReference("Specimen", input.SpecimenId);
            }

            // Set device
            if (!string.IsNullOrWhiteSpace(input.DeviceId))
            {
                observation.Device = FhirHelper.CreateReference("Device", input.DeviceId);
            }

            // Add notes
            if (!string.IsNullOrWhiteSpace(input.Notes))
            {
                observation.Note = new List<Annotation>
                {
                    new Annotation { Text = input.Notes }
                };
            }

            return observation;
        }

        /// <summary>
        /// Transforms MedicationRequestInputModel to FHIR MedicationRequest resource
        /// </summary>
        /// <param name="input">Input model containing medication request data</param>
        /// <returns>FHIR MedicationRequest resource</returns>
        public static MedicationRequest TransformMedicationRequest(MedicationRequestInputModel input)
        {
            if (input == null || !input.IsValid())
                throw new ArgumentException("Invalid MedicationRequestInputModel");

            var medicationRequest = new MedicationRequest
            {
                Id = FhirHelper.GenerateUniqueId("MedicationRequest"),
                Meta = new Meta
                {
                    Profile = new[] { input.GetJpClinsProfile() }
                },
                Subject = FhirHelper.CreateReference("Patient", input.PatientId),
                Status = Enum.Parse<MedicationRequest.MedicationrequestStatus>(input.Status.Replace("-", ""), true),
                Intent = Enum.Parse<MedicationRequest.MedicationRequestIntent>(input.Intent.Replace("-", ""), true),
                AuthoredOnElement = new FhirDateTime(input.AuthoredOn),
                Requester = FhirHelper.CreateReference("Practitioner", input.RequesterId)
            };

            // Set medication
            if (!string.IsNullOrWhiteSpace(input.MedicationCode) && !string.IsNullOrWhiteSpace(input.MedicationCodeSystem))
            {
                medicationRequest.Medication = FhirHelper.CreateCodeableConcept(
                    code: input.MedicationCode,
                    system: input.MedicationCodeSystem,
                    display: input.MedicationName);
            }
            else
            {
                medicationRequest.Medication = new CodeableConcept { Text = input.MedicationName };
            }

            // Set category
            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                medicationRequest.Category = new List<CodeableConcept>
                {
                    FhirHelper.CreateCodeableConcept(input.Category, "http://terminology.hl7.org/CodeSystem/medicationrequest-category")
                };
            }

            // Set priority
            if (!string.IsNullOrWhiteSpace(input.Priority))
            {
                medicationRequest.Priority = Enum.Parse<RequestPriority>(input.Priority, true);
            }

            // Set dosage instruction
            if (input.DoseQuantity.HasValue || !string.IsNullOrWhiteSpace(input.Frequency))
            {
                var dosage = FhirHelper.CreateDosage(
                    input.Route,
                    input.DoseQuantity,
                    input.DoseUnit,
                    input.Frequency,
                    input.PatientInstructions);

                medicationRequest.DosageInstruction = new List<Dosage> { dosage };
            }

            // Set dispense request
            if (input.DispenseQuantity.HasValue || input.NumberOfRepeatsAllowed.HasValue)
            {
                var dispenseRequest = new MedicationRequest.DispenseRequestComponent();

                if (input.DispenseQuantity.HasValue && !string.IsNullOrWhiteSpace(input.DispenseUnit))
                {
                    dispenseRequest.Quantity = FhirHelper.CreateQuantity(input.DispenseQuantity.Value, input.DispenseUnit);
                }

                if (input.NumberOfRepeatsAllowed.HasValue)
                {
                    dispenseRequest.NumberOfRepeatsAllowed = input.NumberOfRepeatsAllowed.Value;
                }

                if (input.EffectivePeriodStart.HasValue || input.EffectivePeriodEnd.HasValue)
                {
                    dispenseRequest.ValidityPeriod = FhirHelper.CreatePeriod(input.EffectivePeriodStart, input.EffectivePeriodEnd);
                }

                if (!string.IsNullOrWhiteSpace(input.DispenserId))
                {
                    dispenseRequest.Performer = FhirHelper.CreateReference("Organization", input.DispenserId);
                }

                medicationRequest.DispenseRequest = dispenseRequest;
            }

            // Set encounter
            if (!string.IsNullOrWhiteSpace(input.EncounterId))
            {
                medicationRequest.Encounter = FhirHelper.CreateReference("Encounter", input.EncounterId);
            }

            // Set reason
            if (!string.IsNullOrWhiteSpace(input.ReasonCode))
            {
                medicationRequest.ReasonCode = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = input.ReasonCode }
                };
            }

            if (!string.IsNullOrWhiteSpace(input.ReasonReference))
            {
                medicationRequest.ReasonReference = new List<ResourceReference>
                {
                    new ResourceReference { Reference = input.ReasonReference }
                };
            }

            // Add notes
            if (!string.IsNullOrWhiteSpace(input.Notes))
            {
                medicationRequest.Note = new List<Annotation>
                {
                    new Annotation { Text = input.Notes }
                };
            }

            return medicationRequest;
        }

        // TODO: JP-CLINS Specific Transformation Notes:
        // 1. Ensure all resources follow JP-CLINS profile constraints
        // 2. Apply Japanese-specific extensions where required
        // 3. Use appropriate Japanese coding systems for codes
        // 4. Validate against JP-CLINS terminology bindings
        // 5. Consider Japanese healthcare workflow requirements
        // 6. Apply occupational health specific rules for checkup documents
        // 7. Ensure proper linkage between related resources
        // 8. Apply Japanese privacy and security requirements
    }
}