using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.Utilities;
using Newtonsoft.Json.Linq;

namespace HL7_JP_CLINS_Tranforms.Mappers
{
    /// <summary>
    /// Mapper for transforming hospital patient data to JP-CLINS Patient resource
    /// Handles Japanese patient data including Kanji/Kana names, Japanese addresses, and insurance information
    /// </summary>
    public static class PatientMapper
    {
        /// <summary>
        /// Maps hospital patient data to FHIR Patient resource compliant with JP-CLINS
        /// </summary>
        /// <param name="patientData">Hospital patient data (JSON or dynamic object)</param>
        /// <returns>FHIR Patient resource</returns>
        public static Patient MapToPatient(dynamic patientData)
        {
            var patient = new Patient
            {
                Id = FhirHelper.GenerateUniqueId("Patient"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Patient },
                    LastUpdated = DateTimeOffset.UtcNow
                }
            };

            // Map patient identifiers
            MapPatientIdentifiers(patient, patientData);

            // Map patient names (Japanese Kanji/Kana)
            MapPatientNames(patient, patientData);

            // Map demographics
            MapDemographics(patient, patientData);

            // Map addresses (Japanese format)
            MapAddresses(patient, patientData);

            // Map contact information
            MapContactInfo(patient, patientData);

            // Map insurance information (Japanese health insurance)
            MapInsuranceInfo(patient, patientData);

            return patient;
        }

        /// <summary>
        /// Maps patient identifiers including Japanese medical ID formats
        /// </summary>
        private static void MapPatientIdentifiers(Patient patient, dynamic patientData)
        {
            patient.Identifier = new List<Identifier>();

            // Primary patient ID
            if (patientData.patientId != null)
            {
                patient.Identifier.Add(FhirHelper.CreateJapaneseIdentifier(
                    "patient-id",
                    patientData.patientId.ToString()));
            }

            // Insurance number (健康保険証番号)
            if (patientData.insuranceNumber != null)
            {
                patient.Identifier.Add(FhirHelper.CreateJapaneseIdentifier(
                    "insurance-number",
                    patientData.insuranceNumber.ToString()));
            }

            // Medical record number
            if (patientData.medicalRecordNumber != null)
            {
                patient.Identifier.Add(new Identifier
                {
                    System = "urn:oid:1.2.392.100495.20.3.51.1", // Hospital specific
                    Value = patientData.medicalRecordNumber.ToString(),
                    Type = FhirHelper.CreateCodeableConcept(code: "MR", system: "http://terminology.hl7.org/CodeSystem/v2-0203", display: "Medical Record Number")
                });
            }
        }

