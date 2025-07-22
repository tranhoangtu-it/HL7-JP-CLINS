using HL7_JP_CLINS_Core.Models.Base;
using HL7_JP_CLINS_Core.Models.Documents;
using HL7_JP_CLINS_Core.Utilities;
using Hl7.Fhir.Model;

namespace HL7_JP_CLINS_Core.Validators
{
    /// <summary>
    /// Specialized validator for JP-CLINS documents and FHIR resources
    /// Implements validation rules specific to Japanese healthcare standards
    /// </summary>
    public static class ClinsValidator
    {
        /// <summary>
        /// Validates any JP-CLINS document against common rules
        /// </summary>
        /// <param name="document">Document to validate</param>
        /// <returns>Validation result</returns>
        public static HL7_JP_CLINS_Core.Utilities.ValidationResult ValidateDocument(IClinsDocument document)
        {
            var result = new HL7_JP_CLINS_Core.Utilities.ValidationResult();

            if (document == null)
            {
                result.AddError("Document cannot be null");
                return result;
            }

            // Common document validation
            ValidateCommonDocumentRules(document, result);

            // Type-specific validation
            switch (document)
            {
                case EReferralDocument eReferral:
                    ValidateEReferral(eReferral, result);
                    break;
                case EDischargeSummaryDocument eDischargeSummary:
                    ValidateEDischargeSummary(eDischargeSummary, result);
                    break;
                case ECheckupDocument eCheckup:
                    ValidateECheckup(eCheckup, result);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Validates common rules applicable to all JP-CLINS documents
        /// </summary>
        private static void ValidateCommonDocumentRules(IClinsDocument document, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // ID validation
            if (string.IsNullOrWhiteSpace(document.Id))
            {
                result.AddError("Document ID is required");
            }

            // Status validation
            var validStatuses = new[] { "draft", "final", "amended", "cancelled" };
            if (!validStatuses.Contains(document.Status?.ToLower()))
            {
                result.AddError($"Invalid document status: {document.Status}. Must be one of: {string.Join(", ", validStatuses)}");
            }

            // Patient reference validation
            if (document.PatientReference == null || string.IsNullOrWhiteSpace(document.PatientReference.Reference))
            {
                result.AddError("Patient reference is required");
            }

            // Author reference validation
            if (document.AuthorReference == null || string.IsNullOrWhiteSpace(document.AuthorReference.Reference))
            {
                result.AddError("Author reference is required");
            }

            // Organization reference validation
            if (document.OrganizationReference == null || string.IsNullOrWhiteSpace(document.OrganizationReference.Reference))
            {
                result.AddError("Organization reference is required");
            }

            // Date validation
            if (document.CreatedDate > DateTime.UtcNow)
            {
                result.AddError("Document creation date cannot be in the future");
            }

            if (document.LastModifiedDate.HasValue && document.LastModifiedDate > DateTime.UtcNow)
            {
                result.AddError("Document last modified date cannot be in the future");
            }

            if (document.LastModifiedDate.HasValue && document.LastModifiedDate < document.CreatedDate)
            {
                result.AddError("Document last modified date cannot be before creation date");
            }
        }

        /// <summary>
        /// Validates eReferral specific rules
        /// </summary>
        private static void ValidateEReferral(EReferralDocument eReferral, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Referral reason validation
            if (string.IsNullOrWhiteSpace(eReferral.ReferralReason))
            {
                result.AddError("Referral reason is required");
            }
            else if (eReferral.ReferralReason.Length > 1000)
            {
                result.AddError("Referral reason must not exceed 1000 characters");
            }

            // Urgency validation
            var validUrgencies = new[] { "routine", "urgent", "asap", "stat" };
            if (!string.IsNullOrWhiteSpace(eReferral.Urgency) && !validUrgencies.Contains(eReferral.Urgency.ToLower()))
            {
                result.AddError($"Invalid urgency level: {eReferral.Urgency}. Must be one of: {string.Join(", ", validUrgencies)}");
            }

            // Referred to organization validation
            if (eReferral.ReferredToOrganization == null || string.IsNullOrWhiteSpace(eReferral.ReferredToOrganization.Reference))
            {
                result.AddError("Referred to organization is required");
            }

            // Service requested validation
            if (eReferral.ServiceRequested == null || !eReferral.ServiceRequested.Any())
            {
                result.AddError("At least one service must be requested");
            }

            // Clinical notes length validation
            if (!string.IsNullOrWhiteSpace(eReferral.ClinicalNotes) && eReferral.ClinicalNotes.Length > 5000)
            {
                result.AddError("Clinical notes must not exceed 5000 characters");
            }

            // JP-CLINS specific eReferral validation
            ValidateJapaneseServiceCodes(eReferral.ServiceRequested, result);
            ValidateJapaneseOrganizationIdentifiers(eReferral.ReferredToOrganization, result);
            ValidateReferralTypeRequirements(eReferral, result);
        }

        /// <summary>
        /// Validates eDischargeSummary specific rules
        /// </summary>
        private static void ValidateEDischargeSummary(EDischargeSummaryDocument eDischargeSummary, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Date validation
            if (eDischargeSummary.AdmissionDate >= eDischargeSummary.DischargeDate)
            {
                result.AddError("Admission date must be before discharge date");
            }

            if (eDischargeSummary.DischargeDate > DateTime.UtcNow)
            {
                result.AddError("Discharge date cannot be in the future");
            }

            // Admission reason validation
            if (string.IsNullOrWhiteSpace(eDischargeSummary.AdmissionReason))
            {
                result.AddError("Admission reason is required");
            }
            else if (eDischargeSummary.AdmissionReason.Length > 1000)
            {
                result.AddError("Admission reason must not exceed 1000 characters");
            }

            // Principal diagnosis validation
            if (eDischargeSummary.PrincipalDiagnosis == null ||
                (eDischargeSummary.PrincipalDiagnosis.Coding?.Count == 0 && string.IsNullOrWhiteSpace(eDischargeSummary.PrincipalDiagnosis.Text)))
            {
                result.AddError("Principal diagnosis is required");
            }

            // Discharge condition validation
            var validDischargeConditions = new[] { "improved", "stable", "worsened", "deceased", "transferred" };
            if (string.IsNullOrWhiteSpace(eDischargeSummary.DischargeCondition) ||
                !validDischargeConditions.Contains(eDischargeSummary.DischargeCondition.ToLower()))
            {
                result.AddError($"Invalid discharge condition: {eDischargeSummary.DischargeCondition}. Must be one of: {string.Join(", ", validDischargeConditions)}");
            }

            // Length validation for text fields
            if (!string.IsNullOrWhiteSpace(eDischargeSummary.HospitalCourse) && eDischargeSummary.HospitalCourse.Length > 5000)
            {
                result.AddError("Hospital course must not exceed 5000 characters");
            }

            if (!string.IsNullOrWhiteSpace(eDischargeSummary.DischargeInstructions) && eDischargeSummary.DischargeInstructions.Length > 3000)
            {
                result.AddError("Discharge instructions must not exceed 3000 characters");
            }

            // JP-CLINS specific eDischargeSummary validation
            ValidateJapaneseDiagnosisCodes(eDischargeSummary.PrincipalDiagnosis, result);
            ValidateJapaneseDiagnosisCodes(eDischargeSummary.SecondaryDiagnoses, result);
            ValidateJapaneseMedicationCodes(eDischargeSummary.DischargeMedications, result);
            ValidateJapaneseDischargeStandards(eDischargeSummary, result);
        }

        /// <summary>
        /// Validates eCheckup specific rules
        /// </summary>
        private static void ValidateECheckup(ECheckupDocument eCheckup, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Checkup date validation
            if (eCheckup.CheckupDate > DateTime.UtcNow.Date)
            {
                result.AddError("Checkup date cannot be in the future");
            }

            // Checkup type validation
            if (eCheckup.CheckupType == null ||
                (eCheckup.CheckupType.Coding?.Count == 0 && string.IsNullOrWhiteSpace(eCheckup.CheckupType.Text)))
            {
                result.AddError("Checkup type is required");
            }

            // Checkup purpose validation
            if (string.IsNullOrWhiteSpace(eCheckup.CheckupPurpose))
            {
                result.AddError("Checkup purpose is required");
            }
            else if (eCheckup.CheckupPurpose.Length > 500)
            {
                result.AddError("Checkup purpose must not exceed 500 characters");
            }

            // Follow-up validation
            if (eCheckup.FollowUpRequired && !eCheckup.FollowUpDate.HasValue)
            {
                result.AddError("Follow-up date is required when follow-up is indicated");
            }

            if (eCheckup.FollowUpDate.HasValue && eCheckup.FollowUpDate <= eCheckup.CheckupDate)
            {
                result.AddError("Follow-up date must be after checkup date");
            }

            // Overall assessment validation
            if (eCheckup.OverallAssessment == null ||
                (eCheckup.OverallAssessment.Coding?.Count == 0 && string.IsNullOrWhiteSpace(eCheckup.OverallAssessment.Text)))
            {
                result.AddError("Overall assessment is required");
            }

            // Recommendations length validation
            if (!string.IsNullOrWhiteSpace(eCheckup.Recommendations) && eCheckup.Recommendations.Length > 3000)
            {
                result.AddError("Recommendations must not exceed 3000 characters");
            }

            // Certification status validation
            if (!string.IsNullOrWhiteSpace(eCheckup.CertificationStatus))
            {
                var validCertificationStatuses = new[] { "fit for work", "restricted", "unfit", "requires evaluation" };
                if (!validCertificationStatuses.Contains(eCheckup.CertificationStatus.ToLower()))
                {
                    result.AddError($"Invalid certification status: {eCheckup.CertificationStatus}. Must be one of: {string.Join(", ", validCertificationStatuses)}");
                }
            }

            // JP-CLINS specific eCheckup validation
            ValidateJapaneseHealthCheckupStandards(eCheckup, result);
            ValidateJapaneseHealthAssessmentCodes(eCheckup.OverallAssessment, result);
            ValidateOccupationalHealthRegulations(eCheckup, result);
            ValidateCheckupExaminationRequirements(eCheckup, result);
        }

        /// <summary>
        /// Validates FHIR Bundle structure for JP-CLINS documents
        /// </summary>
        /// <param name="bundle">FHIR Bundle to validate</param>
        /// <returns>Validation result</returns>
        public static HL7_JP_CLINS_Core.Utilities.ValidationResult ValidateFhirBundle(Bundle bundle)
        {
            var result = new HL7_JP_CLINS_Core.Utilities.ValidationResult();

            if (bundle == null)
            {
                result.AddError("Bundle cannot be null");
                return result;
            }

            // Bundle type validation
            if (bundle.Type != Bundle.BundleType.Document)
            {
                result.AddError("Bundle type must be 'document' for JP-CLINS documents");
            }

            // First entry must be Composition
            if (bundle.Entry == null || !bundle.Entry.Any())
            {
                result.AddError("Bundle must contain at least one entry");
            }
            else
            {
                var firstEntry = bundle.Entry.First();
                if (firstEntry.Resource is not Composition)
                {
                    result.AddError("First entry in document bundle must be a Composition resource");
                }
            }

            // JP-CLINS specific Bundle validation
            ValidateBundleResourceReferences(bundle, result);
            ValidateJpClinsProfiles(bundle, result);
            ValidateRequiredResourcesForDocumentType(bundle, result);

            return result;
        }

        // JP-CLINS specific validation methods

        /// <summary>
        /// Validates Japanese service codes for referrals
        /// </summary>
        private static void ValidateJapaneseServiceCodes(List<CodeableConcept> serviceRequested, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            var validJapaneseServiceSystems = new[]
            {
                Constants.JpClinsConstants.CodingSystems.JapanProcedure,
                "http://jpfhir.jp/fhir/core/CodeSystem/JLAC10",
                "http://jpfhir.jp/fhir/core/CodeSystem/MEDIS-DC",
                Constants.JpClinsConstants.CodingSystems.LOINC,
                Constants.JpClinsConstants.CodingSystems.SNOMED
            };

            foreach (var service in serviceRequested)
            {
                if (service.Coding?.Any() == true)
                {
                    bool hasValidJapaneseCoding = service.Coding.Any(coding =>
                        validJapaneseServiceSystems.Contains(coding.System));

                    if (!hasValidJapaneseCoding)
                    {
                        result.Errors.Add($"Service '{service.Text}' should include Japanese standard coding");
                    }
                }
            }
        }

        /// <summary>
        /// Validates Japanese organization identifiers
        /// </summary>
        private static void ValidateJapaneseOrganizationIdentifiers(ResourceReference organizationRef, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (organizationRef?.Reference != null)
            {
                var orgId = organizationRef.Reference.Split('/').LastOrDefault();
                if (!FhirHelper.ValidateJapaneseIdentifier(orgId, "facility-code"))
                {
                    result.Errors.Add("Organization identifier should follow Japanese medical institution code format");
                }
            }
        }

        /// <summary>
        /// Validates referral type specific requirements
        /// </summary>
        private static void ValidateReferralTypeRequirements(EReferralDocument eReferral, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (eReferral.Urgency?.ToLower() == "stat")
            {
                if (string.IsNullOrWhiteSpace(eReferral.ClinicalNotes))
                {
                    result.Errors.Add("STAT urgency referrals must include detailed clinical notes");
                }
            }

            if (eReferral.CurrentConditions.Any() && string.IsNullOrWhiteSpace(eReferral.RelevantHistory))
            {
                result.Errors.Add("Referrals with current conditions should include relevant medical history");
            }
        }

        /// <summary>
        /// Validates Japanese diagnosis codes
        /// </summary>
        private static void ValidateJapaneseDiagnosisCodes(CodeableConcept diagnosis, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (diagnosis?.Coding?.Any() == true)
            {
                var validJapaneseDiagnosisSystems = new[]
                {
                    Constants.JpClinsConstants.CodingSystems.ICD10CMJP,
                    Constants.JpClinsConstants.CodingSystems.ICD10JP,
                    Constants.JpClinsConstants.CodingSystems.ICD10,
                    Constants.JpClinsConstants.CodingSystems.SNOMED
                };

                bool hasValidCoding = diagnosis.Coding.Any(coding =>
                    validJapaneseDiagnosisSystems.Contains(coding.System));

                if (!hasValidCoding)
                {
                    result.Errors.Add($"Diagnosis '{diagnosis.Text}' should include Japanese standard diagnosis coding");
                }
            }
        }

        /// <summary>
        /// Validates Japanese diagnosis codes for multiple diagnoses
        /// </summary>
        private static void ValidateJapaneseDiagnosisCodes(List<CodeableConcept> diagnoses, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            foreach (var diagnosis in diagnoses)
            {
                ValidateJapaneseDiagnosisCodes(diagnosis, result);
            }
        }

        /// <summary>
        /// Validates Japanese medication codes
        /// </summary>
        private static void ValidateJapaneseMedicationCodes(List<ResourceReference> medications, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            var validJapaneseMedicationSystems = new[]
            {
                Constants.JpClinsConstants.CodingSystems.YakuzaiCode,
                Constants.JpClinsConstants.CodingSystems.HOTCode
            };

            foreach (var medication in medications)
            {
                if (!string.IsNullOrWhiteSpace(medication.Reference))
                {
                    if (!medication.Reference.StartsWith("Medication/") &&
                        !medication.Reference.StartsWith("MedicationRequest/"))
                    {
                        result.IsValid = false;
                        result.Errors.Add("Medication references must point to Medication or MedicationRequest resources");
                    }
                }
            }
        }

        /// <summary>
        /// Validates Japanese discharge standards
        /// </summary>
        private static void ValidateJapaneseDischargeStandards(EDischargeSummaryDocument eDischargeSummary, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Validate length of stay reasonableness
            if (eDischargeSummary.LengthOfStay > 365)
            {
                result.Errors.Add("Warning: Unusually long hospital stay (>1 year) - please verify");
            }

            // Check for mandatory Japanese discharge information
            if (string.IsNullOrWhiteSpace(eDischargeSummary.DischargeCondition))
            {
                result.IsValid = false;
                result.Errors.Add("Discharge condition is required for Japanese discharge summaries");
            }

            // Validate attending physician
            if (eDischargeSummary.AttendingPhysician?.Reference == null)
            {
                result.IsValid = false;
                result.Errors.Add("Attending physician reference is required for discharge summary");
            }
        }

        /// <summary>
        /// Validates Japanese health checkup standards
        /// </summary>
        private static void ValidateJapaneseHealthCheckupStandards(ECheckupDocument eCheckup, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            // Validate checkup date reasonableness
            if (eCheckup.CheckupDate > DateTime.UtcNow.Date)
            {
                result.IsValid = false;
                result.Errors.Add("Checkup date cannot be in the future");
            }

            // Check for required Japanese checkup elements
            if (eCheckup.OverallAssessment == null)
            {
                result.IsValid = false;
                result.Errors.Add("Overall assessment is required for Japanese health checkups");
            }

            // Validate examining physician
            if (eCheckup.ExaminingPhysician?.Reference == null)
            {
                result.IsValid = false;
                result.Errors.Add("Examining physician reference is required for health checkups");
            }
        }

        /// <summary>
        /// Validates Japanese health assessment codes
        /// </summary>
        private static void ValidateJapaneseHealthAssessmentCodes(CodeableConcept assessment, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (assessment?.Coding?.Any() == true)
            {
                var validJapaneseAssessmentSystems = new[]
                {
                    "http://jpfhir.jp/fhir/core/CodeSystem/health-assessment-jp",
                    Constants.JpClinsConstants.CodingSystems.LOINC,
                    Constants.JpClinsConstants.CodingSystems.SNOMED
                };

                bool hasValidCoding = assessment.Coding.Any(coding =>
                    validJapaneseAssessmentSystems.Contains(coding.System));

                if (!hasValidCoding)
                {
                    result.Errors.Add($"Health assessment '{assessment.Text}' should include Japanese standard coding");
                }
            }
        }

        /// <summary>
        /// Validates occupational health regulations compliance
        /// </summary>
        private static void ValidateOccupationalHealthRegulations(ECheckupDocument eCheckup, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            var checkupTypeText = eCheckup.CheckupType?.Text?.ToLower() ?? "";

            // Check for occupational health specific requirements
            if (checkupTypeText.Contains("occupational") || checkupTypeText.Contains("職業"))
            {
                if (string.IsNullOrWhiteSpace(eCheckup.CertificationStatus))
                {
                    result.IsValid = false;
                    result.Errors.Add("Occupational health checkups must include work capability certification");
                }

                if (!eCheckup.PhysicalExamFindings.Any())
                {
                    result.Errors.Add("Occupational health checkups should include physical examination findings");
                }
            }
        }

        /// <summary>
        /// Validates checkup examination requirements
        /// </summary>
        private static void ValidateCheckupExaminationRequirements(ECheckupDocument eCheckup, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            var checkupTypeText = eCheckup.CheckupType?.Text?.ToLower() ?? "";

            // Annual checkup requirements
            if (checkupTypeText.Contains("annual") || checkupTypeText.Contains("定期"))
            {
                if (!eCheckup.VitalSigns.Any())
                {
                    result.Errors.Add("Annual checkups must include vital signs measurements");
                }

                if (!eCheckup.LaboratoryResults.Any())
                {
                    result.Errors.Add("Annual checkups should include laboratory test results");
                }
            }

            // Executive checkup requirements
            if (checkupTypeText.Contains("executive") || checkupTypeText.Contains("人間ドック"))
            {
                if (!eCheckup.ImagingResults.Any())
                {
                    result.Errors.Add("Executive checkups should include imaging studies");
                }
            }
        }

        /// <summary>
        /// Validates Bundle resource references integrity
        /// </summary>
        private static void ValidateBundleResourceReferences(Bundle bundle, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (bundle.Entry == null) return;

            var resourceIds = new HashSet<string>();
            var references = new List<string>();

            // Collect all resource IDs and references
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource?.Id != null)
                {
                    resourceIds.Add($"{entry.Resource.TypeName}/{entry.Resource.Id}");
                }

                // Collect references from Composition
                if (entry.Resource is Composition composition)
                {
                    if (composition.Subject != null)
                    {
                        references.Add(composition.Subject.Reference);
                    }
                    if (composition.Author?.Any() == true)
                    {
                        references.AddRange(composition.Author.Select(a => a.Reference).Where(r => !string.IsNullOrWhiteSpace(r)));
                    }
                }
            }

            // Check for broken references
            foreach (var reference in references)
            {
                if (!reference.StartsWith("http") && !resourceIds.Contains(reference))
                {
                    result.Errors.Add($"Bundle contains broken reference: {reference}");
                }
            }
        }

