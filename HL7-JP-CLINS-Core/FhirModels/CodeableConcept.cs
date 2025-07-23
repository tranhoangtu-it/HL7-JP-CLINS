using System.Collections.Generic;
using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class CodeableConcept
    {
        [JsonProperty("coding", NullValueHandling = NullValueHandling.Ignore)]
        public List<Coding> Coding { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }
}