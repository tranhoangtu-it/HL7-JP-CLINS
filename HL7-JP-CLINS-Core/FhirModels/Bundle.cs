using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Bundle : FhirResource
    {
        public override string ResourceType => "Bundle";

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonProperty("entry")]
        public List<BundleEntry> Entry { get; set; } = new();
    }
}