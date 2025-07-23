using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.FhirModels;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Utilities;
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
                Id = FhirHelper.GenerateUniqueId("Patient")
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
                patient.Identifier.Add(new Identifier
                {
                    System = new Uri("urn:oid:1.2.392.100495.20.3.51.1"),
                    Value = TransformHelper.SafeGetString(patientData, "patientId"),
                    Type = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = new Uri("http://terminology.hl7.org/CodeSystem/v2-0203"),
                                Code = "PI",
                                Display = "Patient Identifier"
                            }
                        }
                    }
                });
            }

            // Insurance number (健康保険証番号)
            if (patientData.insuranceNumber != null)
            {
                patient.Identifier.Add(new Identifier
                {
                    System = new Uri("urn:oid:1.2.392.100495.20.3.51.2"),
                    Value = TransformHelper.SafeGetString(patientData, "insuranceNumber"),
                    Type = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = new Uri("http://terminology.hl7.org/CodeSystem/v2-0203"),
                                Code = "SS",
                                Display = "Social Security Number"
                            }
                        }
                    }
                });
            }

            // Medical record number
            if (patientData.medicalRecordNumber != null)
            {
                patient.Identifier.Add(new Identifier
                {
                    System = new Uri("urn:oid:1.2.392.100495.20.3.51.1"), // Hospital specific
                    Value = TransformHelper.SafeGetString(patientData, "medicalRecordNumber"),
                    Type = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new Coding
                            {
                                System = new Uri("http://terminology.hl7.org/CodeSystem/v2-0203"),
                                Code = "MR",
                                Display = "Medical Record Number"
                            }
                        }
                    }
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
                    Use = "official",
                    Family = TransformHelper.SafeGetString(patientData, "familyName"),
                    Given = new List<string> { TransformHelper.SafeGetString(patientData, "givenName") }
                };
                patient.Name.Add(officialName);
            }

            // Kana name (カナ)
            if (patientData.familyNameKana != null || patientData.givenNameKana != null)
            {
                var kanaName = new HumanName
                {
                    Use = "usual",
                    Family = TransformHelper.SafeGetString(patientData, "familyNameKana"),
                    Given = new List<string> { TransformHelper.SafeGetString(patientData, "givenNameKana") }
                };
                patient.Name.Add(kanaName);
            }
        }

        /// <summary>
        /// Maps patient demographics including Japanese date formats
        /// </summary>
        private static void MapDemographics(Patient patient, dynamic patientData)
        {
            // Gender mapping
            if (patientData.gender != null)
            {
                var gender = TransformHelper.SafeGetString(patientData, "gender").ToLower();
                patient.Gender = gender switch
                {
                    "male" or "m" or "男" => "male",
                    "female" or "f" or "女" => "female",
                    "other" or "o" or "その他" => "other",
                    "unknown" or "u" or "不明" => "unknown",
                    _ => "unknown"
                };
            }

            // Birth date (handle Japanese era dates)
            if (patientData.birthDate != null)
            {
                var birthDateStr = TransformHelper.SafeGetString(patientData, "birthDate");
                var gregorianDate = TransformHelper.ConvertJapaneseEraToGregorian(birthDateStr);
                if (gregorianDate.HasValue)
                {
                    patient.BirthDate = gregorianDate.Value.ToString("yyyy-MM-dd");
                }
                else
                {
                    // Try direct parsing if not Japanese era
                    if (DateTime.TryParse(birthDateStr, out DateTime parsedDate))
                    {
                        patient.BirthDate = parsedDate.ToString("yyyy-MM-dd");
                    }
                }
            }
        }

        /// <summary>
        /// Maps patient addresses in Japanese format
        /// </summary>
        private static void MapAddresses(Patient patient, dynamic patientData)
        {
            // TODO: Implement address mapping when Address model is created
            // For now, this is a placeholder for Japanese address format
            // Japanese addresses typically include: 〒123-4567 東京都渋谷区...
        }

        /// <summary>
        /// Maps patient contact information
        /// </summary>
        private static void MapContactInfo(Patient patient, dynamic patientData)
        {
            // TODO: Implement contact mapping when ContactPoint model is created
            // Japanese phone numbers: 03-1234-5678, 090-1234-5678
            // Email addresses: standard format
        }

        /// <summary>
        /// Maps Japanese health insurance information
        /// </summary>
        private static void MapInsuranceInfo(Patient patient, dynamic patientData)
        {
            // TODO: Implement insurance mapping when Coverage model is created
            // Japanese health insurance includes:
            // - 健康保険 (Health Insurance)
            // - 国民健康保険 (National Health Insurance)
            // - 後期高齢者医療制度 (Late-stage Elderly Medical Care System)
        }
    }
}