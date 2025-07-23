using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Models.InputModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HL7_JP_CLINS_Core.Utilities
{
    /// <summary>
    /// Utility class providing common FHIR operations and helpers
    /// </summary>
    public static class FhirHelper
    {
        /// <summary>
        /// Generates a unique identifier for FHIR resources
        /// </summary>
        /// <param name="prefix">Optional prefix for the ID</param>
        /// <returns>Unique identifier string</returns>
        public static string GenerateUniqueId(string? prefix = null)
        {
            var guid = Guid.NewGuid().ToString("N")[..8]; // Use first 8 characters
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            return string.IsNullOrWhiteSpace(prefix)
                ? $"{timestamp}-{guid}"
                : $"{prefix}-{timestamp}-{guid}";
        }

        /// <summary>
        /// Creates a FHIR date string from DateTime
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>FHIR-compliant date string</returns>
        public static string ToFhirDate(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Creates a FHIR datetime string from DateTime
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>FHIR-compliant datetime string</returns>
        public static string ToFhirDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssK");
        }

        /// <summary>
        /// Creates a ResourceReference with proper URL formatting
        /// </summary>
        /// <param name="resourceType">FHIR resource type</param>
        /// <param name="id">Resource ID</param>
        /// <param name="display">Optional display text</param>
        /// <returns>Properly formatted ResourceReference</returns>
        public static ResourceReference CreateResourceReference(string resourceType, string id, string? display = null)
        {
            var reference = new ResourceReference($"{resourceType}/{id}");
            if (!string.IsNullOrWhiteSpace(display))
            {
                reference.Display = display;
            }
            return reference;
        }



        /// <summary>
        /// Validates basic FHIR resource structure
        /// </summary>
        /// <param name="resource">FHIR resource to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateFhirResource(Resource resource)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            if (resource == null)
            {
                result.IsValid = false;
                result.Errors.Add("Resource cannot be null");
                return result;
            }

            // Basic FHIR validation
            if (string.IsNullOrWhiteSpace(resource.Id))
            {
                result.IsValid = false;
                result.Errors.Add("Resource must have an ID");
            }

            // Comprehensive FHIR validation
            ValidateResourceMetadata(resource, result);
            ValidateResourceReferences(resource, result);
            ValidateResourceSpecificRules(resource, result);
            ValidateJpClinsCompliance(resource, result);

            return result;
        }

        /// <summary>
        /// Converts FHIR resource to JSON string with proper formatting
        /// </summary>
        /// <param name="resource">FHIR resource to serialize</param>
        /// <returns>JSON representation of the resource</returns>
        public static string ToJson(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var serializer = new Hl7.Fhir.Serialization.FhirJsonSerializer();
            return serializer.SerializeToString(resource);
        }

        /// <summary>
        /// Converts JSON string to FHIR resource
        /// </summary>
        /// <typeparam name="T">FHIR resource type</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized FHIR resource</returns>
        public static T? FromJson<T>(string json) where T : Resource
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var parser = new Hl7.Fhir.Serialization.FhirJsonParser();
            return parser.Parse<T>(json);
        }

        /// <summary>
        /// Creates a new Meta element with JP-CLINS profile
        /// </summary>
        /// <param name="profileUrl">JP-CLINS profile URL</param>
        /// <returns>Meta element with the specified profile</returns>
        public static Meta CreateJpClinsMeta(string profileUrl)
        {
            return new Meta
            {
                Profile = new[] { profileUrl },
                Tag = new List<Coding>
                {
                    new Coding("http://jpfhir.jp/fhir/clins/CodeSystem/jp-clins-document-codes", "JP-CLINS", "JP-CLINS Document")
                }
            };
        }

        /// <summary>
        /// Validates Japanese identifier format (for organizations, practitioners, etc.)
        /// </summary>
        /// <param name="identifier">Identifier to validate</param>
        /// <param name="identifierType">Type of identifier (e.g., "medical-license", "facility-code")</param>
        /// <returns>True if identifier format is valid</returns>
        public static bool ValidateJapaneseIdentifier(string identifier, string identifierType)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // TODO: Implement specific Japanese identifier validation rules
            // Different types of identifiers have different format requirements:
            // - Medical license numbers
            // - Healthcare facility codes
            // - Insurance numbers
            // - National ID formats

            return identifierType switch
            {
                "medical-license" => ValidateMedicalLicenseNumber(identifier),
                "facility-code" => ValidateFacilityCode(identifier),
                "insurance-number" => ValidateInsuranceNumber(identifier),
                _ => true // Default to true for unknown types, implement specific validation as needed
            };
        }

        public static bool ValidateMedicalLicenseNumber(string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(licenseNumber)) return false;

            // Remove any non-alphanumeric characters
            var cleanLicense = System.Text.RegularExpressions.Regex.Replace(licenseNumber, @"[^0-9A-Za-z]", "");

            // Japanese medical license number formats per JP-CLINS requirements:

            // Doctor license (医師免許番号): 6 digits 
            // Format: YYXXXX (YY = graduation year code, XXXX = sequential number)
            if (cleanLicense.Length == 6 && cleanLicense.All(char.IsDigit))
            {
                var graduationYearCode = int.Parse(cleanLicense.Substring(0, 2));
                // Valid graduation year codes: 01-99 (representing years)
                return graduationYearCode >= 1 && graduationYearCode <= 99;
            }

            // Nurse license (看護師免許番号): 8 digits 
            // Format: PPXXXXXX (PP = prefecture code 01-47, XXXXXX = license number)
            if (cleanLicense.Length == 8 && cleanLicense.All(char.IsDigit))
            {
                var prefectureCode = int.Parse(cleanLicense.Substring(0, 2));
                var licenseSequence = cleanLicense.Substring(2);

                // Validate prefecture code and ensure license number is not all zeros
                return prefectureCode >= 1 && prefectureCode <= 47 && licenseSequence != "000000";
            }

            // Pharmacist license (薬剤師免許番号): 7 characters
            // Format: PXXXXXX (P = prefix letter, XXXXXX = 6-digit number)
            if (cleanLicense.Length == 7 && char.IsLetter(cleanLicense[0]) && cleanLicense.Substring(1).All(char.IsDigit))
            {
                var prefix = cleanLicense[0];
                var numberPart = cleanLicense.Substring(1);

                // Valid prefixes for pharmacist licenses
                var validPrefixes = new[] { 'P', 'Y', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
                return validPrefixes.Contains(prefix) && numberPart != "000000";
            }

            // Dentist license (歯科医師免許番号): Similar to doctor but with 'D' prefix
            if (cleanLicense.Length == 7 && cleanLicense[0] == 'D' && cleanLicense.Substring(1).All(char.IsDigit))
            {
                var numberPart = cleanLicense.Substring(1);
                return numberPart != "000000";
            }

            // Midwife license (助産師免許番号): 8 digits similar to nurse
            if (cleanLicense.Length == 8 && cleanLicense.All(char.IsDigit))
            {
                // Check if it could be a midwife license (same format as nurse)
                var prefectureCode = int.Parse(cleanLicense.Substring(0, 2));
                return prefectureCode >= 1 && prefectureCode <= 47;
            }

            return false;
        }

        private static bool ValidateFacilityCode(string facilityCode)
        {
            if (string.IsNullOrWhiteSpace(facilityCode)) return false;

            // Japanese healthcare facility codes follow specific patterns:
            // Medical institution code: 10 digits (PP + 8 digits)
            // PP = Prefecture code (01-47)
            // Following 8 digits = Institution number

            var cleanCode = System.Text.RegularExpressions.Regex.Replace(facilityCode, @"[^0-9]", "");

            // Standard medical institution code: 10 digits
            if (cleanCode.Length == 10 && cleanCode.All(char.IsDigit))
            {
                var prefectureCode = int.Parse(cleanCode.Substring(0, 2));
                var institutionNumber = cleanCode.Substring(2);

                // Validate prefecture code (01-47)
                if (prefectureCode < 1 || prefectureCode > 47)
                {
                    return false;
                }

                // Institution number should not be all zeros
                if (institutionNumber == "00000000")
                {
                    return false;
                }

                return true;
            }

            // Pharmacy code: May have different format (7-digit with pharmacy indicator)
            if (cleanCode.Length == 7 && cleanCode.All(char.IsDigit))
            {
                return true;
            }

            return false;
        }

        private static bool ValidateInsuranceNumber(string insuranceNumber)
        {
            if (string.IsNullOrWhiteSpace(insuranceNumber)) return false;

            var cleanNumber = System.Text.RegularExpressions.Regex.Replace(insuranceNumber, @"[^0-9A-Za-z]", "");

            // Japanese health insurance number formats:

            // 1. National Health Insurance (国民健康保険): 8 digits
            if (cleanNumber.Length == 8 && cleanNumber.All(char.IsDigit))
            {
                // First 2 digits should be valid insurer number (not 00)
                var insurerCode = cleanNumber.Substring(0, 2);
                return insurerCode != "00";
            }

            // 2. Employee Health Insurance (健康保険): 8 digits
            // Similar format but different issuer codes
            if (cleanNumber.Length == 8 && cleanNumber.All(char.IsDigit))
            {
                return true;
            }

            // 3. Mutual Aid Insurance (共済組合): May have alphanumeric format
            if (cleanNumber.Length >= 6 && cleanNumber.Length <= 10)
            {
                // Allow alphanumeric for mutual aid insurance
                return cleanNumber.All(char.IsLetterOrDigit);
            }

            // 4. Late-stage Elderly Healthcare (後期高齢者医療): 12 digits
            if (cleanNumber.Length == 12 && cleanNumber.All(char.IsDigit))
            {
                return true;
            }

            // 5. Long-term Care Insurance (介護保険): 10 digits
            if (cleanNumber.Length == 10 && cleanNumber.All(char.IsDigit))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates Japanese postal code format
        /// </summary>
        /// <param name="postalCode">Postal code to validate</param>
        /// <returns>True if valid Japanese postal code format</returns>
        public static bool ValidatePostalCode(string postalCode)
        {
            return ValidateJapanesePostalCode(postalCode);
        }

        /// <summary>
        /// Validates Japanese postal code format
        /// </summary>
        /// <param name="postalCode">Postal code to validate</param>
        /// <returns>True if valid Japanese postal code format</returns>
        public static bool ValidateJapanesePostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode)) return false;

            // Japanese postal code format: 123-4567 or 1234567
            var cleanCode = System.Text.RegularExpressions.Regex.Replace(postalCode, @"[^0-9]", "");

            // Must be exactly 7 digits
            if (cleanCode.Length != 7 || !cleanCode.All(char.IsDigit))
            {
                return false;
            }

            // First digit cannot be 0 (no postal codes start with 0 in Japan)
            if (cleanCode[0] == '0')
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates Japanese prefecture code
        /// </summary>
        /// <param name="prefectureCode">Prefecture code to validate</param>
        /// <returns>True if valid prefecture code (01-47)</returns>
        public static bool ValidateJapanesePrefectureCode(string prefectureCode)
        {
            if (string.IsNullOrWhiteSpace(prefectureCode)) return false;

            if (int.TryParse(prefectureCode, out int code))
            {
                return code >= 1 && code <= 47;
            }

            return false;
        }

        /// <summary>
        /// Validates Japanese phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid Japanese phone number format</returns>
        public static bool ValidatePhoneNumber(string phoneNumber)
        {
            return ValidateJapanesePhoneNumber(phoneNumber);
        }

        /// <summary>
        /// Validates Japanese phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid Japanese phone number format</returns>
        public static bool ValidateJapanesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // Remove all non-digit characters
            var cleanNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^0-9]", "");

            // Japanese phone number patterns:
            // Landline: 10-11 digits (area code + number)
            // Mobile: 11 digits (090/080/070 + 8 digits)
            // Toll-free: 11 digits (0120/0800 + 7 digits)

            if (cleanNumber.Length < 10 || cleanNumber.Length > 11)
            {
                return false;
            }

            // Must start with 0
            if (!cleanNumber.StartsWith("0"))
            {
                return false;
            }

            // Mobile phone patterns
            if (cleanNumber.Length == 11)
            {
                if (cleanNumber.StartsWith("090") ||
                    cleanNumber.StartsWith("080") ||
                    cleanNumber.StartsWith("070"))
                {
                    return true;
                }

                // Toll-free numbers
                if (cleanNumber.StartsWith("0120") ||
                    cleanNumber.StartsWith("0800"))
                {
                    return true;
                }
            }

            // Landline patterns (10-11 digits starting with area codes)
            if (cleanNumber.Length >= 10)
            {
                // Major area codes: 03 (Tokyo), 06 (Osaka), 052 (Nagoya), etc.
                var areaCode = cleanNumber.Substring(0, 2);
                var validAreaCodes = new[] { "03", "04", "05", "06", "07", "08", "09" };

                if (validAreaCodes.Contains(areaCode))
                {
                    return true;
                }
            }

            return false;
        }

        // =====================================================================
        // FHIR Resource Creation Helper Methods for Core 5 Information Resources
        // =====================================================================

        /// <summary>
        /// Creates a CodeableConcept from code and system
        /// </summary>
        /// <param name="code">The code value</param>
        /// <param name="system">The coding system URI</param>
        /// <param name="display">Optional display text</param>
        /// <returns>CodeableConcept instance</returns>
        public static CodeableConcept CreateCodeableConcept(string code, string system, string? display = null)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(system))
                throw new ArgumentException("Code and system are required");

            return new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding(system, code, display)
                }
            };
        }

        /// <summary>
        /// Creates a Reference to another FHIR resource
        /// </summary>
        /// <param name="resourceType">FHIR resource type (e.g., "Patient", "Practitioner")</param>
        /// <param name="resourceId">Resource identifier</param>
        /// <param name="display">Optional display text</param>
        /// <returns>ResourceReference instance</returns>
        public static ResourceReference CreateReference(string resourceType, string resourceId, string? display = null)
        {
            if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("ResourceType and ResourceId are required");

            return new ResourceReference
            {
                Reference = $"{resourceType}/{resourceId}",
                Display = display
            };
        }

        /// <summary>
        /// Creates a Quantity for measurements with value and unit
        /// </summary>
        /// <param name="value">Numeric value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <param name="system">Coding system for unit (defaults to UCUM)</param>
        /// <returns>Quantity instance</returns>
        public static Quantity CreateQuantity(decimal value, string unit, string system = "http://unitsofmeasure.org")
        {
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Unit is required");

            return new Quantity
            {
                Value = value,
                Unit = unit,
                System = system,
                Code = unit // Assuming unit code is same as unit for simplicity
            };
        }

        /// <summary>
        /// Creates a Dosage instruction from input data
        /// </summary>
        /// <param name="route">Route of administration</param>
        /// <param name="doseQuantity">Dose amount</param>
        /// <param name="doseUnit">Dose unit</param>
        /// <param name="frequency">Frequency description</param>
        /// <param name="instructions">Patient instructions</param>
        /// <returns>Dosage instance</returns>
        public static Dosage CreateDosage(string? route = null, decimal? doseQuantity = null,
            string? doseUnit = null, string? frequency = null, string? instructions = null)
        {
            var dosage = new Dosage();

            if (!string.IsNullOrWhiteSpace(route))
            {
                dosage.Route = CreateCodeableConcept(route, "http://terminology.hl7.org/CodeSystem/v3-RouteOfAdministration");
            }

            if (doseQuantity.HasValue && !string.IsNullOrWhiteSpace(doseUnit))
            {
                dosage.DoseAndRate = new List<Dosage.DoseAndRateComponent>
                {
                    new Dosage.DoseAndRateComponent
                    {
                        Dose = CreateQuantity(doseQuantity.Value, doseUnit)
                    }
                };
            }

            if (!string.IsNullOrWhiteSpace(frequency))
            {
                dosage.Text = frequency;
            }

            if (!string.IsNullOrWhiteSpace(instructions))
            {
                dosage.PatientInstruction = instructions;
            }

            return dosage;
        }

        /// <summary>
        /// Creates Japanese-specific coding for common healthcare concepts
        /// </summary>
        /// <param name="conceptType">Type of concept (diagnosis, medication, observation, etc.)</param>
        /// <param name="code">Japanese code</param>
        /// <param name="display">Japanese display text</param>
        /// <returns>Coding instance with appropriate Japanese system</returns>
        public static Coding CreateJapaneseCoding(string conceptType, string code, string? display = null)
        {
            var system = conceptType.ToLower() switch
            {
                "diagnosis-icd10" => "http://jpfhir.jp/fhir/core/CodeSystem/disease-code-icd10-jp",
                "diagnosis-icd11" => "http://jpfhir.jp/fhir/core/CodeSystem/disease-code-icd11-jp",
                "medication-yj" => "urn:oid:1.2.392.100495.20.2.74", // YJ codes
                "medication-hot" => "urn:oid:1.2.392.100495.20.2.73", // HOT codes
                "observation-jlac10" => "urn:oid:1.2.392.200119.4.504", // JLAC10
                "service-medis" => "http://jpfhir.jp/fhir/core/CodeSystem/procedure-code-medis-dc",
                _ => "http://jpfhir.jp/fhir/core/CodeSystem/jp-core-work-classification"
            };

            return new Coding(system, code, display);
        }

        /// <summary>
        /// Validates if a reference follows FHIR reference format
        /// </summary>
        /// <param name="reference">Reference string to validate</param>
        /// <returns>True if valid FHIR reference format</returns>
        public static bool IsValidFhirReference(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return false;

            // FHIR reference format: ResourceType/id or urn:uuid:uuid
            var fhirReferencePattern = @"^([A-Z][a-zA-Z]+\/[A-Za-z0-9\-\.]{1,64}|urn:uuid:[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$";
            return Regex.IsMatch(reference, fhirReferencePattern);
        }

        /// <summary>
        /// Creates a Period representing a time range
        /// </summary>
        /// <param name="start">Start date/time</param>
        /// <param name="end">End date/time</param>
        /// <returns>Period instance</returns>
        public static Period CreatePeriod(DateTime? start = null, DateTime? end = null)
        {
            var period = new Period();

            if (start.HasValue)
            {
                period.StartElement = new FhirDateTime(start.Value);
            }

            if (end.HasValue)
            {
                period.EndElement = new FhirDateTime(end.Value);
            }

            return period;
        }

        /// <summary>
        /// Creates an Identifier for Japanese healthcare contexts
        /// </summary>
        /// <param name="type">Type of identifier (medical-license, organization, etc.)</param>
        /// <param name="value">Identifier value</param>
        /// <param name="system">Identifier system URI</param>
        /// <returns>Identifier instance</returns>
        public static Identifier CreateJapaneseIdentifier(string type, string value, string? system = null)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Type and value are required");

            // Use appropriate Japanese identifier systems
            var identifierSystem = system ?? type.ToLower() switch
            {
                "medical-license" => "urn:oid:1.2.392.100495.20.3.31",
                "nurse-license" => "urn:oid:1.2.392.100495.20.3.32",
                "pharmacist-license" => "urn:oid:1.2.392.100495.20.3.33",
                "organization-id" => "urn:oid:1.2.392.100495.20.3.21",
                "patient-id" => "urn:oid:1.2.392.100495.20.3.51",
                "insurance-number" => "urn:oid:1.2.392.100495.20.3.61",
                _ => "urn:oid:1.2.392.100495.20.3.99"
            };

            return new Identifier
            {
                System = identifierSystem,
                Value = value,
                Type = CreateCodeableConcept(type, "http://terminology.hl7.org/CodeSystem/v2-0203")
            };
        }

        // =====================================================================
        // JP-CLINS Specific Validation Methods
        // =====================================================================

        /// <summary>
        /// Validates Japanese medication codes (YJ codes, HOT codes)
        /// </summary>
        /// <param name="medicationCode">Medication code to validate</param>
        /// <param name="codeSystem">Coding system URI</param>
        /// <returns>True if valid Japanese medication code</returns>
        public static bool ValidateJapaneseMedicationCode(string medicationCode, string codeSystem)
        {
            if (string.IsNullOrWhiteSpace(medicationCode) || string.IsNullOrWhiteSpace(codeSystem))
                return false;

            switch (codeSystem)
            {
                case "urn:oid:1.2.392.100495.20.2.74": // YJ codes (Yakka codes)
                    return ValidateYJCode(medicationCode);

                case "urn:oid:1.2.392.100495.20.2.73": // HOT codes
                    return ValidateHOTCode(medicationCode);

                default:
                    return true; // Allow other coding systems
            }
        }

        /// <summary>
        /// Validates YJ code (Yakka code) format
        /// </summary>
        /// <param name="yjCode">YJ code to validate</param>
        /// <returns>True if valid YJ code format</returns>
        private static bool ValidateYJCode(string yjCode)
        {
            if (string.IsNullOrWhiteSpace(yjCode))
                return false;

            // YJ code format: 4 digits + 2 letters + 2 digits + 1 digit + 1 digit + 1 letter
            // Example: 1234AA567B8C
            var pattern = @"^\d{4}[A-Z]{2}\d{3}[A-Z]\d{1}$";
            return Regex.IsMatch(yjCode, pattern);
        }

        /// <summary>
        /// Validates HOT code format
        /// </summary>
        /// <param name="hotCode">HOT code to validate</param>
        /// <returns>True if valid HOT code format</returns>
        private static bool ValidateHOTCode(string hotCode)
        {
            if (string.IsNullOrWhiteSpace(hotCode))
                return false;

            // HOT code format: 9 digits
            return hotCode.Length == 9 && hotCode.All(char.IsDigit);
        }

        /// <summary>
        /// Validates JLAC10 laboratory code format
        /// </summary>
        /// <param name="jlacCode">JLAC10 code to validate</param>
        /// <returns>True if valid JLAC10 code format</returns>
        public static bool ValidateJLAC10Code(string jlacCode)
        {
            if (string.IsNullOrWhiteSpace(jlacCode))
                return false;

            // JLAC10 format: 17 characters (5+5+2+2+1+1+1)
            // Example: 3A015000001926101
            return jlacCode.Length == 17 && jlacCode.All(c => char.IsDigit(c) || char.IsLetter(c));
        }

        /// <summary>
        /// Validates Japanese diagnosis codes (ICD-10-CM-JP, ICD-11-MMS-JP)
        /// </summary>
        /// <param name="diagnosisCode">Diagnosis code to validate</param>
        /// <param name="codeSystem">Coding system URI</param>
        /// <returns>True if valid Japanese diagnosis code</returns>
        public static bool ValidateJapaneseDiagnosisCode(string diagnosisCode, string codeSystem)
        {
            if (string.IsNullOrWhiteSpace(diagnosisCode) || string.IsNullOrWhiteSpace(codeSystem))
                return false;

            switch (codeSystem)
            {
                case "http://jpfhir.jp/fhir/core/CodeSystem/icd-10-cm-jp":
                    return ValidateICD10CMJPCode(diagnosisCode);

                case "http://jpfhir.jp/fhir/core/CodeSystem/icd-11-mms-jp":
                    return ValidateICD11MMSJPCode(diagnosisCode);

                default:
                    return true; // Allow other coding systems
            }
        }

        /// <summary>
        /// Validates ICD-10-CM-JP code format
        /// </summary>
        /// <param name="icdCode">ICD-10-CM-JP code to validate</param>
        /// <returns>True if valid format</returns>
        private static bool ValidateICD10CMJPCode(string icdCode)
        {
            if (string.IsNullOrWhiteSpace(icdCode))
                return false;

            // ICD-10-CM-JP format: Letter + 2-3 digits + optional decimal + up to 4 more characters
            // Examples: A00, A00.0, Z51.11
            var pattern = @"^[A-Z]\d{2,3}(\.\d{1,4})?$";
            return Regex.IsMatch(icdCode, pattern);
        }

        /// <summary>
        /// Validates ICD-11-MMS-JP code format
        /// </summary>
        /// <param name="icdCode">ICD-11-MMS-JP code to validate</param>
        /// <returns>True if valid format</returns>
        private static bool ValidateICD11MMSJPCode(string icdCode)
        {
            if (string.IsNullOrWhiteSpace(icdCode))
                return false;

            // ICD-11 format: Alphanumeric with specific patterns
            // Examples: 1A00, 8E60.1
            var pattern = @"^[0-9A-Z]{2,4}(\.[0-9A-Z]{1,2})?$";
            return Regex.IsMatch(icdCode, pattern);
        }

        /// <summary>
        /// Validates Japanese vital signs within normal ranges
        /// </summary>
        /// <param name="vitalType">Type of vital sign</param>
        /// <param name="value">Measured value</param>
        /// <param name="unit">Unit of measurement</param>
        /// <returns>True if within acceptable ranges for Japanese population</returns>
        public static bool ValidateJapaneseVitalSigns(string vitalType, decimal value, string unit)
        {
            if (string.IsNullOrWhiteSpace(vitalType))
                return false;

            // Japanese population-specific normal ranges
            return vitalType.ToLower() switch
            {
                "systolic-bp" or "収縮期血圧" => unit == "mmHg" && value >= 90 && value <= 180,
                "diastolic-bp" or "拡張期血圧" => unit == "mmHg" && value >= 60 && value <= 110,
                "heart-rate" or "心拍数" => unit == "bpm" && value >= 50 && value <= 120,
                "body-temperature" or "体温" => unit == "C" && value >= 35.0m && value <= 42.0m,
                "height" or "身長" => unit == "cm" && value >= 100 && value <= 250,
                "weight" or "体重" => unit == "kg" && value >= 20 && value <= 200,
                "bmi" => value >= 15 && value <= 40,
                _ => true // Unknown vital signs pass validation
            };
        }

        /// <summary>
        /// Validates Japanese dosing frequency patterns
        /// </summary>
        /// <param name="frequency">Dosing frequency text</param>
        /// <returns>True if valid Japanese dosing pattern</returns>
        public static bool ValidateJapaneseDosingFrequency(string frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                return false;

            // Common Japanese dosing patterns
            var validPatterns = new[]
            {
                "朝", "昼", "夕", "寝前", "朝食後", "昼食後", "夕食後", "食前", "食後", "食間",
                "1日1回", "1日2回", "1日3回", "1日4回", "毎朝", "毎夕", "隔日",
                "once daily", "twice daily", "three times daily", "four times daily",
                "every morning", "every evening", "every other day", "as needed", "頓服"
            };

            return validPatterns.Any(pattern => frequency.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Japanese pharmaceutical route codes
        /// </summary>
        /// <param name="routeCode">Route code to validate</param>
        /// <returns>True if valid Japanese route code</returns>
        public static bool ValidateJapaneseRouteCode(string routeCode)
        {
            if (string.IsNullOrWhiteSpace(routeCode))
                return false;

            // Common Japanese pharmaceutical routes
            var validRoutes = new[]
            {
                "PO", "SL", "IV", "IM", "SC", "TOP", "INH", "PR", "PV", "OPH", "OTI",
                "経口", "舌下", "静脈内", "筋肉内", "皮下", "外用", "吸入", "直腸", "膣", "点眼", "点耳"
            };

            return validRoutes.Contains(routeCode, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates controlled substance classification for Japanese regulations
        /// </summary>
        /// <param name="medicationCode">Medication code</param>
        /// <param name="medicationName">Medication name</param>
        /// <returns>True if medication is a controlled substance in Japan</returns>
        public static bool IsJapaneseControlledSubstance(string medicationCode, string medicationName)
        {
            if (string.IsNullOrWhiteSpace(medicationCode) && string.IsNullOrWhiteSpace(medicationName))
                return false;

            // Common controlled substances in Japan (partial list for validation)
            var controlledSubstancePatterns = new[]
            {
                "morphine", "モルヒネ", "fentanyl", "フェンタニル", "oxycodone", "オキシコドン",
                "methylphenidate", "メチルフェニデート", "tramadol", "トラマドール",
                "codeine", "コデイン", "diazepam", "ジアゼパム", "alprazolam", "アルプラゾラム"
            };

            var searchText = $"{medicationCode} {medicationName}".ToLower();
            return controlledSubstancePatterns.Any(pattern =>
                searchText.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates resource metadata including profile and version information
        /// </summary>
        private static void ValidateResourceMetadata(Resource resource, ValidationResult result)
        {
            if (resource.Meta != null)
            {
                // Validate profiles
                if (resource.Meta.Profile?.Any() == true)
                {
                    foreach (var profile in resource.Meta.Profile)
                    {
                        if (!IsValidUrl(profile))
                        {
                            result.AddError($"Invalid profile URL: {profile}");
                        }
                    }
                }

                // Validate version ID
                if (!string.IsNullOrWhiteSpace(resource.Meta.VersionId) && !IsValidVersionId(resource.Meta.VersionId))
                {
                    result.AddError($"Invalid version ID format: {resource.Meta.VersionId}");
                }

                // Validate last updated timestamp
                if (resource.Meta.LastUpdated.HasValue && resource.Meta.LastUpdated > DateTimeOffset.UtcNow)
                {
                    result.AddError("LastUpdated timestamp cannot be in the future");
                }
            }
        }

        /// <summary>
        /// Validates resource references integrity
        /// </summary>
        private static void ValidateResourceReferences(Resource resource, ValidationResult result)
        {
            // Get all ResourceReference properties using reflection
            var properties = resource.GetType().GetProperties()
                .Where(p => typeof(ResourceReference).IsAssignableFrom(p.PropertyType) ||
                           (p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericArguments().Any(t => typeof(ResourceReference).IsAssignableFrom(t))));

            foreach (var property in properties)
            {
                var value = property.GetValue(resource);
                if (value is ResourceReference reference)
                {
                    ValidateResourceReference(reference, result);
                }
                else if (value is IEnumerable<ResourceReference> references)
                {
                    foreach (var refItem in references)
                    {
                        ValidateResourceReference(refItem, result);
                    }
                }
            }
        }

        /// <summary>
        /// Validates specific rules for different resource types
        /// </summary>
        private static void ValidateResourceSpecificRules(Resource resource, ValidationResult result)
        {
            switch (resource)
            {
                case Patient patient:
                    ValidatePatientSpecificRules(patient, result);
                    break;
                case Practitioner practitioner:
                    ValidatePractitionerSpecificRules(practitioner, result);
                    break;
                case Organization organization:
                    ValidateOrganizationSpecificRules(organization, result);
                    break;
                case Bundle bundle:
                    ValidateBundleSpecificRules(bundle, result);
                    break;
                case Composition composition:
                    ValidateCompositionSpecificRules(composition, result);
                    break;
            }
        }

        /// <summary>
        /// Validates JP-CLINS specific compliance rules
        /// </summary>
        private static void ValidateJpClinsCompliance(Resource resource, ValidationResult result)
        {
            // Check for JP-CLINS specific profiles
            if (resource.Meta?.Profile?.Any() == true)
            {
                var hasJpClinsProfile = resource.Meta.Profile.Any(p => p.Contains("jpfhir.jp/fhir/clins"));
                if (!hasJpClinsProfile)
                {
                    result.AddWarning("Resource should use JP-CLINS profiles for compliance");
                }
            }

            // Validate Japanese specific content based on resource type
            switch (resource)
            {
                case Patient patient:
                    ValidateJapanesePatientContent(patient, result);
                    break;
                case Practitioner practitioner:
                    ValidateJapanesePractitionerContent(practitioner, result);
                    break;
                case Organization organization:
                    ValidateJapaneseOrganizationContent(organization, result);
                    break;
            }
        }

        // Helper validation methods
        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        private static bool IsValidVersionId(string versionId)
        {
            return !string.IsNullOrWhiteSpace(versionId) && versionId.All(char.IsDigit);
        }

        private static void ValidateResourceReference(ResourceReference reference, ValidationResult result)
        {
            if (reference == null) return;

            if (!string.IsNullOrWhiteSpace(reference.Reference))
            {
                // Validate reference format (ResourceType/id or #id for contained resources)
                if (!reference.Reference.Contains('/') && !reference.Reference.StartsWith('#'))
                {
                    result.AddError($"Invalid reference format: {reference.Reference}");
                }
            }
            else if (string.IsNullOrWhiteSpace(reference.Display))
            {
                result.AddWarning("Resource reference should have either reference or display");
            }
        }

        private static void ValidatePatientSpecificRules(Patient patient, ValidationResult result)
        {
            if (patient.Active == null)
            {
                result.AddWarning("Patient.active should be specified");
            }

            if (patient.Name == null || !patient.Name.Any())
            {
                result.AddError("Patient must have at least one name");
            }

            if (patient.Gender == null)
            {
                result.AddWarning("Patient.gender should be specified");
            }
        }

        private static void ValidatePractitionerSpecificRules(Practitioner practitioner, ValidationResult result)
        {
            if (practitioner.Name == null || !practitioner.Name.Any())
            {
                result.AddError("Practitioner must have at least one name");
            }

            if (practitioner.Identifier == null || !practitioner.Identifier.Any())
            {
                result.AddWarning("Practitioner should have identifiers (e.g., medical license)");
            }
        }

        private static void ValidateOrganizationSpecificRules(Organization organization, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(organization.Name))
            {
                result.AddError("Organization must have a name");
            }

            if (organization.Active == null)
            {
                result.AddWarning("Organization.active should be specified");
            }
        }

        private static void ValidateBundleSpecificRules(Bundle bundle, ValidationResult result)
        {
            if (bundle.Type == null)
            {
                result.AddError("Bundle must have a type");
            }

            if (bundle.Entry == null || !bundle.Entry.Any())
            {
                result.AddWarning("Bundle has no entries");
            }
        }

        private static void ValidateCompositionSpecificRules(Composition composition, ValidationResult result)
        {
            if (composition.Status == null)
            {
                result.AddError("Composition must have a status");
            }

            if (composition.Type == null)
            {
                result.AddError("Composition must have a type");
            }

            if (composition.Subject == null)
            {
                result.AddError("Composition must have a subject");
            }
        }

        private static void ValidateJapanesePatientContent(Patient patient, ValidationResult result)
        {
            // Validate Japanese identifiers
            if (patient.Identifier?.Any() == true)
            {
                foreach (var identifier in patient.Identifier)
                {
                    if (identifier.System?.Contains("jpfhir.jp") == true ||
                        identifier.System?.Contains("healthcare.jp") == true)
                    {
                        ValidateJapaneseIdentifierValue(identifier.Value, identifier.System, result);
                    }
                }
            }

            // Validate Japanese names (Kanji/Kana)
            if (patient.Name?.Any() == true)
            {
                foreach (var name in patient.Name)
                {
                    if (name.Family != null || name.Given?.Any() == true)
                    {
                        ValidateJapaneseName(name, result);
                    }
                }
            }
        }

        private static void ValidateJapanesePractitionerContent(Practitioner practitioner, ValidationResult result)
        {
            // Validate medical license numbers
            if (practitioner.Identifier?.Any() == true)
            {
                var hasValidLicense = false;
                foreach (var identifier in practitioner.Identifier)
                {
                    if (IsJapaneseMedicalLicenseSystem(identifier.System))
                    {
                        hasValidLicense = ValidateMedicalLicenseNumber(identifier.Value) || hasValidLicense;
                    }
                }

                if (!hasValidLicense)
                {
                    result.AddWarning("Practitioner should have valid Japanese medical license identifier");
                }
            }
        }

        private static void ValidateJapaneseOrganizationContent(Organization organization, ValidationResult result)
        {
            // Validate medical institution codes
            if (organization.Identifier?.Any() == true)
            {
                foreach (var identifier in organization.Identifier)
                {
                    if (IsJapaneseMedicalInstitutionSystem(identifier.System))
                    {
                        ValidateJapaneseMedicalInstitutionCode(identifier.Value, result);
                    }
                }
            }
        }

        private static void ValidateJapaneseIdentifierValue(string value, string system, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            // Add specific validation based on Japanese identifier systems
            if (system?.Contains("patient-id") == true)
            {
                // Validate Japanese patient ID format
                if (!IsValidJapanesePatientId(value))
                {
                    result.AddWarning($"Patient ID '{value}' may not follow Japanese healthcare standards");
                }
            }
        }

        private static void ValidateJapaneseName(HumanName name, ValidationResult result)
        {
            // Check for Japanese characters in names
            var fullName = $"{name.Family} {string.Join(" ", name.Given ?? new List<string>())}";

            if (ContainsJapaneseCharacters(fullName))
            {
                // For Japanese names, check if both Kanji and Kana representations exist
                var hasKanjiReading = name.Extension?.Any(e =>
                    e.Url == "http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation" &&
                    e.Value?.ToString() == "IDE") == true;

                var hasKanaReading = name.Extension?.Any(e =>
                    e.Url == "http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation" &&
                    e.Value?.ToString() == "SYL") == true;

                if (!hasKanaReading)
                {
                    result.AddWarning("Japanese names should include Kana (furigana) reading");
                }
            }
        }

        private static bool IsJapaneseMedicalLicenseSystem(string system)
        {
            if (string.IsNullOrWhiteSpace(system)) return false;

            return system.Contains("jpfhir.jp") ||
                   system.Contains("medical-license") ||
                   system.Contains("healthcare.jp");
        }

        private static bool IsJapaneseMedicalInstitutionSystem(string system)
        {
            if (string.IsNullOrWhiteSpace(system)) return false;

            return system.Contains("jpfhir.jp") ||
                   system.Contains("medical-institution") ||
                   system.Contains("healthcare.jp");
        }

        private static void ValidateJapaneseMedicalInstitutionCode(string code, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            // Japanese medical institution codes are typically 10 digits
            if (code.Length == 10 && code.All(char.IsDigit))
            {
                // Valid format
                return;
            }

            result.AddWarning($"Medical institution code '{code}' may not follow Japanese format (10 digits)");
        }

        private static bool IsValidJapanesePatientId(string patientId)
        {
            if (string.IsNullOrWhiteSpace(patientId)) return false;

            // Japanese patient IDs are typically alphanumeric with specific patterns
            return patientId.Length >= 6 && patientId.Length <= 20 &&
                   patientId.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        private static bool ContainsJapaneseCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            return text.Any(c =>
                (c >= 0x3040 && c <= 0x309F) || // Hiragana
                (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                (c >= 0x4E00 && c <= 0x9FAF));  // Kanji
        }
    }

    /// <summary>
    /// Validation result class for FHIR operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}