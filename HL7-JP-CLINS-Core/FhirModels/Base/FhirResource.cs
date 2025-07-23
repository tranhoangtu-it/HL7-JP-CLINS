using Newtonsoft.Json;

namespace HL7_JP_CLINS_Core.FhirModels.Base
{
    public abstract class FhirResource : FhirBackboneElement
    {
        [JsonProperty("resourceType")]
        public abstract string ResourceType { get; }
    }
}