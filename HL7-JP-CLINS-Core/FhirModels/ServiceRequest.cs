using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class ServiceRequest : FhirResource
    {
        public override string ResourceType => "ServiceRequest";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("requester")]
        public Reference Requester { get; set; }

        [JsonProperty("code")]
        public CodeableConcept Code { get; set; }

        [JsonProperty("reasonCode")]
        public List<CodeableConcept> ReasonCode { get; set; }

        [JsonProperty("priority")]
        public string Priority { get; set; }
    }
}