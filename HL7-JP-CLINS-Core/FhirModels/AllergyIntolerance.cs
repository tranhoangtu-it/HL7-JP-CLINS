using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class AllergyIntolerance : FhirResource
    {
        public override string ResourceType => "AllergyIntolerance";

        [JsonProperty("clinicalStatus")]
        public CodeableConcept ClinicalStatus { get; set; }

        [JsonProperty("verificationStatus")]
        public CodeableConcept VerificationStatus { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Category { get; set; }

        [JsonProperty("criticality", NullValueHandling = NullValueHandling.Ignore)]
        public string Criticality { get; set; }

        [JsonProperty("code")]
        public CodeableConcept Code { get; set; }

        [JsonProperty("patient")]
        public Reference Patient { get; set; }

        [JsonProperty("reaction", NullValueHandling = NullValueHandling.Ignore)]
        public List<AllergyIntoleranceReaction> Reaction { get; set; }
    }

    public class AllergyIntoleranceReaction
    {
        [JsonProperty("manifestation")]
        public List<CodeableConcept> Manifestation { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
    }
}