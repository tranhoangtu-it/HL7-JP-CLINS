using Hl7.Fhir.Model;
using HL7_JP_CLINS_Core.Constants;
using HL7_JP_CLINS_Core.Utilities;
using HL7_JP_CLINS_Tranforms.Utilities;

namespace HL7_JP_CLINS_Tranforms.Mappers
{
    /// <summary>
    /// Mapper for transforming hospital practitioner data to JP-CLINS Practitioner resource
    /// Handles Japanese medical license numbers, specialties, and qualifications
    /// </summary>
    public static class PractitionerMapper
    {
        /// <summary>
        /// Maps hospital practitioner data to FHIR Practitioner resource compliant with JP-CLINS
        /// </summary>
        /// <param name="practitionerData">Hospital practitioner data</param>
        /// <returns>FHIR Practitioner resource</returns>
        public static Practitioner MapToPractitioner(dynamic practitionerData)
        {
            var practitioner = new Practitioner
            {
                Id = FhirHelper.GenerateUniqueId("Practitioner"),
                Meta = new Meta
                {
                    Profile = new[] { JpClinsConstants.ResourceProfiles.Practitioner },
                    LastUpdated = DateTimeOffset.UtcNow
                }
            };

            // Map practitioner identifiers (Japanese medical licenses)
            MapPractitionerIdentifiers(practitioner, practitionerData);

            // Map practitioner names
            MapPractitionerNames(practitioner, practitionerData);

            // Map qualifications and specialties
            MapQualifications(practitioner, practitionerData);

            // Map contact information
            MapContactInfo(practitioner, practitionerData);

            // Map gender and basic demographics
            MapDemographics(practitioner, practitionerData);

            return practitioner;
        }

        /// <summary>
        /// Maps practitioner identifiers including Japanese medical license numbers
        /// </summary>
        private static void MapPractitionerIdentifiers(Practitioner practitioner, dynamic practitionerData)
        {
            practitioner.Identifier = new List<Identifier>();

            // Practitioner ID (internal hospital ID)
            if (practitionerData.practitionerId != null)
            {
                practitioner.Identifier.Add(new Identifier
                {
                    System = "urn:oid:1.2.392.100495.20.3.41", // Hospital practitioner ID
                    Value = practitionerData.practitionerId.ToString(),
                    Type = FhirHelper.CreateCodeableConcept(code: "PRN", system: "http://terminology.hl7.org/CodeSystem/v2-0203", display: "Provider Number")
                });
            }

            // Medical license number (医師免許番号)
            if (practitionerData.medicalLicenseNumber != null)
            {
                var licenseNumber = practitionerData.medicalLicenseNumber.ToString();
                if (FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    practitioner.Identifier.Add(FhirHelper.CreateJapaneseIdentifier(
                        "medical-license",
                        licenseNumber));
                }
            }

            // Nursing license number (看護師免許番号)
            if (practitionerData.nursingLicenseNumber != null)
            {
                var licenseNumber = practitionerData.nursingLicenseNumber.ToString();
                if (FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    practitioner.Identifier.Add(FhirHelper.CreateJapaneseIdentifier(
                        "nurse-license",
                        licenseNumber));
                }
            }

            // Pharmacist license number (薬剤師免許番号)
            if (practitionerData.pharmacistLicenseNumber != null)
            {
                var licenseNumber = practitionerData.pharmacistLicenseNumber.ToString();
                if (FhirHelper.ValidateMedicalLicenseNumber(licenseNumber))
                {
                    practitioner.Identifier.Add(FhirHelper.CreateJapaneseIdentifier(
                        "pharmacist-license",
                        licenseNumber));
                }
            }

            // National provider identifier (if applicable)
            if (practitionerData.nationalProviderId != null)
            {
                practitioner.Identifier.Add(new Identifier
                {
                    System = "urn:oid:1.2.392.100495.20.3.31", // Japanese national provider ID
                    Value = practitionerData.nationalProviderId.ToString(),
                    Type = FhirHelper.CreateCodeableConcept(code: "NPI", system: "http://terminology.hl7.org/CodeSystem/v2-0203", display: "National Provider Identifier")
                });
            }
        }

