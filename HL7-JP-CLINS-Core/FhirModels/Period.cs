using Newtonsoft.Json;
using System;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Period
    {
        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Start { get; set; }

        [JsonProperty("end", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? End { get; set; }
    }
}