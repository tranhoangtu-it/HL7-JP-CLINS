using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Encounter : FhirResource
    {
        public override string ResourceType => "Encounter";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("class")]
        public Coding Class { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("participant")]
        public List<EncounterParticipant> Participant { get; set; }

        [JsonProperty("period")]
        public Period Period { get; set; }
    }

    public class EncounterParticipant
    {
        [JsonProperty("type")]
        public List<CodeableConcept> Type { get; set; }

        [JsonProperty("individual")]
        public Reference Individual { get; set; }
    }
}