        /// <summary>
        /// Maps practitioner names supporting Japanese Kanji and Kana
        /// </summary>
        private static void MapPractitionerNames(Practitioner practitioner, dynamic practitionerData)
        {
            practitioner.Name = new List<HumanName>();

            // Official name (通常は漢字)
            if (practitionerData.familyName != null || practitionerData.givenName != null)
            {
                var officialName = new HumanName
                {
                    Use = HumanName.NameUse.Official,
                    Family = TransformHelper.SafeGetString(practitionerData, "familyName"),
                    Given = !string.IsNullOrWhiteSpace(TransformHelper.SafeGetString(practitionerData, "givenName"))
                        ? (IEnumerable<string>)new[] { TransformHelper.SafeGetString(practitionerData, "givenName") }
                        : null
                };

                // Add title/prefix (Dr., Prof., etc.)
                var title = TransformHelper.SafeGetString(practitionerData, "title");
                if (!string.IsNullOrWhiteSpace(title))
                {
                    officialName.Prefix = (IEnumerable<string>)new[] { title };
                }

                practitioner.Name.Add(officialName);
            }

            // Phonetic name (フリガナ - Kana reading)
            if (practitionerData.familyNameKana != null || practitionerData.givenNameKana != null)
            {
                var phoneticName = new HumanName
                {
                    Extension = new List<Extension>
                    {
                        new Extension("http://hl7.org/fhir/StructureDefinition/iso21090-EN-representation",
                                    new Code("PHN")) // Phonetic representation
                    },
                    Family = TransformHelper.SafeGetString(practitionerData, "familyNameKana"),
                    Given = !string.IsNullOrWhiteSpace(TransformHelper.SafeGetString(practitionerData, "givenNameKana"))
                        ? (IEnumerable<string>)new[] { TransformHelper.SafeGetString(practitionerData, "givenNameKana") }
                        : null
                };
                practitioner.Name.Add(phoneticName);
            }
        }

        /// <summary>
        /// Maps qualifications, specialties, and credentials
        /// </summary>
        private static void MapQualifications(Practitioner practitioner, dynamic practitionerData)
        {
            practitioner.Qualification = new List<Practitioner.QualificationComponent>();

            // Medical degree
            if (practitionerData.medicalDegree != null)
            {
                var medicalDegree = new Practitioner.QualificationComponent
                {
                    Code = FhirHelper.CreateCodeableConcept(
                        code: "MD",
                        system: "http://terminology.hl7.org/CodeSystem/v2-0360",
                        display: "Doctor of Medicine"),
                    Issuer = practitionerData.medicalSchool != null
                        ? FhirHelper.CreateReference("Organization", practitionerData.medicalSchool.ToString())
                        : null
                };

                // Graduation date
                var graduationDate = TransformHelper.SafeGetDateTime(practitionerData, "graduationDate");
                if (graduationDate.HasValue)
                {
                    medicalDegree.Period = new Period
                    {
                        StartElement = new FhirDateTime(graduationDate.Value)
                    };
                }

                practitioner.Qualification.Add(medicalDegree);
            }

            // Specialty certifications (専門医資格)
            if (practitionerData.specialties != null)
            {
                var specialtiesArray = practitionerData.specialties as System.Collections.IEnumerable;
                if (specialtiesArray != null)
                {
                    foreach (var specialty in specialtiesArray)
                    {
                        var specialtyCode = TransformHelper.SafeGetString(specialty, "code", specialty.ToString() ?? "");
                        var specialtyQualification = new Practitioner.QualificationComponent
                        {
                            Code = MapJapaneseSpecialty(specialtyCode),
                            Issuer = FhirHelper.CreateReference("Organization", "japanese-medical-specialty-board")
                        };
                        practitioner.Qualification.Add(specialtyQualification);
                    }
                }
            }

            // Board certifications
            if (practitionerData.boardCertifications != null)
            {
                var certificationsArray = practitionerData.boardCertifications as System.Collections.IEnumerable;
                if (certificationsArray != null)
                {
                    foreach (var certification in certificationsArray)
                    {
                        var certificationData = certification;
                        var boardCertification = new Practitioner.QualificationComponent
                        {
                            Code = FhirHelper.CreateCodeableConcept(
                                code: TransformHelper.SafeGetString(certificationData, "code", "BOARD"),
                                system: "http://jpfhir.jp/fhir/core/CodeSystem/JP_MedicalSpecialty",
                                display: TransformHelper.SafeGetString(certificationData, "name", "")),

                            Period = new Period()
                        };

                        // Certification date
                        var certificationDate = TransformHelper.SafeGetDateTime(certificationData, "certificationDate");
                        if (certificationDate.HasValue)
                        {
                            boardCertification.Period.StartElement = new FhirDateTime(certificationDate.Value);
                        }

                        // Expiration date
                        var expirationDate = TransformHelper.SafeGetDateTime(certificationData, "expirationDate");
                        if (expirationDate.HasValue)
                        {
                            boardCertification.Period.EndElement = new FhirDateTime(expirationDate.Value);
                        }

                        practitioner.Qualification.Add(boardCertification);
                    }
                }
            }
        }

