using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.Models.InputModels
{
    /// <summary>
    /// Input model for Observation data to be converted to JP_Observation-LabResult-eCS FHIR resource
    /// This class represents minimal data needed from hospital systems to create FHIR Observation resources
    /// </summary>
    public class ObservationInputModel
    {
        /// <summary>
        /// Patient identifier this observation belongs to
        /// </summary>
        [JsonProperty("patientId")]
        [Required]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// Observation code (what was observed/measured)
        /// JP-CLINS Profile: Should use JLAC10 for lab tests, Japanese vital signs codes
        /// </summary>
        [JsonProperty("observationCode")]
        [Required]
        public string ObservationCode { get; set; } = string.Empty;

        /// <summary>
        /// Coding system for the observation code
        /// JP-CLINS: JLAC10 for lab tests, LOINC for international compatibility
        /// </summary>
        [JsonProperty("observationCodeSystem")]
        [Required]
        public string ObservationCodeSystem { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name/description of what was observed
        /// </summary>
        [JsonProperty("observationName")]
        [Required]
        [MaxLength(200)]
        public string ObservationName { get; set; } = string.Empty;

        /// <summary>
        /// Status of the observation: registered, preliminary, final, amended, corrected, cancelled, entered-in-error, unknown
        /// </summary>
        [JsonProperty("status")]
        [Required]
        public string Status { get; set; } = "final";

        /// <summary>
        /// Category of observation: vital-signs, laboratory, survey, social-history, etc.
        /// JP-CLINS: Should categorize based on Japanese healthcare practice
        /// </summary>
        [JsonProperty("category")]
        public string? Category { get; set; } = "laboratory";

        /// <summary>
        /// Measured value (numeric)
        /// </summary>
        [JsonProperty("valueQuantity")]
        public decimal? ValueQuantity { get; set; }

        /// <summary>
        /// Unit of measurement
        /// JP-CLINS: Should use UCUM units or Japanese standard units
        /// </summary>
        [JsonProperty("unit")]
        public string? Unit { get; set; }

        /// <summary>
        /// Text/coded value for non-numeric observations
        /// </summary>
        [JsonProperty("valueString")]
        public string? ValueString { get; set; }

        /// <summary>
        /// Coded value for categorical observations
        /// </summary>
        [JsonProperty("valueCode")]
        public string? ValueCode { get; set; }

        /// <summary>
        /// Date/time when observation was made
        /// </summary>
        [JsonProperty("effectiveDateTime")]
        [Required]
        public DateTime EffectiveDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date/time when observation was recorded
        /// </summary>
        [JsonProperty("issued")]
        public DateTime? Issued { get; set; }

        /// <summary>
        /// Who performed this observation
        /// </summary>
        [JsonProperty("performerId")]
        public string? PerformerId { get; set; }

        /// <summary>
        /// Encounter context for this observation
        /// </summary>
        [JsonProperty("encounterId")]
        public string? EncounterId { get; set; }

        /// <summary>
        /// Reference range low value
        /// JP-CLINS: Should use Japanese laboratory reference ranges
        /// </summary>
        [JsonProperty("referenceRangeLow")]
        public decimal? ReferenceRangeLow { get; set; }

        /// <summary>
        /// Reference range high value
        /// </summary>
        [JsonProperty("referenceRangeHigh")]
        public decimal? ReferenceRangeHigh { get; set; }

        /// <summary>
        /// Reference range text (for complex ranges)
        /// </summary>
        [JsonProperty("referenceRangeText")]
        public string? ReferenceRangeText { get; set; }

        /// <summary>
        /// Interpretation of the observation result: normal, abnormal, high, low, etc.
        /// JP-CLINS: Should use Japanese interpretation codes
        /// </summary>
        [JsonProperty("interpretation")]
        public string? Interpretation { get; set; }

        /// <summary>
        /// Body site where observation was made
        /// JP-CLINS: Should use Japanese anatomical terminology
        /// </summary>
        [JsonProperty("bodySite")]
        public string? BodySite { get; set; }

        /// <summary>
        /// Method used to make the observation
        /// JP-CLINS: Should reference Japanese medical device codes
        /// </summary>
        [JsonProperty("method")]
        public string? Method { get; set; }

        /// <summary>
        /// Specimen used for the observation (for lab tests)
        /// </summary>
        [JsonProperty("specimenId")]
        public string? SpecimenId { get; set; }

        /// <summary>
        /// Additional notes about the observation
        /// </summary>
        [JsonProperty("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Device used to make the observation
        /// </summary>
        [JsonProperty("deviceId")]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Validates the input model according to JP-CLINS requirements
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(PatientId) ||
                string.IsNullOrWhiteSpace(ObservationCode) ||
                string.IsNullOrWhiteSpace(ObservationCodeSystem) ||
                string.IsNullOrWhiteSpace(ObservationName))
                return false;

            var validStatuses = new[] { "registered", "preliminary", "final", "amended", "corrected", "cancelled", "entered-in-error", "unknown" };
            if (!validStatuses.Contains(Status?.ToLower()))
                return false;

            // Must have at least one value
            if (!ValueQuantity.HasValue && string.IsNullOrWhiteSpace(ValueString) && string.IsNullOrWhiteSpace(ValueCode))
                return false;

            // If numeric value, should have unit
            if (ValueQuantity.HasValue && string.IsNullOrWhiteSpace(Unit))
                return false;

            // Reference range validation
            if (ReferenceRangeLow.HasValue && ReferenceRangeHigh.HasValue && ReferenceRangeLow > ReferenceRangeHigh)
                return false;

            // JP-CLINS specific validations
            if (!ValidateJapaneseSpecificRules())
                return false;

            return true;
        }

        /// <summary>
        /// Validates JP-CLINS specific rules for observations
        /// </summary>
        /// <returns>True if passes JP-CLINS validation</returns>
        private bool ValidateJapaneseSpecificRules()
        {
            // 1. Validate JLAC10 codes for lab tests
            if (ObservationCodeSystem == "urn:oid:1.2.392.200119.4.504")
            {
                if (!Utilities.FhirHelper.ValidateJLAC10Code(ObservationCode))
                    return false;
            }

            // 2. Validate Japanese vital signs ranges
            if (ValueQuantity.HasValue && !string.IsNullOrWhiteSpace(Unit))
            {
                var vitalType = DetermineVitalSignType();
                if (!string.IsNullOrWhiteSpace(vitalType))
                {
                    if (!Utilities.FhirHelper.ValidateJapaneseVitalSigns(vitalType, ValueQuantity.Value, Unit))
                        return false;
                }
            }

            // 3. Validate Japanese interpretation codes
            if (!string.IsNullOrWhiteSpace(Interpretation))
            {
                if (!ValidateJapaneseInterpretationCode(Interpretation))
                    return false;
            }

            // 4. Validate Japanese reference ranges
            if (!ValidateJapaneseReferenceRanges())
                return false;

            // 5. Validate occupational health requirements
            if (Category?.ToLower() == "survey" && IsOccupationalHealthContext())
            {
                if (!ValidateOccupationalHealthRequirements())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines the vital sign type based on observation code and name
        /// </summary>
        /// <returns>Vital sign type for validation</returns>
        private string DetermineVitalSignType()
        {
            var codeAndName = $"{ObservationCode} {ObservationName}".ToLower();

            if (codeAndName.Contains("血圧") || codeAndName.Contains("blood pressure") ||
                codeAndName.Contains("systolic") || codeAndName.Contains("収縮期"))
                return "systolic-bp";

            if (codeAndName.Contains("拡張期") || codeAndName.Contains("diastolic"))
                return "diastolic-bp";

            if (codeAndName.Contains("心拍") || codeAndName.Contains("heart rate") || codeAndName.Contains("pulse"))
                return "heart-rate";

            if (codeAndName.Contains("体温") || codeAndName.Contains("temperature"))
                return "body-temperature";

            if (codeAndName.Contains("身長") || codeAndName.Contains("height"))
                return "height";

            if (codeAndName.Contains("体重") || codeAndName.Contains("weight"))
                return "weight";

            if (codeAndName.Contains("bmi") || codeAndName.Contains("体格"))
                return "bmi";

            return string.Empty;
        }

        /// <summary>
        /// Validates Japanese interpretation codes
        /// </summary>
        /// <param name="interpretation">Interpretation code to validate</param>
        /// <returns>True if valid Japanese interpretation</returns>
        private bool ValidateJapaneseInterpretationCode(string interpretation)
        {
            var validInterpretations = new[]
            {
                // Standard FHIR interpretation codes
                "normal", "abnormal", "high", "low", "critical", "positive", "negative",
                // Japanese interpretation terms
                "正常", "異常", "高値", "低値", "要注意", "要精密検査", "要治療", "陽性", "陰性",
                "基準値内", "基準値外", "軽度異常", "中等度異常", "重度異常"
            };

            return validInterpretations.Any(valid =>
                interpretation.Contains(valid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Japanese laboratory reference ranges
        /// </summary>
        /// <returns>True if reference ranges are appropriate for Japanese population</returns>
        private bool ValidateJapaneseReferenceRanges()
        {
            // Japanese population may have different reference ranges for certain tests
            var japaneseSpecificTests = new Dictionary<string, (decimal min, decimal max)>
            {
                { "glucose", (70, 110) }, // mg/dL
                { "hba1c", (4.6m, 6.2m) }, // %
                { "cholesterol", (150, 219) }, // mg/dL
                { "triglyceride", (30, 149) }, // mg/dL
                { "creatinine-male", (0.65m, 1.07m) }, // mg/dL
                { "creatinine-female", (0.46m, 0.79m) }, // mg/dL
            };

            var testName = ObservationName.ToLower();
            foreach (var test in japaneseSpecificTests)
            {
                if (testName.Contains(test.Key))
                {
                    if (ReferenceRangeLow.HasValue && ReferenceRangeLow < test.Value.min)
                        return false;
                    if (ReferenceRangeHigh.HasValue && ReferenceRangeHigh > test.Value.max)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if this observation is in occupational health context
        /// </summary>
        /// <returns>True if occupational health related</returns>
        private bool IsOccupationalHealthContext()
        {
            var contextTerms = new[] { "occupational", "workplace", "職業", "職場", "産業医", "健康診断" };
            var searchText = $"{ObservationName} {Notes}".ToLower();

            return contextTerms.Any(term => searchText.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates occupational health requirements per Japanese regulations
        /// </summary>
        /// <returns>True if meets occupational health standards</returns>
        private bool ValidateOccupationalHealthRequirements()
        {
            // Japanese occupational health regulations require specific observations
            var requiredOccupationalTests = new[]
            {
                "chest x-ray", "胸部X線", "hearing test", "聴力検査", "vision test", "視力検査",
                "blood pressure", "血圧", "urinalysis", "尿検査", "blood test", "血液検査"
            };

            // Check if this is a required occupational health test
            var testName = ObservationName.ToLower();
            var isRequiredTest = requiredOccupationalTests.Any(required =>
                testName.Contains(required, StringComparison.OrdinalIgnoreCase));

            if (isRequiredTest)
            {
                // Required tests should have performer information
                if (string.IsNullOrWhiteSpace(PerformerId))
                    return false;

                // Required tests should have final or amended status
                if (Status != "final" && Status != "amended")
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the appropriate JP-CLINS profile URL for this Observation
        /// </summary>
        /// <returns>JP-CLINS profile URL</returns>
        public string GetJpClinsProfile()
        {
            return Constants.JpClinsConstants.ResourceProfiles.ObservationLabResult;
        }

        /// <summary>
        /// Determines if this observation value is within the reference range
        /// </summary>
        /// <returns>True if within range, false if outside range, null if cannot determine</returns>
        public bool? IsWithinReferenceRange()
        {
            if (!ValueQuantity.HasValue || (!ReferenceRangeLow.HasValue && !ReferenceRangeHigh.HasValue))
                return null;

            if (ReferenceRangeLow.HasValue && ValueQuantity < ReferenceRangeLow)
                return false;

            if (ReferenceRangeHigh.HasValue && ValueQuantity > ReferenceRangeHigh)
                return false;

            return true;
        }

        // TODO: JP-CLINS Specific Implementation Notes:
        // 1. Observation codes should prioritize JLAC10 for Japanese lab tests
        // 2. Units should use UCUM or Japanese standard units
        // 3. Reference ranges should follow Japanese laboratory standards
        // 4. Interpretation codes should use Japanese abnormal flags
        // 5. Method should reference Japanese medical device classification
        // 6. Body site should use Japanese anatomical coding systems
        // 7. Consider Japanese health checkup requirements for occupational health
        // 8. Extensions may be needed for Japanese-specific observation characteristics
        // 9. Link to relevant procedures and diagnostic reports
        // 10. Consider Japanese vital signs normal ranges and measurement practices
    }
}