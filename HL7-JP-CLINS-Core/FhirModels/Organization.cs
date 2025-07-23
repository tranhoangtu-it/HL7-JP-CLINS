using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Organization : FhirResource
    {
        public override string ResourceType => "Organization";

        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
        public List<Identifier> Identifier { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}