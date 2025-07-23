using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Patient : FhirResource
    {
        public override string ResourceType => "Patient";

        [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
        public List<Identifier> Identifier { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public List<HumanName> Name { get; set; }

        [JsonProperty("gender", NullValueHandling = NullValueHandling.Ignore)]
        public string Gender { get; set; }

        [JsonProperty("birthDate", NullValueHandling = NullValueHandling.Ignore)]
        public string BirthDate { get; set; } // ISO date string
    }
}