using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.Models.InputModels
{
    /// <summary>
    /// Input model for MedicationRequest data to be converted to JP_MedicationRequest-eCS FHIR resource
    /// This class represents minimal data needed from hospital systems to create FHIR MedicationRequest resources
    /// </summary>
    public class MedicationRequestInputModel
    {
        /// <summary>
        /// Patient identifier this medication request is for
        /// </summary>
        [JsonProperty("patientId")]
        [Required]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// Medication code
        /// JP-CLINS Profile: Should use YJ code (Yakka code), HOT code, or Japanese medication codes
        /// </summary>
        [JsonProperty("medicationCode")]
        public string? MedicationCode { get; set; }

        /// <summary>
        /// Coding system for the medication code
        /// JP-CLINS: YJ codes ("urn:oid:1.2.392.100495.20.2.74"), HOT codes, etc.
        /// </summary>
        [JsonProperty("medicationCodeSystem")]
        public string? MedicationCodeSystem { get; set; }

        /// <summary>
        /// Human-readable medication name
        /// </summary>
        [JsonProperty("medicationName")]
        [Required]
        [MaxLength(300)]
        public string MedicationName { get; set; } = string.Empty;

        /// <summary>
        /// Status of the medication request: active, on-hold, cancelled, completed, entered-in-error, stopped, draft, unknown
        /// </summary>
        [JsonProperty("status")]
        [Required]
        public string Status { get; set; } = "active";

        /// <summary>
        /// Intent of the request: proposal, plan, order, original-order, reflex-order, filler-order, instance-order, option
        /// JP-CLINS: Typically "order" for prescriptions
        /// </summary>
        [JsonProperty("intent")]
        [Required]
        public string Intent { get; set; } = "order";

        /// <summary>
        /// Category of medication request: inpatient, outpatient, community, discharge
        /// JP-CLINS: Should categorize based on Japanese healthcare context
        /// </summary>
        [JsonProperty("category")]
        public string? Category { get; set; } = "outpatient";

        /// <summary>
        /// Priority of the request: routine, urgent, stat, asap
        /// </summary>
        [JsonProperty("priority")]
        public string? Priority { get; set; } = "routine";

        /// <summary>
        /// Dosage amount (e.g., 10)
        /// </summary>
        [JsonProperty("doseQuantity")]
        public decimal? DoseQuantity { get; set; }

        /// <summary>
        /// Dosage unit (e.g., mg, ml, tablet)
        /// JP-CLINS: Should use Japanese pharmaceutical units
        /// </summary>
        [JsonProperty("doseUnit")]
        public string? DoseUnit { get; set; }

        /// <summary>
        /// Route of administration (e.g., oral, injection, topical)
        /// JP-CLINS: Should use Japanese route coding systems
        /// </summary>
        [JsonProperty("route")]
        public string? Route { get; set; }

        /// <summary>
        /// Frequency of administration (e.g., "twice daily", "as needed")
        /// JP-CLINS: Should follow Japanese dosing conventions
        /// </summary>
        [JsonProperty("frequency")]
        public string? Frequency { get; set; }

        /// <summary>
        /// Duration of treatment
        /// </summary>
        [JsonProperty("duration")]
        public string? Duration { get; set; }

        /// <summary>
        /// Total quantity to be dispensed
        /// </summary>
        [JsonProperty("dispenseQuantity")]
        public decimal? DispenseQuantity { get; set; }

        /// <summary>
        /// Unit for dispense quantity
        /// </summary>
        [JsonProperty("dispenseUnit")]
        public string? DispenseUnit { get; set; }

        /// <summary>
        /// Number of allowed refills
        /// JP-CLINS: Consider Japanese prescription refill regulations
        /// </summary>
        [JsonProperty("numberOfRepeatsAllowed")]
        public int? NumberOfRepeatsAllowed { get; set; }

        /// <summary>
        /// Date when the medication request was written
        /// </summary>
        [JsonProperty("authoredOn")]
        [Required]
        public DateTime AuthoredOn { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who prescribed this medication
        /// </summary>
        [JsonProperty("requesterId")]
        [Required]
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// Encounter context for this medication request
        /// </summary>
        [JsonProperty("encounterId")]
        public string? EncounterId { get; set; }

        /// <summary>
        /// Reason for prescribing (condition or diagnosis)
        /// JP-CLINS: Should reference Japanese diagnosis codes
        /// </summary>
        [JsonProperty("reasonCode")]
        public string? ReasonCode { get; set; }

        /// <summary>
        /// Reference to condition/diagnosis motivating this prescription
        /// </summary>
        [JsonProperty("reasonReference")]
        public string? ReasonReference { get; set; }

        /// <summary>
        /// Additional instructions for patient
        /// </summary>
        [JsonProperty("patientInstructions")]
        [MaxLength(1000)]
        public string? PatientInstructions { get; set; }

        /// <summary>
        /// Additional notes about the prescription
        /// </summary>
        [JsonProperty("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Date range when medication should be taken
        /// </summary>
        [JsonProperty("effectivePeriodStart")]
        public DateTime? EffectivePeriodStart { get; set; }

        /// <summary>
        /// End date for medication treatment
        /// </summary>
        [JsonProperty("effectivePeriodEnd")]
        public DateTime? EffectivePeriodEnd { get; set; }

        /// <summary>
        /// Pharmacy or dispenser
        /// </summary>
        [JsonProperty("dispenserId")]
        public string? DispenserId { get; set; }

        /// <summary>
        /// Validates the input model according to JP-CLINS requirements
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(PatientId) ||
                string.IsNullOrWhiteSpace(MedicationName) ||
                string.IsNullOrWhiteSpace(RequesterId))
                return false;

            var validStatuses = new[] { "active", "on-hold", "cancelled", "completed", "entered-in-error", "stopped", "draft", "unknown" };
            if (!validStatuses.Contains(Status?.ToLower()))
                return false;

            var validIntents = new[] { "proposal", "plan", "order", "original-order", "reflex-order", "filler-order", "instance-order", "option" };
            if (!validIntents.Contains(Intent?.ToLower()))
                return false;

            // Effective period validation
            if (EffectivePeriodStart.HasValue && EffectivePeriodEnd.HasValue && EffectivePeriodStart > EffectivePeriodEnd)
                return false;

            // Dose validation
            if (DoseQuantity.HasValue && DoseQuantity <= 0)
                return false;

            if (DispenseQuantity.HasValue && DispenseQuantity <= 0)
                return false;

            // JP-CLINS specific validations
            if (!ValidateJapaneseSpecificRules())
                return false;

            return true;
        }

        /// <summary>
        /// Validates JP-CLINS specific rules for medication requests
        /// </summary>
        /// <returns>True if passes JP-CLINS validation</returns>
        private bool ValidateJapaneseSpecificRules()
        {
            // 1. Validate Japanese medication codes (YJ codes, HOT codes)
            if (!string.IsNullOrWhiteSpace(MedicationCode) && !string.IsNullOrWhiteSpace(MedicationCodeSystem))
            {
                if (!Utilities.FhirHelper.ValidateJapaneseMedicationCode(MedicationCode, MedicationCodeSystem))
                    return false;
            }

            // 2. Validate Japanese pharmaceutical route codes
            if (!string.IsNullOrWhiteSpace(Route))
            {
                if (!Utilities.FhirHelper.ValidateJapaneseRouteCode(Route))
                    return false;
            }

            // 3. Validate Japanese dosing frequency patterns
            if (!string.IsNullOrWhiteSpace(Frequency))
            {
                if (!Utilities.FhirHelper.ValidateJapaneseDosingFrequency(Frequency))
                    return false;
            }

            // 4. Validate Japanese pharmaceutical units
            if (!string.IsNullOrWhiteSpace(DoseUnit))
            {
                if (!ValidateJapanesePharmaceuticalUnit(DoseUnit))
                    return false;
            }

            // 5. Validate controlled substance requirements
            if (IsControlledSubstance())
            {
                // Controlled substances require additional documentation
                if (string.IsNullOrWhiteSpace(ReasonCode) && string.IsNullOrWhiteSpace(ReasonReference))
                    return false;

                // Controlled substances should have limited dispense quantities
                if (DispenseQuantity.HasValue && DispenseQuantity > 30) // Max 30 days supply typically
                    return false;
            }

            // 6. Validate elderly dosing adjustments (if age context available)
            if (DoseQuantity.HasValue && IsElderly())
            {
                if (!ValidateElderlyDosingAdjustments())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates Japanese pharmaceutical units
        /// </summary>
        /// <param name="unit">Unit to validate</param>
        /// <returns>True if valid Japanese pharmaceutical unit</returns>
        private bool ValidateJapanesePharmaceuticalUnit(string unit)
        {
            var validUnits = new[]
            {
                // Japanese units
                "錠", "カプセル", "包", "袋", "本", "瓶", "管", "枚", "滴", "吸入", "シート", "バイアル",
                // International units commonly used in Japan
                "mg", "g", "mL", "L", "tablet", "capsule", "patch", "drop", "puff", "IU", "mcg", "μg"
            };

            return validUnits.Contains(unit, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if dosing is for elderly patient (placeholder - would need patient context)
        /// </summary>
        /// <returns>True if elderly dosing considerations apply</returns>
        private bool IsElderly()
        {
            // This would typically check patient age from context
            // For now, return false as we don't have patient context in this input model
            return false;
        }

        /// <summary>
        /// Validates elderly dosing adjustments per Japanese guidelines
        /// </summary>
        /// <returns>True if appropriate for elderly patients</returns>
        private bool ValidateElderlyDosingAdjustments()
        {
            // Japanese elderly dosing guidelines typically recommend:
            // - Start low, go slow approach
            // - Reduced doses for certain drug classes
            // - More frequent monitoring

            if (DoseQuantity.HasValue)
            {
                // Common drugs that require elderly dose reduction
                var elderlyReducedDrugs = new[]
                {
                    "warfarin", "ワルファリン", "digoxin", "ジゴキシン", "lithium", "リチウム",
                    "benzodiazepine", "ベンゾジアゼピン", "diuretic", "利尿薬"
                };

                var medicationText = $"{MedicationCode} {MedicationName}".ToLower();
                var requiresReduction = elderlyReducedDrugs.Any(drug =>
                    medicationText.Contains(drug, StringComparison.OrdinalIgnoreCase));

                if (requiresReduction)
                {
                    // Could implement specific dose checking logic here
                    // For now, just ensure notes are present for elderly considerations
                    return !string.IsNullOrWhiteSpace(Notes) || !string.IsNullOrWhiteSpace(PatientInstructions);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the appropriate JP-CLINS profile URL for this MedicationRequest
        /// </summary>
        /// <returns>JP-CLINS profile URL</returns>
        public string GetJpClinsProfile()
        {
            return Constants.JpClinsConstants.ResourceProfiles.MedicationRequesteCS;
        }

        /// <summary>
        /// Determines if this is a controlled substance based on Japanese regulations
        /// </summary>
        /// <returns>True if controlled substance, false otherwise</returns>
        public bool IsControlledSubstance()
        {
            return Utilities.FhirHelper.IsJapaneseControlledSubstance(MedicationCode ?? "", MedicationName);
        }

        /// <summary>
        /// Gets the expected treatment duration in days
        /// </summary>
        /// <returns>Duration in days, null if cannot determine</returns>
        public int? GetTreatmentDurationDays()
        {
            if (EffectivePeriodStart.HasValue && EffectivePeriodEnd.HasValue)
            {
                return (int)(EffectivePeriodEnd.Value - EffectivePeriodStart.Value).TotalDays;
            }
            return null;
        }

        // TODO: JP-CLINS Specific Implementation Notes:
        // 1. Medication codes should prioritize YJ codes (Yakka codes) for Japanese medications
        // 2. Route should use Japanese pharmaceutical route coding systems
        // 3. Frequency should follow Japanese dosing conventions (朝・昼・夕・寝前)
        // 4. Units should use Japanese pharmaceutical units
        // 5. Consider Japanese prescription regulations and controlled substance laws
        // 6. Dispense quantity should consider Japanese pharmacy dispensing rules
        // 7. Extensions may be needed for Japanese-specific prescription information
        // 8. Link to relevant diagnoses and treatment plans
        // 9. Consider drug interaction checking with Japanese medication databases
        // 10. Support for Japanese generic substitution rules
        // 11. Integration with Japanese health insurance system requirements
        // 12. Consider elderly dosing adjustments per Japanese guidelines
    }
}