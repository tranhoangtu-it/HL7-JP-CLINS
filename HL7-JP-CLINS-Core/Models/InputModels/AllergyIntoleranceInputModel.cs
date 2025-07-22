using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.Models.InputModels
{
    /// <summary>
    /// Input model for AllergyIntolerance data to be converted to JP_AllergyIntolerance_eCS FHIR resource
    /// This class represents minimal data needed from hospital systems to create FHIR AllergyIntolerance resources
    /// </summary>
    public class AllergyIntoleranceInputModel
    {
        /// <summary>
        /// Patient identifier this allergy/intolerance belongs to
        /// </summary>
        [JsonProperty("patientId")]
        [Required]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// Name/description of the substance causing allergy/intolerance
        /// </summary>
        [JsonProperty("substanceName")]
        [Required]
        [MaxLength(200)]
        public string SubstanceName { get; set; } = string.Empty;

        /// <summary>
        /// Substance code from Japanese coding system (if available)
        /// JP-CLINS Profile: Should use Japanese medication codes (YJ code, HOT code) for medications
        /// </summary>
        [JsonProperty("substanceCode")]
        public string? SubstanceCode { get; set; }

        /// <summary>
        /// Coding system for the substance code
        /// JP-CLINS: Typically "urn:oid:1.2.392.100495.20.2.74" for YJ codes
        /// </summary>
        [JsonProperty("substanceCodeSystem")]
        public string? SubstanceCodeSystem { get; set; }

        /// <summary>
        /// Category of allergen: medication, food, environment, biologic
        /// JP-CLINS Profile: Must be one of the valid category values
        /// </summary>
        [JsonProperty("category")]
        [Required]
        public string Category { get; set; } = "medication"; // medication, food, environment, biologic

        /// <summary>
        /// Type of intolerance: allergy, intolerance, unspecified
        /// </summary>
        [JsonProperty("type")]
        public string? Type { get; set; } = "allergy";

        /// <summary>
        /// Clinical status: active, inactive, resolved
        /// JP-CLINS: Should follow FHIR AllergyIntolerance clinical status codes
        /// </summary>
        [JsonProperty("clinicalStatus")]
        public string ClinicalStatus { get; set; } = "active";

        /// <summary>
        /// Verification status: unconfirmed, confirmed, refuted, entered-in-error
        /// </summary>
        [JsonProperty("verificationStatus")]
        public string VerificationStatus { get; set; } = "confirmed";

        /// <summary>
        /// Severity of the worst reaction: mild, moderate, severe
        /// JP-CLINS Profile: Should use Japanese severity coding when available
        /// </summary>
        [JsonProperty("reactionSeverity")]
        public string? ReactionSeverity { get; set; }

        /// <summary>
        /// Description of the allergic reaction
        /// </summary>
        [JsonProperty("reactionDescription")]
        [MaxLength(1000)]
        public string? ReactionDescription { get; set; }

        /// <summary>
        /// Manifestation of the reaction (e.g., rash, nausea, anaphylaxis)
        /// JP-CLINS: Should use Japanese symptom codes when available
        /// </summary>
        [JsonProperty("manifestation")]
        public string? Manifestation { get; set; }

        /// <summary>
        /// Date when this allergy was first recorded
        /// </summary>
        [JsonProperty("recordedDate")]
        public DateTime? RecordedDate { get; set; }

        /// <summary>
        /// Date of the last known occurrence of a reaction
        /// </summary>
        [JsonProperty("lastOccurrence")]
        public DateTime? LastOccurrence { get; set; }

        /// <summary>
        /// Who recorded this allergy information
        /// </summary>
        [JsonProperty("recorderId")]
        public string? RecorderId { get; set; }

        /// <summary>
        /// Additional notes about the allergy/intolerance
        /// </summary>
        [JsonProperty("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Validates the input model according to JP-CLINS requirements
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(PatientId) || string.IsNullOrWhiteSpace(SubstanceName))
                return false;

            var validCategories = new[] { "medication", "food", "environment", "biologic" };
            if (!validCategories.Contains(Category?.ToLower()))
                return false;

            var validClinicalStatuses = new[] { "active", "inactive", "resolved" };
            if (!string.IsNullOrWhiteSpace(ClinicalStatus) && !validClinicalStatuses.Contains(ClinicalStatus.ToLower()))
                return false;

            // JP-CLINS specific validations
            if (!ValidateJapaneseSpecificRules())
                return false;

            return true;
        }

        /// <summary>
        /// Validates JP-CLINS specific rules for allergy/intolerance
        /// </summary>
        /// <returns>True if passes JP-CLINS validation</returns>
        private bool ValidateJapaneseSpecificRules()
        {
            // 1. Validate Japanese medication codes for substance (YJ code, HOT code)
            if (Category?.ToLower() == "medication" &&
                !string.IsNullOrWhiteSpace(SubstanceCode) &&
                !string.IsNullOrWhiteSpace(SubstanceCodeSystem))
            {
                if (!Utilities.FhirHelper.ValidateJapaneseMedicationCode(SubstanceCode, SubstanceCodeSystem))
                    return false;
            }

            // 2. Validate Japanese symptom coding systems for manifestations
            if (!string.IsNullOrWhiteSpace(Manifestation))
            {
                if (!ValidateJapaneseSymptomCoding(Manifestation))
                    return false;
            }

            // 3. Validate Japanese severity scales
            if (!string.IsNullOrWhiteSpace(ReactionSeverity))
            {
                if (!ValidateJapaneseSeverityScale(ReactionSeverity))
                    return false;
            }

            // 4. Validate workplace exposure allergies for occupational health contexts
            if (IsWorkplaceExposureAllergy())
            {
                if (!ValidateWorkplaceExposureRequirements())
                    return false;
            }

            // 5. Validate Japanese food allergen classifications
            if (Category?.ToLower() == "food")
            {
                if (!ValidateJapaneseFoodAllergen())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates Japanese symptom coding systems for manifestations
        /// </summary>
        /// <param name="manifestation">Manifestation to validate</param>
        /// <returns>True if valid Japanese symptom coding</returns>
        private bool ValidateJapaneseSymptomCoding(string manifestation)
        {
            var validJapaneseSymptoms = new[]
            {
                // Common allergic reactions in Japanese
                "発疹", "蕁麻疹", "湿疹", "紅斑", "水疱", "浮腫", "腫脹",
                "呼吸困難", "喘息", "咳嗽", "鼻炎", "結膜炎", "流涙",
                "嘔吐", "下痢", "腹痛", "嘔気", "胃腸症状",
                "アナフィラキシー", "ショック", "血圧低下", "意識消失",
                // English equivalents commonly used
                "rash", "urticaria", "eczema", "erythema", "blister", "edema", "swelling",
                "dyspnea", "asthma", "cough", "rhinitis", "conjunctivitis", "lacrimation",
                "vomiting", "diarrhea", "abdominal pain", "nausea", "gastrointestinal",
                "anaphylaxis", "shock", "hypotension", "loss of consciousness"
            };

            return validJapaneseSymptoms.Any(symptom =>
                manifestation.Contains(symptom, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Japanese severity scales for allergic reactions
        /// </summary>
        /// <param name="severity">Severity to validate</param>
        /// <returns>True if valid Japanese severity scale</returns>
        private bool ValidateJapaneseSeverityScale(string severity)
        {
            var validSeverities = new[]
            {
                // Standard severity levels
                "mild", "moderate", "severe", "life-threatening",
                // Japanese severity terms
                "軽度", "中等度", "重度", "重篤", "生命に関わる",
                // Japanese clinical grading
                "グレード1", "グレード2", "グレード3", "グレード4",
                "grade 1", "grade 2", "grade 3", "grade 4"
            };

            return validSeverities.Any(valid =>
                severity.Contains(valid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if this is a workplace exposure allergy
        /// </summary>
        /// <returns>True if workplace/occupational allergy</returns>
        private bool IsWorkplaceExposureAllergy()
        {
            var workplaceTerms = new[]
            {
                "workplace", "occupational", "work-related", "職場", "職業性", "労働",
                "industrial", "factory", "office", "工場", "事務所", "産業"
            };

            var searchText = $"{SubstanceName} {ReactionDescription} {Notes}".ToLower();
            return workplaceTerms.Any(term =>
                searchText.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates workplace exposure allergy requirements
        /// </summary>
        /// <returns>True if meets workplace exposure documentation standards</returns>
        private bool ValidateWorkplaceExposureRequirements()
        {
            // Workplace allergies require additional documentation for occupational health

            // Must have detailed substance information
            if (string.IsNullOrWhiteSpace(SubstanceCode) && string.IsNullOrWhiteSpace(SubstanceCodeSystem))
                return false;

            // Must have reaction description for occupational health assessment
            if (string.IsNullOrWhiteSpace(ReactionDescription))
                return false;

            // Should have recorder information (occupational health professional)
            if (string.IsNullOrWhiteSpace(RecorderId))
                return false;

            // Must be confirmed for workplace safety measures
            if (VerificationStatus != "confirmed")
                return false;

            return true;
        }

        /// <summary>
        /// Validates Japanese food allergen classifications
        /// </summary>
        /// <returns>True if valid Japanese food allergen</returns>
        private bool ValidateJapaneseFoodAllergen()
        {
            // Japan has specific food allergen labeling requirements (28 allergens)
            var japaneseRequiredAllergens = new[]
            {
                // 特定原材料7品目 (7 major allergens)
                "卵", "乳", "小麦", "そば", "落花生", "えび", "かに",
                "egg", "milk", "wheat", "buckwheat", "peanut", "shrimp", "crab",
                
                // 特定原材料に準ずるもの21品目 (21 additional allergens)
                "アーモンド", "あわび", "いか", "いくら", "オレンジ", "カシューナッツ",
                "キウイフルーツ", "牛肉", "くるみ", "ごま", "さけ", "さば", "大豆",
                "鶏肉", "バナナ", "豚肉", "まつたけ", "もも", "やまいも", "りんご", "ゼラチン",
                "almond", "abalone", "squid", "salmon roe", "orange", "cashew",
                "kiwi", "beef", "walnut", "sesame", "salmon", "mackerel", "soy",
                "chicken", "banana", "pork", "matsutake", "peach", "yam", "apple", "gelatin"
            };

            var substanceName = SubstanceName.ToLower();

            // If it's a known Japanese allergen, it's valid
            if (japaneseRequiredAllergens.Any(allergen =>
                substanceName.Contains(allergen, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // If it's not a known allergen but marked as food category, 
            // it should have additional documentation
            if (string.IsNullOrWhiteSpace(Notes) && string.IsNullOrWhiteSpace(ReactionDescription))
                return false;

            return true;
        }

        /// <summary>
        /// Gets the appropriate JP-CLINS profile URL for this AllergyIntolerance
        /// </summary>
        /// <returns>JP-CLINS profile URL</returns>
        public string GetJpClinsProfile()
        {
            return Constants.JpClinsConstants.ResourceProfiles.AllergyIntolerance;
        }

        // TODO: JP-CLINS Specific Implementation Notes:
        // 1. Substance coding should prioritize Japanese medication codes (YJ code, HOT code)
        // 2. Manifestation should use Japanese symptom coding systems when available
        // 3. Severity should map to Japanese severity scales if specified in JP-CLINS
        // 4. Extensions may be needed for Japanese-specific allergy classifications
        // 5. Consider workplace exposure allergies for occupational health contexts
    }
}