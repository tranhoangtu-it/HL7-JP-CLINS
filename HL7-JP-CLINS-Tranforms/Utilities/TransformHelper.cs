using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HL7_JP_CLINS_Tranforms.Utilities
{
    /// <summary>
    /// Utility helper class providing common transformation methods and JP-CLINS specific utilities
    /// Contains reusable methods for data validation, format conversion, and FHIR resource creation
    /// </summary>
    public static class TransformHelper
    {
        /// <summary>
        /// Converts dynamic object to strongly typed object of specified type
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="dynamicObject">Dynamic object to convert</param>
        /// <returns>Strongly typed object</returns>
        public static T ConvertDynamicObject<T>(dynamic dynamicObject) where T : class
        {
            try
            {
                if (dynamicObject == null)
                    return null;

                // If it's already the correct type, return as-is
                if (dynamicObject is T directType)
                    return directType;

                // Convert via JSON serialization/deserialization
                var json = JsonConvert.SerializeObject(dynamicObject);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert dynamic object to {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely extracts string value from dynamic object property
        /// </summary>
        /// <param name="dynamicObject">Dynamic object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if property is null or empty</param>
        /// <returns>String value or default</returns>
        public static string SafeGetString(dynamic dynamicObject, string propertyName, string defaultValue = "")
        {
            try
            {
                if (dynamicObject == null)
                    return defaultValue;

                // Handle JObject
                if (dynamicObject is JObject jObj)
                {
                    var property = jObj[propertyName];
                    return property?.ToString() ?? defaultValue;
                }

                // Handle dynamic object with property access
                var propertyInfo = dynamicObject.GetType().GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(dynamicObject);
                    return value?.ToString() ?? defaultValue;
                }

                // Try reflection for fields
                var fieldInfo = dynamicObject.GetType().GetField(propertyName);
                if (fieldInfo != null)
                {
                    var value = fieldInfo.GetValue(dynamicObject);
                    return value?.ToString() ?? defaultValue;
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely extracts DateTime value from dynamic object property
        /// </summary>
        /// <param name="dynamicObject">Dynamic object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if property is null or invalid</param>
        /// <returns>DateTime value or default</returns>
        public static DateTime? SafeGetDateTime(dynamic dynamicObject, string propertyName, DateTime? defaultValue = null)
        {
            try
            {
                var stringValue = SafeGetString(dynamicObject, propertyName);
                if (string.IsNullOrWhiteSpace(stringValue))
                    return defaultValue;

                if (DateTime.TryParse(stringValue, out DateTime result))
                    return result;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely extracts integer value from dynamic object property
        /// </summary>
        /// <param name="dynamicObject">Dynamic object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if property is null or invalid</param>
        /// <returns>Integer value or default</returns>
        public static int? SafeGetInt(dynamic dynamicObject, string propertyName, int? defaultValue = null)
        {
            try
            {
                var stringValue = SafeGetString(dynamicObject, propertyName);
                if (string.IsNullOrWhiteSpace(stringValue))
                    return defaultValue;

                if (int.TryParse(stringValue, out int result))
                    return result;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely extracts decimal value from dynamic object property
        /// </summary>
        /// <param name="dynamicObject">Dynamic object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if property is null or invalid</param>
        /// <returns>Decimal value or default</returns>
        public static decimal? SafeGetDecimal(dynamic dynamicObject, string propertyName, decimal? defaultValue = null)
        {
            try
            {
                var stringValue = SafeGetString(dynamicObject, propertyName);
                if (string.IsNullOrWhiteSpace(stringValue))
                    return defaultValue;

                if (decimal.TryParse(stringValue, out decimal result))
                    return result;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Safely extracts boolean value from dynamic object property
        /// </summary>
        /// <param name="dynamicObject">Dynamic object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <param name="defaultValue">Default value if property is null or invalid</param>
        /// <returns>Boolean value or default</returns>
        public static bool? SafeGetBool(dynamic dynamicObject, string propertyName, bool? defaultValue = null)
        {
            try
            {
                var stringValue = SafeGetString(dynamicObject, propertyName);
                if (string.IsNullOrWhiteSpace(stringValue))
                    return defaultValue;

                if (bool.TryParse(stringValue, out bool result))
                    return result;

                // Handle common string representations
                var lowerValue = stringValue.ToLower();
                return lowerValue switch
                {
                    "1" or "yes" or "true" or "はい" or "有" => true,
                    "0" or "no" or "false" or "いいえ" or "無" => false,
                    _ => defaultValue
                };
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Validates and formats Japanese postal code
        /// </summary>
        /// <param name="postalCode">Postal code to validate</param>
        /// <returns>Formatted postal code or null if invalid</returns>
        public static string ValidateAndFormatJapanesePostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return null;

            // Remove common separators
            var cleanCode = postalCode.Replace("-", "").Replace("〒", "").Trim();

            // Japanese postal codes are 7 digits
            if (cleanCode.Length == 7 && cleanCode.All(char.IsDigit))
            {
                return $"{cleanCode.Substring(0, 3)}-{cleanCode.Substring(3, 4)}";
            }

            return null;
        }

        /// <summary>
        /// Validates and formats Japanese phone number
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>Formatted phone number or null if invalid</returns>
        public static string ValidateAndFormatJapanesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Remove common separators and spaces
            var cleanNumber = phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

            // Handle international format
            if (cleanNumber.StartsWith("+81"))
            {
                cleanNumber = "0" + cleanNumber.Substring(3);
            }

            // Japanese phone numbers typically start with 0 and have 10-11 digits
            if (cleanNumber.StartsWith("0") && cleanNumber.Length >= 10 && cleanNumber.Length <= 11 && cleanNumber.All(char.IsDigit))
            {
                // Format as XXX-XXXX-XXXX or XXXX-XX-XXXX depending on length
                if (cleanNumber.Length == 10)
                {
                    return $"{cleanNumber.Substring(0, 3)}-{cleanNumber.Substring(3, 3)}-{cleanNumber.Substring(6, 4)}";
                }
                else if (cleanNumber.Length == 11)
                {
                    return $"{cleanNumber.Substring(0, 3)}-{cleanNumber.Substring(3, 4)}-{cleanNumber.Substring(7, 4)}";
                }
            }

            return null;
        }

        /// <summary>
        /// Creates Japanese-specific extensions for FHIR resources
        /// </summary>
        /// <param name="extensionType">Type of Japanese extension</param>
        /// <param name="value">Extension value</param>
        /// <returns>FHIR Extension</returns>
        public static Extension CreateJapaneseExtension(string extensionType, object value)
        {
            var extensionUrl = extensionType.ToLower() switch
            {
                "kana" => "http://jpfhir.jp/fhir/core/Extension/JP_Patient_KanaName",
                "race" => "http://jpfhir.jp/fhir/core/Extension/JP_Patient_Race",
                "insurance" => "http://jpfhir.jp/fhir/core/Extension/JP_Coverage_InsuranceSymbol",
                "facility" => "http://jpfhir.jp/fhir/core/Extension/JP_Organization_FacilityCode",
                "specialty" => "http://jpfhir.jp/fhir/core/Extension/JP_Practitioner_Specialty",
                _ => $"http://jpfhir.jp/fhir/core/Extension/JP_{extensionType}"
            };

            var extension = new Extension { Url = extensionUrl };

            switch (value)
            {
                case string stringValue:
                    extension.Value = new FhirString(stringValue);
                    break;
                case int intValue:
                    extension.Value = new Integer(intValue);
                    break;
                case decimal decimalValue:
                    extension.Value = new FhirDecimal(decimalValue);
                    break;
                case bool boolValue:
                    extension.Value = new FhirBoolean(boolValue);
                    break;
                case DateTime dateValue:
                    extension.Value = new FhirDateTime(dateValue);
                    break;
                case CodeableConcept codeableConceptValue:
                    extension.Value = codeableConceptValue;
                    break;
                default:
                    extension.Value = new FhirString(value?.ToString() ?? "");
                    break;
            }

            return extension;
        }

        /// <summary>
        /// Validates Japanese medical codes (YJ, HOT, etc.)
        /// </summary>
        /// <param name="code">Medical code to validate</param>
        /// <param name="codeType">Type of medical code</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateJapaneseMedicalCode(string code, string codeType)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return codeType.ToLower() switch
            {
                "yj" => FhirHelper.ValidateJapaneseMedicationCode(code, "urn:oid:1.2.392.100495.20.2.74"),
                "hot" => FhirHelper.ValidateJapaneseMedicationCode(code, "urn:oid:1.2.392.100495.20.2.73"),
                "jlac10" => FhirHelper.ValidateJLAC10Code(code),
                "icd10jp" => FhirHelper.ValidateJapaneseDiagnosisCode(code, JpClinsConstants.CodingSystems.ICD10JP),
                _ => !string.IsNullOrWhiteSpace(code)
            };
        }

        /// <summary>
        /// Converts Japanese era date to Gregorian date
        /// </summary>
        /// <param name="eraDate">Japanese era date (e.g., "令和3年4月1日")</param>
        /// <returns>Gregorian DateTime or null if conversion fails</returns>
        public static DateTime? ConvertJapaneseEraToGregorian(string eraDate)
        {
            if (string.IsNullOrWhiteSpace(eraDate))
                return null;

            try
            {
                // This is a simplified implementation
                // A full implementation would need comprehensive era conversion logic

                // For now, return null and let calling code handle standard date parsing
                // TODO: Implement full Japanese era date conversion
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates standardized Japanese text narrative for FHIR resources
        /// </summary>
        /// <param name="content">Text content</param>
        /// <param name="status">Narrative status</param>
        /// <returns>FHIR Narrative</returns>
        public static Narrative CreateJapaneseNarrative(string content, Narrative.NarrativeStatus status = Narrative.NarrativeStatus.Generated)
        {
            if (string.IsNullOrWhiteSpace(content))
                content = "詳細情報が記載されています";

            return new Narrative
            {
                Status = status,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"ja\">{System.Net.WebUtility.HtmlEncode(content)}</div>"
            };
        }

        /// <summary>
        /// Validates JP-CLINS document completeness
        /// </summary>
        /// <param name="bundle">FHIR Bundle to validate</param>
        /// <returns>List of validation issues</returns>
        public static List<string> ValidateJpClinsDocumentCompleteness(Bundle bundle)
        {
            var issues = new List<string>();

            if (bundle == null)
            {
                issues.Add("Bundle cannot be null");
                return issues;
            }

            // Check Bundle type
            if (bundle.Type != Bundle.BundleType.Document)
            {
                issues.Add("Bundle must be of type 'document' for JP-CLINS");
            }

            // Check first entry is Composition
            if (bundle.Entry?.FirstOrDefault()?.Resource is not Composition)
            {
                issues.Add("First entry must be a Composition resource");
            }

            // Check required profiles
            if (bundle.Meta?.Profile?.Any() != true)
            {
                issues.Add("Bundle must declare JP-CLINS profile");
            }

            // Check for required resources
            var resourceTypes = bundle.Entry?.Select(e => e.Resource?.TypeName).Where(t => t != null).ToHashSet();

            if (!resourceTypes.Contains("Patient"))
            {
                issues.Add("Bundle must contain a Patient resource");
            }

            if (!resourceTypes.Contains("Practitioner"))
            {
                issues.Add("Bundle must contain at least one Practitioner resource");
            }

            // Check Japanese-specific requirements
            ValidateJapaneseResourceRequirements(bundle, issues);

            return issues;
        }

        /// <summary>
        /// Validates Japanese-specific resource requirements
        /// </summary>
        /// <param name="bundle">FHIR Bundle</param>
        /// <param name="issues">List to add issues to</param>
        private static void ValidateJapaneseResourceRequirements(Bundle bundle, List<string> issues)
        {
            foreach (var entry in bundle.Entry ?? new List<Bundle.EntryComponent>())
            {
                var resource = entry.Resource;
                if (resource == null) continue;

                switch (resource)
                {
                    case Patient patient:
                        ValidateJapanesePatientRequirements(patient, issues);
                        break;
                    case Practitioner practitioner:
                        ValidateJapanesePractitionerRequirements(practitioner, issues);
                        break;
                    case Observation observation:
                        ValidateJapaneseObservationRequirements(observation, issues);
                        break;
                    case MedicationRequest medicationRequest:
                        ValidateJapaneseMedicationRequirements(medicationRequest, issues);
                        break;
                }
            }
        }

        /// <summary>
        /// Validates Japanese Patient resource requirements
        /// </summary>
        /// <param name="patient">Patient resource</param>
        /// <param name="issues">List to add issues to</param>
        private static void ValidateJapanesePatientRequirements(Patient patient, List<string> issues)
        {
            // Check for Japanese name formats
            if (patient.Name?.Any() != true)
            {
                issues.Add("Patient must have at least one name");
            }
            else
            {
                var hasOfficialName = patient.Name.Any(n => n.Use == HumanName.NameUse.Official);
                if (!hasOfficialName)
                {
                    issues.Add("Patient should have an official name");
                }
            }

            // Check for Japanese identifiers
            if (patient.Identifier?.Any() != true)
            {
                issues.Add("Patient should have at least one identifier");
            }
        }

        /// <summary>
        /// Validates Japanese Practitioner resource requirements
        /// </summary>
        /// <param name="practitioner">Practitioner resource</param>
        /// <param name="issues">List to add issues to</param>
        private static void ValidateJapanesePractitionerRequirements(Practitioner practitioner, List<string> issues)
        {
            // Check for medical license identifiers
            var hasMedicalLicense = practitioner.Identifier?.Any(i =>
                i.System?.Contains("medical-license") == true ||
                i.Type?.Coding?.Any(c => c.Code == "MD") == true) == true;

            if (!hasMedicalLicense)
            {
                issues.Add("Practitioner should have medical license identifier");
            }
        }

        /// <summary>
        /// Validates Japanese Observation resource requirements
        /// </summary>
        /// <param name="observation">Observation resource</param>
        /// <param name="issues">List to add issues to</param>
        private static void ValidateJapaneseObservationRequirements(Observation observation, List<string> issues)
        {
            // Check for Japanese coding systems
            if (observation.Code?.Coding?.Any() != true)
            {
                issues.Add("Observation should have coded observation type");
            }
            else
            {
                var hasJapaneseCoding = observation.Code.Coding.Any(c =>
    c.System == "urn:oid:1.2.392.200119.4.504" || // JLAC10
    c.System == JpClinsConstants.CodingSystems.LOINC);

                if (!hasJapaneseCoding)
                {
                    issues.Add("Observation should use Japanese coding systems (JLAC10 or LOINC)");
                }
            }
        }

        /// <summary>
        /// Validates Japanese MedicationRequest resource requirements
        /// </summary>
        /// <param name="medicationRequest">MedicationRequest resource</param>
        /// <param name="issues">List to add issues to</param>
        private static void ValidateJapaneseMedicationRequirements(MedicationRequest medicationRequest, List<string> issues)
        {
            // Check for Japanese medication coding
            if (medicationRequest.Medication is CodeableConcept medicationCodeable)
            {
                var hasJapaneseCoding = medicationCodeable.Coding?.Any(c =>
    c.System == JpClinsConstants.CodingSystems.YakuzaiCode ||
    c.System == JpClinsConstants.CodingSystems.HOTCode) == true;

                if (!hasJapaneseCoding)
                {
                    issues.Add("MedicationRequest should use Japanese medication coding (YJ or HOT codes)");
                }
            }
        }

        /// <summary>
        /// Generates JP-CLINS compliant resource ID
        /// </summary>
        /// <param name="resourceType">FHIR resource type</param>
        /// <param name="hospitalId">Hospital identifier</param>
        /// <param name="sequence">Optional sequence number</param>
        /// <returns>JP-CLINS compliant resource ID</returns>
        public static string GenerateJpClinsResourceId(string resourceType, string hospitalId = null, int? sequence = null)
        {
            var baseId = FhirHelper.GenerateUniqueId(resourceType);

            if (!string.IsNullOrWhiteSpace(hospitalId))
            {
                baseId = $"{hospitalId}-{baseId}";
            }

            if (sequence.HasValue)
            {
                baseId = $"{baseId}-{sequence:D4}";
            }

            return baseId;
        }

        // TODO: JP-CLINS Transform Helper Implementation Notes:
        // 1. Add comprehensive Japanese era date conversion (昭和, 平成, 令和)
        // 2. Implement Japanese address validation and formatting
        // 3. Add support for Japanese medical terminology translation
        // 4. Include Japanese insurance validation helpers
        // 5. Add Japanese phone number and postal code validation
        // 6. Implement Japanese character encoding validation (UTF-8)
        // 7. Add Japanese medical code validation (comprehensive)
        // 8. Include Japanese healthcare workflow helpers
        // 9. Add Japanese privacy compliance utilities
        // 10. Implement Japanese reporting format helpers
    }
}