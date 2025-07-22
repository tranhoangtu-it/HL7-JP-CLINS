using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.Models.InputModels
{
    /// <summary>
    /// Input model for Condition data to be converted to JP_Condition_eCS FHIR resource
    /// This class represents minimal data needed from hospital systems to create FHIR Condition resources
    /// </summary>
    public class ConditionInputModel
    {
        /// <summary>
        /// Patient identifier this condition belongs to
        /// </summary>
        [JsonProperty("patientId")]
        [Required]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>
        /// Condition/diagnosis code
        /// JP-CLINS Profile: Should use ICD-10-CM-JP, ICD-11-MMS-JP, or Japanese disease classification
        /// </summary>
        [JsonProperty("conditionCode")]
        public string? ConditionCode { get; set; }

        /// <summary>
        /// Coding system for the condition code
        /// JP-CLINS: ICD-10-CM-JP, ICD-11-MMS-JP, or Japanese specific codes
        /// </summary>
        [JsonProperty("conditionCodeSystem")]
        public string? ConditionCodeSystem { get; set; }

        /// <summary>
        /// Human-readable name/description of the condition
        /// </summary>
        [JsonProperty("conditionName")]
        [Required]
        [MaxLength(300)]
        public string ConditionName { get; set; } = string.Empty;

        /// <summary>
        /// Clinical status: active, recurrence, relapse, inactive, remission, resolved
        /// JP-CLINS: Must follow FHIR Condition clinical status codes
        /// </summary>
        [JsonProperty("clinicalStatus")]
        [Required]
        public string ClinicalStatus { get; set; } = "active";

        /// <summary>
        /// Verification status: unconfirmed, provisional, differential, confirmed, refuted, entered-in-error
        /// </summary>
        [JsonProperty("verificationStatus")]
        public string VerificationStatus { get; set; } = "confirmed";

        /// <summary>
        /// Category of condition: problem-list-item, encounter-diagnosis
        /// JP-CLINS: Should categorize based on Japanese healthcare context
        /// </summary>
        [JsonProperty("category")]
        public string? Category { get; set; } = "encounter-diagnosis";

        /// <summary>
        /// Severity of condition: mild, moderate, severe
        /// JP-CLINS Profile: Should use Japanese severity coding when available
        /// </summary>
        [JsonProperty("severity")]
        public string? Severity { get; set; }

        /// <summary>
        /// Body site affected by the condition
        /// JP-CLINS: Should use Japanese anatomical terminology when available
        /// </summary>
        [JsonProperty("bodySite")]
        public string? BodySite { get; set; }

        /// <summary>
        /// Date when this condition was first recorded
        /// </summary>
        [JsonProperty("recordedDate")]
        [Required]
        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date/time when condition was first clinically recognized
        /// </summary>
        [JsonProperty("onsetDateTime")]
        public DateTime? OnsetDateTime { get; set; }

        /// <summary>
        /// Date/time when condition was resolved (if applicable)
        /// </summary>
        [JsonProperty("abatementDateTime")]
        public DateTime? AbatementDateTime { get; set; }

        /// <summary>
        /// Who recorded this condition
        /// </summary>
        [JsonProperty("recorderId")]
        public string? RecorderId { get; set; }

        /// <summary>
        /// Who asserted this condition
        /// </summary>
        [JsonProperty("asserterId")]
        public string? AsserterId { get; set; }

        /// <summary>
        /// Encounter when condition was diagnosed
        /// </summary>
        [JsonProperty("encounterId")]
        public string? EncounterId { get; set; }

        /// <summary>
        /// Additional clinical notes about the condition
        /// </summary>
        [JsonProperty("notes")]
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Stage or phase of the condition (for cancer, etc.)
        /// JP-CLINS: Should use Japanese staging systems when applicable
        /// </summary>
        [JsonProperty("stage")]
        public string? Stage { get; set; }

        /// <summary>
        /// Evidence supporting the condition diagnosis
        /// </summary>
        [JsonProperty("evidenceDetail")]
        public string? EvidenceDetail { get; set; }

        /// <summary>
        /// Validates the input model according to JP-CLINS requirements
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(PatientId) || string.IsNullOrWhiteSpace(ConditionName))
                return false;

            var validClinicalStatuses = new[] { "active", "recurrence", "relapse", "inactive", "remission", "resolved" };
            if (!validClinicalStatuses.Contains(ClinicalStatus?.ToLower()))
                return false;

            var validVerificationStatuses = new[] { "unconfirmed", "provisional", "differential", "confirmed", "refuted", "entered-in-error" };
            if (!string.IsNullOrWhiteSpace(VerificationStatus) && !validVerificationStatuses.Contains(VerificationStatus.ToLower()))
                return false;

            // Onset cannot be after abatement
            if (OnsetDateTime.HasValue && AbatementDateTime.HasValue && OnsetDateTime > AbatementDateTime)
                return false;

            // JP-CLINS specific validations
            if (!ValidateJapaneseSpecificRules())
                return false;

            return true;
        }

        /// <summary>
        /// Validates JP-CLINS specific rules for conditions/diagnoses
        /// </summary>
        /// <returns>True if passes JP-CLINS validation</returns>
        private bool ValidateJapaneseSpecificRules()
        {
            // 1. Validate Japanese diagnosis codes (ICD-10-CM-JP, ICD-11-MMS-JP)
            if (!string.IsNullOrWhiteSpace(ConditionCode) && !string.IsNullOrWhiteSpace(ConditionCodeSystem))
            {
                if (!Utilities.FhirHelper.ValidateJapaneseDiagnosisCode(ConditionCode, ConditionCodeSystem))
                    return false;
            }

            // 2. Validate Japanese anatomical body site terminology
            if (!string.IsNullOrWhiteSpace(BodySite))
            {
                if (!ValidateJapaneseBodySite(BodySite))
                    return false;
            }

            // 3. Validate Japanese severity classifications
            if (!string.IsNullOrWhiteSpace(Severity))
            {
                if (!ValidateJapaneseSeverity(Severity))
                    return false;
            }

            // 4. Validate Japanese cancer staging systems
            if (!string.IsNullOrWhiteSpace(Stage))
            {
                if (!ValidateJapaneseCancerStaging(Stage))
                    return false;
            }

            // 5. Validate occupational disease classifications
            if (IsOccupationalDisease())
            {
                if (!ValidateOccupationalDiseaseRequirements())
                    return false;
            }

            // 6. Validate Japanese healthcare terminology for condition descriptions
            if (!ValidateJapaneseTerminology())
                return false;

            return true;
        }

        /// <summary>
        /// Validates Japanese anatomical body site terminology
        /// </summary>
        /// <param name="bodySite">Body site to validate</param>
        /// <returns>True if valid Japanese body site terminology</returns>
        private bool ValidateJapaneseBodySite(string bodySite)
        {
            var validBodySites = new[]
            {
                // Japanese anatomical terms
                "頭部", "頸部", "胸部", "腹部", "骨盤", "上肢", "下肢", "背部", "腰部",
                "心臓", "肺", "肝臓", "腎臓", "胃", "腸", "膀胱", "子宮", "卵巣", "前立腺",
                "脳", "脊髄", "甲状腺", "副腎", "膵臓", "脾臓", "胆嚢", "食道", "十二指腸",
                // English anatomical terms commonly used
                "head", "neck", "chest", "abdomen", "pelvis", "arm", "leg", "back", "spine",
                "heart", "lung", "liver", "kidney", "stomach", "intestine", "bladder", "uterus",
                "brain", "thyroid", "pancreas", "spleen", "gallbladder", "esophagus"
            };

            return validBodySites.Any(valid =>
                bodySite.Contains(valid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Japanese severity classifications
        /// </summary>
        /// <param name="severity">Severity to validate</param>
        /// <returns>True if valid Japanese severity classification</returns>
        private bool ValidateJapaneseSeverity(string severity)
        {
            var validSeverities = new[]
            {
                // English severity terms
                "mild", "moderate", "severe", "critical", "fatal",
                // Japanese severity terms
                "軽度", "中等度", "重度", "重篤", "致命的", "軽症", "中等症", "重症",
                "grade 1", "grade 2", "grade 3", "grade 4", "grade 5",
                "グレード1", "グレード2", "グレード3", "グレード4", "グレード5"
            };

            return validSeverities.Any(valid =>
                severity.Contains(valid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Japanese cancer staging systems
        /// </summary>
        /// <param name="stage">Stage to validate</param>
        /// <returns>True if valid Japanese cancer staging</returns>
        private bool ValidateJapaneseCancerStaging(string stage)
        {
            var validStagePatterns = new[]
            {
                // TNM staging
                @"^T[0-4][a-z]?N[0-3][a-z]?M[0-1][a-z]?$",
                // Stage grouping
                @"^Stage\s+[0IV]+[ABC]?$",
                @"^病期\s*[0IV]+[ABC]?$",
                // Japanese specific patterns
                @"^進行度\s*[I-V]+$",
                @"^臨床病期\s*[I-V]+$"
            };

            return validStagePatterns.Any(pattern =>
                System.Text.RegularExpressions.Regex.IsMatch(stage, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Determines if this condition is an occupational disease
        /// </summary>
        /// <returns>True if occupational disease</returns>
        private bool IsOccupationalDisease()
        {
            var occupationalDiseaseTerms = new[]
            {
                "occupational", "workplace", "職業病", "職業性", "労働災害", "職場", "産業",
                "pneumoconiosis", "じん肺", "asbestosis", "アスベスト", "silicosis", "珪肺",
                "noise-induced", "騒音性", "vibration", "振動", "chemical exposure", "化学物質"
            };

            var searchText = $"{ConditionName} {Notes}".ToLower();
            return occupationalDiseaseTerms.Any(term =>
                searchText.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates occupational disease specific requirements
        /// </summary>
        /// <returns>True if meets occupational disease standards</returns>
        private bool ValidateOccupationalDiseaseRequirements()
        {
            // Occupational diseases require specific documentation in Japan

            // Must have evidence or supporting documentation
            if (string.IsNullOrWhiteSpace(EvidenceDetail) && string.IsNullOrWhiteSpace(Notes))
                return false;

            // Must have asserter (industrial physician or specialist)
            if (string.IsNullOrWhiteSpace(AsserterId))
                return false;

            // Must be confirmed status for legal/compensation purposes
            if (VerificationStatus != "confirmed")
                return false;

            // Should have encounter reference for proper documentation
            if (string.IsNullOrWhiteSpace(EncounterId))
                return false;

            return true;
        }

        /// <summary>
        /// Validates Japanese healthcare terminology usage
        /// </summary>
        /// <returns>True if appropriate Japanese healthcare terminology</returns>
        private bool ValidateJapaneseTerminology()
        {
            // Check for appropriate use of Japanese medical terminology
            var inappropriateTerms = new[]
            {
                // Common translation errors or inappropriate terms
                "病気", // Too general, should be more specific
                "具合が悪い", // Colloquial, not medical
                "調子が悪い" // Colloquial, not medical
            };

            var searchText = ConditionName.ToLower();

            // Avoid overly colloquial terms in medical context
            if (inappropriateTerms.Any(term =>
                searchText.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                // Allow if it's in notes (patient reported) but not in main condition name
                return !string.IsNullOrWhiteSpace(Notes);
            }

            return true;
        }

        /// <summary>
        /// Gets the appropriate JP-CLINS profile URL for this Condition
        /// </summary>
        /// <returns>JP-CLINS profile URL</returns>
        public string GetJpClinsProfile()
        {
            return Constants.JpClinsConstants.ResourceProfiles.ConditioneCS;
        }

        // TODO: JP-CLINS Specific Implementation Notes:
        // 1. Condition codes should prioritize ICD-10-CM-JP or ICD-11-MMS-JP
        // 2. Body site should use Japanese anatomical coding systems
        // 3. Severity should map to Japanese severity classifications
        // 4. Stage should use Japanese cancer staging systems (if applicable)
        // 5. Consider Japanese healthcare terminology for condition descriptions
        // 6. Extensions may be needed for Japanese-specific condition classifications
        // 7. Link to relevant diagnostic procedures and lab results
        // 8. Consider occupational diseases for workplace health contexts
    }
}