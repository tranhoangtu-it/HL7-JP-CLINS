using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class ReferralRequest : FhirResource
    {
        public override string ResourceType => "ReferralRequest";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("requester")]
        public Reference Requester { get; set; }

        [JsonProperty("recipient")]
        public List<Reference> Recipient { get; set; }

        [JsonProperty("reasonCode")]
        public List<CodeableConcept> ReasonCode { get; set; }
    }
}