using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class MedicationRequest : FhirResource
    {
        public override string ResourceType => "MedicationRequest";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("medicationCodeableConcept")]
        public CodeableConcept Medication { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("requester", NullValueHandling = NullValueHandling.Ignore)]
        public Reference Requester { get; set; }

        [JsonProperty("dosageInstruction", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> DosageInstruction { get; set; }
    }
}