        /// <summary>
        /// Validates JP-CLINS profiles usage
        /// </summary>
        private static void ValidateJpClinsProfiles(Bundle bundle, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (bundle.Meta?.Profile?.Any() != true)
            {
                result.Errors.Add("Bundle should declare JP-CLINS profile");
                return;
            }

            var jpClinsProfiles = new[]
            {
                Constants.JpClinsConstants.DocumentProfiles.EReferral,
                Constants.JpClinsConstants.DocumentProfiles.EDischargeSummary,
                Constants.JpClinsConstants.DocumentProfiles.ECheckup
            };

            bool hasJpClinsProfile = bundle.Meta.Profile.Any(profile =>
                jpClinsProfiles.Contains(profile));

            if (!hasJpClinsProfile)
            {
                result.Errors.Add("Bundle should use JP-CLINS document profile");
            }
        }

        /// <summary>
        /// Validates required resources for document type
        /// </summary>
        private static void ValidateRequiredResourcesForDocumentType(Bundle bundle, HL7_JP_CLINS_Core.Utilities.ValidationResult result)
        {
            if (bundle.Entry?.Any() != true) return;

            var composition = bundle.Entry.FirstOrDefault()?.Resource as Composition;
            if (composition == null) return;

            var hasPatient = bundle.Entry.Any(e => e.Resource is Patient);
            var hasPractitioner = bundle.Entry.Any(e => e.Resource is Practitioner || e.Resource is PractitionerRole);
            var hasOrganization = bundle.Entry.Any(e => e.Resource is Organization);

            if (!hasPatient)
            {
                result.Errors.Add("JP-CLINS documents should include Patient resource");
            }

            if (!hasPractitioner)
            {
                result.Errors.Add("JP-CLINS documents should include Practitioner or PractitionerRole resource");
            }

            if (!hasOrganization)
            {
                result.Errors.Add("JP-CLINS documents should include Organization resource");
            }
        }
    }
}