using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Condition : FhirResource
    {
        public override string ResourceType => "Condition";

        [JsonProperty("clinicalStatus")]
        public CodeableConcept ClinicalStatus { get; set; }

        [JsonProperty("verificationStatus")]
        public CodeableConcept VerificationStatus { get; set; }

        [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
        public List<CodeableConcept> Category { get; set; }

        [JsonProperty("code")]
        public CodeableConcept Code { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("onsetDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public string OnsetDateTime { get; set; }
    }
}