        /// <summary>
        /// Maps contact information for practitioners
        /// </summary>
        private static void MapContactInfo(Practitioner practitioner, dynamic practitionerData)
        {
            practitioner.Telecom = new List<ContactPoint>();

            // Work phone
            if (practitionerData.workPhone != null)
            {
                var workPhone = practitionerData.workPhone.ToString();
                if (FhirHelper.ValidatePhoneNumber(workPhone))
                {
                    practitioner.Telecom.Add(new ContactPoint
                    {
                        System = ContactPoint.ContactPointSystem.Phone,
                        Value = workPhone,
                        Use = ContactPoint.ContactPointUse.Work
                    });
                }
            }

            // Work email
            if (practitionerData.workEmail != null)
            {
                practitioner.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Email,
                    Value = practitionerData.workEmail.ToString(),
                    Use = ContactPoint.ContactPointUse.Work
                });
            }

            // Pager/beeper
            if (practitionerData.pager != null)
            {
                practitioner.Telecom.Add(new ContactPoint
                {
                    System = ContactPoint.ContactPointSystem.Pager,
                    Value = practitionerData.pager.ToString(),
                    Use = ContactPoint.ContactPointUse.Work
                });
            }
        }

        /// <summary>
        /// Maps basic demographics for practitioners
        /// </summary>
        private static void MapDemographics(Practitioner practitioner, dynamic practitionerData)
        {
            // Gender
            if (practitionerData.gender != null)
            {
                var genderValue = practitionerData.gender.ToString().ToLower();
                practitioner.Gender = genderValue switch
                {
                    "male" or "男性" or "男" => AdministrativeGender.Male,
                    "female" or "女性" or "女" => AdministrativeGender.Female,
                    "other" or "その他" => AdministrativeGender.Other,
                    _ => AdministrativeGender.Unknown
                };
            }

            // Birth date (if available and appropriate to include)
            if (practitionerData.birthDate != null)
            {
                if (DateTime.TryParse(practitionerData.birthDate.ToString(), out DateTime birthDate))
                {
                    practitioner.BirthDateElement = new Date(birthDate.Year, birthDate.Month, birthDate.Day);
                }
            }

            // Active status
            if (practitionerData.active != null)
            {
                practitioner.Active = bool.Parse(practitionerData.active.ToString());
            }
            else
            {
                practitioner.Active = true; // Default to active
            }
        }

        /// <summary>
        /// Maps Japanese medical specialty codes to CodeableConcepts
        /// </summary>
        private static CodeableConcept MapJapaneseSpecialty(string specialtyCode)
        {
            // Map common Japanese medical specialties
            var japaneseSpecialties = new Dictionary<string, (string code, string display)>
            {
                { "internal", ("01", "内科") },
                { "surgery", ("02", "外科") },
                { "pediatrics", ("03", "小児科") },
                { "obstetrics", ("04", "産婦人科") },
                { "orthopedics", ("05", "整形外科") },
                { "neurology", ("06", "神経内科") },
                { "psychiatry", ("07", "精神科") },
                { "dermatology", ("08", "皮膚科") },
                { "ophthalmology", ("09", "眼科") },
                { "otolaryngology", ("10", "耳鼻咽喉科") },
                { "radiology", ("11", "放射線科") },
                { "anesthesiology", ("12", "麻酔科") },
                { "emergency", ("13", "救急科") },
                { "rehabilitation", ("14", "リハビリテーション科") },
                { "pathology", ("15", "病理診断科") }
            };

            if (japaneseSpecialties.TryGetValue(specialtyCode.ToLower(), out var specialty))
            {
                return FhirHelper.CreateCodeableConcept(
                    code: specialty.code,
                    system: "http://jpfhir.jp/fhir/core/CodeSystem/JP_MedicalSpecialty",
                    display: specialty.display);
            }

            // Default specialty mapping
            return FhirHelper.CreateCodeableConcept(
                code: specialtyCode,
                system: "http://jpfhir.jp/fhir/core/CodeSystem/JP_MedicalSpecialty",
                display: specialtyCode);
        }

        // TODO: JP-CLINS Practitioner Mapping Notes:
        // 1. Validate Japanese medical license number formats (doctor, nurse, pharmacist)
        // 2. Support multiple practice locations with different roles
        // 3. Map Japanese medical specialty board certifications accurately
        // 4. Include occupational health physician qualifications (産業医)
        // 5. Support for temporary/locum practitioner arrangements
        // 6. Map hospital department affiliations and roles
        // 7. Include language proficiency for international patients
        // 8. Support for research qualifications and publications
        // 9. Map continuing medical education (CME) requirements
        // 10. Include emergency contact information for practitioners
        // 11. Support for telemedicine licensing and qualifications
        // 12. Map academic appointments and teaching responsibilities
    }
}