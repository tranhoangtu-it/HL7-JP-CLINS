using Newtonsoft.Json;
using System.Collections.Generic;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class HumanName
    {
        [JsonProperty("use", NullValueHandling = NullValueHandling.Ignore)]
        public string Use { get; set; }

        [JsonProperty("family", NullValueHandling = NullValueHandling.Ignore)]
        public string Family { get; set; }

        [JsonProperty("given", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Given { get; set; }

        [JsonProperty("prefix", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Prefix { get; set; }

        [JsonProperty("suffix", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Suffix { get; set; }
    }
}