using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Practitioner : FhirResource
    {
        public override string ResourceType => "Practitioner";

        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
        public List<Identifier> Identifier { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public List<HumanName> Name { get; set; }
    }
}