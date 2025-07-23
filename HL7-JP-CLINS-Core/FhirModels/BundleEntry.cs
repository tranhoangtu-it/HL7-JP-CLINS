using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class BundleEntry
    {
        [JsonProperty("fullUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string FullUrl { get; set; }

        [JsonProperty("resource")]
        public FhirResource Resource { get; set; }

        [JsonProperty("search", NullValueHandling = NullValueHandling.Ignore)]
        public BundleEntrySearch Search { get; set; }
    }
}