        /// <summary>
        /// Maps patient names supporting Japanese Kanji and Kana
        /// </summary>
        private static void MapPatientNames(Patient patient, dynamic patientData)
        {
            patient.Name = new List<HumanName>();

            // Official name (通常は漢字)
            if (patientData.familyName != null || patientData.givenName != null)
            {
                var officialName = new HumanName
                {
                    Use = HumanName.NameUse.Official,
                    Family = patientData.familyName?.ToString(),
                    Given = patientData.givenName != null ? new[] { patientData.givenName.ToString() } : null
                };
                patient.Name.Add(officialName);
            }

            // Phonetic name (フリガナ - Kana reading)
            if (patientData.familyNameKana != null || patientData.givenNameKana != null)
            {
                var phoneticName = new HumanName
                {
                    Extension = new List<Extension>
                    {
                        new Extension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation",
                                    new Code("PHN")) // Phonetic representation
                    },
                    Family = patientData.familyNameKana?.ToString(),
                    Given = patientData.givenNameKana != null ? new[] { patientData.givenNameKana.ToString() } : null
                };
                patient.Name.Add(phoneticName);
            }
        }

        /// <summary>
        /// Maps basic demographics (gender, birth date, etc.)
        /// </summary>
        private static void MapDemographics(Patient patient, dynamic patientData)
        {
            // Birth date
            if (patientData.birthDate != null)
            {
                if (DateTime.TryParse(patientData.birthDate.ToString(), out DateTime birthDate))
                {
                    patient.BirthDateElement = new Date(birthDate.Year, birthDate.Month, birthDate.Day);
                }
            }

            // Gender
            if (patientData.gender != null)
            {
                var genderValue = patientData.gender.ToString().ToLower();
                patient.Gender = genderValue switch
                {
                    "male" or "男性" or "男" => AdministrativeGender.Male,
                    "female" or "女性" or "女" => AdministrativeGender.Female,
                    "other" or "その他" => AdministrativeGender.Other,
                    _ => AdministrativeGender.Unknown
                };
            }

            // Marital status
            if (patientData.maritalStatus != null)
            {
                patient.MaritalStatus = FhirHelper.CreateCodeableConcept(
                    code: patientData.maritalStatus.ToString(),
                    system: "http://terminology.hl7.org/CodeSystem/v3-MaritalStatus");
            }
        }

        /// <summary>
        /// Maps addresses using Japanese address format
        /// </summary>
        private static void MapAddresses(Patient patient, dynamic patientData)
        {
            patient.Address = new List<Address>();

            if (patientData.address != null)
            {
                var address = new Address
                {
                    Use = Address.AddressUse.Home,
                    Type = Address.AddressType.Physical
                };

                // Japanese postal code (郵便番号)
                if (patientData.address.postalCode != null)
                {
                    var postalCode = patientData.address.postalCode.ToString();
                    if (FhirHelper.ValidatePostalCode(postalCode))
                    {
                        address.PostalCode = postalCode;
                    }
                }

                // Prefecture (都道府県)
                if (patientData.address.prefecture != null)
                {
                    address.State = patientData.address.prefecture.ToString();
                }

                // City (市区町村)
                if (patientData.address.city != null)
                {
                    address.City = patientData.address.city.ToString();
                }

                // Address lines (住所詳細)
                var addressLines = new List<string>();
                if (patientData.address.line1 != null)
                    addressLines.Add(patientData.address.line1.ToString());
                if (patientData.address.line2 != null)
                    addressLines.Add(patientData.address.line2.ToString());

                if (addressLines.Any())
                {
                    address.Line = addressLines;
                }

                // Country (国)
                address.Country = "JP"; // Japan

                patient.Address.Add(address);
            }
        }

        /// <summary>
        /// Maps contact information (phone, email)
        /// </summary>
        private static void MapContactInfo(Patient patient, dynamic patientData)
        {
            patient.Telecom = new List<ContactPoint>();

            // Phone number
            if (patientData.phoneNumber != null)
            {
                var phoneNumber = patientData.phoneNumber.ToString();
                if (FhirHelper.ValidatePhoneNumber(phoneNumber))
                {
                    patient.Telecom.Add(new ContactPoint
                    {
                        System = ContactPoint.ContactPointSystem.Phone,
                        Value = phoneNumber,
                        Use = ContactPoint.ContactPointUse.Home
                    });
                }
            }

            // Mobile phone
            if (patientData.mobileNumber != null)
            {
                var mobileNumber = patientData.mobileNumber.ToString();
                if (FhirHelper.ValidatePhoneNumber(mobileNumber))
                {
                    patient.Telecom.Add(new ContactPoint
                    {
                        System = ContactPoint.ContactPointSystem.Phone,
                        Value = mobileNumber,
                        Use = ContactPoint.ContactPointUse.Mobile
                    });
                }
            }

            // Email
            if (patientData.email != null)
            {
                patient.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = patientData.email.ToString(),
                    Use = ContactPoint.ContactPointUse.Home
                });
            }
        }

        /// <summary>
        /// Maps Japanese health insurance information
        /// </summary>
        private static void MapInsuranceInfo(Patient patient, dynamic patientData)
        {
            if (patientData.insurance != null)
            {
                // Add insurance extension for Japanese health insurance system
                var insuranceExtension = new Extension
                {
                    Url = "http://jpfhir.jp/fhir/core/Extension/JP_Patient_Race",
                    Value = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding("http://jpfhir.jp/fhir/core/CodeSystem/JP_Insurance",
                                     patientData.insurance.type?.ToString() ?? "NHI",
                                     "National Health Insurance")
                        }
                    }
                };

                if (patient.Extension == null)
                    patient.Extension = new List<Extension>();

                patient.Extension.Add(insuranceExtension);
            }
        }

        // TODO: JP-CLINS Patient Mapping Notes:
        // 1. Support for Japanese character encoding (UTF-8)
        // 2. Implement proper Kanji/Kana name handling with extensions
        // 3. Use Japanese postal code validation (7-digit format)
        // 4. Support multiple insurance types (NHI, Employee Health Insurance, etc.)
        // 5. Include race/ethnicity extensions for Japanese population
        // 6. Handle Japanese phone number formats (mobile vs landline)
        // 7. Support for family relationship mapping (戸籍関係)
        // 8. Include Japanese-specific patient flags (e.g., elderly care status)
        // 9. Map emergency contact information with Japanese relationships
        // 10. Support for patient consent preferences per Japanese privacy laws
    }
}