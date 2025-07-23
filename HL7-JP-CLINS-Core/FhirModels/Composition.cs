using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class Composition : FhirResource
    {
        public override string ResourceType => "Composition";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public CodeableConcept Type { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("author")]
        public List<Reference> Author { get; set; }

        [JsonProperty("attester", NullValueHandling = NullValueHandling.Ignore)]
        public List<Reference> Attester { get; set; }

        [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
        public List<CodeableConcept> Event { get; set; }

        [JsonProperty("section", NullValueHandling = NullValueHandling.Ignore)]
        public List<CompositionSection> Section { get; set; }
    }

    public class CompositionSection
    {
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public CodeableConcept Code { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }
}