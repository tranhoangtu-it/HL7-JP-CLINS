using Newtonsoft.Json;
using System;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Reference
    {
        [JsonProperty("reference", NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceValue { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Type { get; set; }

        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
        public string Display { get; set; }

        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
        public Identifier Identifier { get; set; }
    }
}