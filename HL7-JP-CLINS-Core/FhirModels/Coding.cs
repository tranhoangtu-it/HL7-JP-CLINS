using Newtonsoft.Json;
using System;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Coding
    {
        [JsonProperty("system", NullValueHandling = NullValueHandling.Ignore)]
        public Uri System { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }

        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
        public string Display { get; set; }
    }
}