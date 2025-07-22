namespace HL7_JP_CLINS_Core.Constants
{
    /// <summary>
    /// Constants for JP-CLINS implementation guide
    /// Reference: https://jpfhir.jp/fhir/clins/igv1/index.html
    /// </summary>
    public static class JpClinsConstants
    {
        /// <summary>
        /// JP-CLINS Implementation Guide version
        /// </summary>
        public const string ImplementationGuideVersion = "1.11.0";

        /// <summary>
        /// Base URL for JP-CLINS profiles
        /// </summary>
        public const string BaseProfileUrl = "http://jpfhir.jp/fhir/clins/StructureDefinition/";

        /// <summary>
        /// Document type profile URLs
        /// </summary>
        public static class DocumentProfiles
        {
            public const string EReferral = BaseProfileUrl + "JP_Bundle_eReferral";
            public const string EDischargeSummary = BaseProfileUrl + "JP_Bundle_eDischargeSummary";
            public const string ECheckup = BaseProfileUrl + "JP_Bundle_eCheckup";
            public const string EPrescription = BaseProfileUrl + "JP_Bundle_ePrescription";
            public const string EInstructionSummary = BaseProfileUrl + "JP_Bundle_eInstructionSummary";
        }

        /// <summary>
        /// Resource profile URLs
        /// </summary>
        public static class ResourceProfiles
        {
            public const string Patient = BaseProfileUrl + "JP_Patient_CLINS";
            public const string Practitioner = BaseProfileUrl + "JP_Practitioner_CLINS";
            public const string Organization = BaseProfileUrl + "JP_Organization_CLINS";
            public const string Encounter = BaseProfileUrl + "JP_Encounter_CLINS";
            public const string Condition = BaseProfileUrl + "JP_Condition_CLINS";
            public const string Procedure = BaseProfileUrl + "JP_Procedure_CLINS";
            public const string MedicationRequest = BaseProfileUrl + "JP_MedicationRequest_CLINS";
            public const string Observation = BaseProfileUrl + "JP_Observation_CLINS";

            // Core 5 information resources commonly used in JP-CLINS documents
            public const string AllergyIntolerance = BaseProfileUrl + "JP_AllergyIntolerance_eCS";
            public const string ConditioneCS = BaseProfileUrl + "JP_Condition_eCS";
            public const string ObservationLabResult = BaseProfileUrl + "JP_Observation-LabResult-eCS";
            public const string MedicationRequesteCS = BaseProfileUrl + "JP_MedicationRequest-eCS";
        }

        /// <summary>
        /// Japanese coding systems
        /// </summary>
        public static class CodingSystems
        {
            // Diagnosis codes
            public const string ICD10JP = "http://jpfhir.jp/fhir/core/CodeSystem/icd-10";
            public const string ICD10CMJP = "http://jpfhir.jp/fhir/core/CodeSystem/icd-10-cm-jp";

            // Procedure codes
            public const string JapanProcedure = "http://jpfhir.jp/fhir/core/CodeSystem/japan-procedure";

            // Medication codes
            public const string YakuzaiCode = "http://jpfhir.jp/fhir/core/CodeSystem/yakuzai-code";
            public const string HOTCode = "http://jpfhir.jp/fhir/core/CodeSystem/hot-code";

            // Organization identifiers
            public const string MedicalInstitutionCode = "http://jpfhir.jp/fhir/core/CodeSystem/medical-institution-code";
            public const string InsuranceCode = "http://jpfhir.jp/fhir/core/CodeSystem/insurance-code";

            // Practitioner identifiers
            public const string MedicalLicenseCode = "http://jpfhir.jp/fhir/core/CodeSystem/medical-license";
            public const string NurseLicenseCode = "http://jpfhir.jp/fhir/core/CodeSystem/nurse-license";

            // Administrative codes
            public const string PrefectureCode = "http://jpfhir.jp/fhir/core/CodeSystem/prefecture";
            public const string PostalCode = "http://jpfhir.jp/fhir/core/CodeSystem/postal-code";

            // Document section codes
            public const string ClinsDocumentSections = "http://jpfhir.jp/fhir/clins/CodeSystem/document-section";

            // Standard international systems also used
            public const string LOINC = "http://loinc.org";
            public const string SNOMED = "http://snomed.info/sct";
            public const string ICD10 = "http://hl7.org/fhir/sid/icd-10";
        }

        /// <summary>
        /// Document status values
        /// </summary>
        public static class DocumentStatus
        {
            public const string Draft = "draft";
            public const string Final = "final";
            public const string Amended = "amended";
            public const string Cancelled = "cancelled";
            public const string Replaced = "replaced";
        }

        /// <summary>
        /// Document section codes for JP-CLINS
        /// </summary>
        public static class SectionCodes
        {
            // Common sections
            public const string PatientInformation = "patient-info";
            public const string MedicalHistory = "medical-history";
            public const string CurrentMedications = "current-medications";
            public const string Allergies = "allergies";
            public const string VitalSigns = "vital-signs";

            // eReferral specific
            public const string ReferralReason = "referral-reason";
            public const string ReferralDetails = "referral-details";
            public const string ClinicalNotes = "clinical-notes";

            // eDischargeSummary specific
            public const string AdmissionInfo = "admission-info";
            public const string HospitalCourse = "hospital-course";
            public const string DischargeInfo = "discharge-info";
            public const string DischargeInstructions = "discharge-instructions";
            public const string FollowUpCare = "follow-up-care";

            // eCheckup specific
            public const string CheckupOverview = "checkup-overview";
            public const string PhysicalExam = "physical-exam";
            public const string LaboratoryResults = "lab-results";
            public const string ImagingResults = "imaging-results";
            public const string HealthAssessment = "health-assessment";
            public const string Recommendations = "recommendations";
        }

        /// <summary>
        /// Common LOINC codes used in JP-CLINS
        /// </summary>
        public static class LoincCodes
        {
            // Document types
            public const string ReferralNote = "57133-1";
            public const string DischargeSummary = "18842-5";
            public const string HealthAssessment = "68604-8";

            // Common sections
            public const string ChiefComplaint = "10154-3";
            public const string HistoryOfPresentIllness = "10164-2";
            public const string PastMedicalHistory = "11348-0";
            public const string FamilyHistory = "10157-6";
            public const string SocialHistory = "29762-2";
            public const string PhysicalExamination = "29545-1";
            public const string AssessmentAndPlan = "51848-0";
            public const string Medications = "10160-0";
            public const string AllergiesAndAdverseReactions = "48765-2";
        }

        /// <summary>
        /// Urgency levels for referrals
        /// </summary>
        public static class UrgencyLevels
        {
            public const string Routine = "routine";
            public const string Urgent = "urgent";
            public const string ASAP = "asap";
            public const string STAT = "stat";
        }

        /// <summary>
        /// Discharge condition values
        /// </summary>
        public static class DischargeConditions
        {
            public const string Improved = "improved";
            public const string Stable = "stable";
            public const string Worsened = "worsened";
            public const string Deceased = "deceased";
            public const string Transferred = "transferred";
        }

        /// <summary>
        /// Checkup certification status values
        /// </summary>
        public static class CertificationStatus
        {
            public const string FitForWork = "fit for work";
            public const string Restricted = "restricted";
            public const string Unfit = "unfit";
            public const string RequiresEvaluation = "requires evaluation";
        }

        /// <summary>
        /// Common identifier system URIs for Japan
        /// </summary>
        public static class IdentifierSystems
        {
            public const string MyNumberCard = "http://jpfhir.jp/fhir/core/NamingSystem/mynumber";
            public const string InsuranceCardNumber = "http://jpfhir.jp/fhir/core/NamingSystem/insurance-card";
            public const string PatientNumber = "http://jpfhir.jp/fhir/core/NamingSystem/patient-number";
            public const string MedicalLicenseNumber = "http://jpfhir.jp/fhir/core/NamingSystem/medical-license";
            public const string FacilityNumber = "http://jpfhir.jp/fhir/core/NamingSystem/facility-number";
        }
    }
}