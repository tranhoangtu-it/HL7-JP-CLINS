using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Observation : FhirResource
    {
        public override string ResourceType => "Observation";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
        public List<CodeableConcept> Category { get; set; }

        [JsonProperty("code")]
        public CodeableConcept Code { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("effectiveDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDateTime { get; set; }

        [JsonProperty("valueString", NullValueHandling = NullValueHandling.Ignore)]
        public string ValueString { get; set; }
    }
}