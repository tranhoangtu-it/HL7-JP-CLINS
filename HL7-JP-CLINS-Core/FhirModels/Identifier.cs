using Newtonsoft.Json;
using System;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Identifier
    {
        [JsonProperty("use", NullValueHandling = NullValueHandling.Ignore)]
        public string Use { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public CodeableConcept Type { get; set; }

        [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
        public Uri System { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonProperty("period", NullValueHandling = NullValueHandling.Ignore)]
        public Period Period { get; set; }

        [JsonProperty("assigner", NullValueHandling = NullValueHandling.Ignore)]
        public Reference Assigner { get; set; }
    }
}