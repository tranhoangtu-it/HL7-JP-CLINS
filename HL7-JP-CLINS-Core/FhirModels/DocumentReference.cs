using System.Collections.Generic;
using Newtonsoft.Json;
using HL7_JP_CLINS_Core.FhirModels.Base;

namespace HL7_JP_CLINS_Core.FhirModels
{
    public class DocumentReference : FhirResource
    {
        public override string ResourceType => "DocumentReference";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("type")]
        public CodeableConcept Type { get; set; }

        [JsonProperty("category")]
        public List<CodeableConcept> Category { get; set; }

        [JsonProperty("subject")]
        public Reference Subject { get; set; }

        [JsonProperty("author")]
        public List<Reference> Author { get; set; }

        [JsonProperty("content")]
        public List<DocumentReferenceContent> Content { get; set; }
    }

    public class DocumentReferenceContent
    {
        [JsonProperty("attachment")]
        public Attachment Attachment { get; set; }

        [JsonProperty("format")]
        public Coding Format { get; set; }
    }

    public class Attachment
    {
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}