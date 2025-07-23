using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.FhirModels.Base
{
    public class FhirElement
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        // Extension, modifierExtension, etc. có thể bổ sung sau
    }
}