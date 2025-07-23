using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class BundleEntrySearch
    {
        [JsonProperty("mode", NullValueHandling = NullValueHandling.Ignore)]
        public string Mode { get; set; }
    }
}