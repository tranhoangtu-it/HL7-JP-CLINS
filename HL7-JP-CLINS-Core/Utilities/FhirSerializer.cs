using Newtonsoft.Json;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace HL7_JP_CLINS_Core.Utilities
{
    /// <summary>
    /// Utility for serializing FHIR resources to JSON/XML.
    /// </summary>
    public static class FhirSerializer
    {
        /// <summary>
        /// Serialize a FHIR resource to JSON string.
        /// </summary>
        public static string SerializeToJson<T>(T resource)
        {
            // TODO: Customize settings for FHIR compliance if needed
            return JsonConvert.SerializeObject(resource, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        /// <summary>
        /// Serialize a FHIR resource to XML string.
        /// </summary>
        public static string SerializeToXml<T>(T resource)
        {
            // TODO: Customize for FHIR XML compliance (attributes, etc.)
            var serializer = new XmlSerializer(typeof(T));
            using var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, resource);
            return stringWriter.ToString();
        }
    }
}
// WARNING: Việc tự xây dựng FHIR models và serialization/deserialization
// đòi hỏi hiểu sâu về chuẩn FHIR R4, các profile, extension, slicing, cardinality, v.v.
// Để tuân thủ JP-CLINS và FHIR R4 thực sự, cần kiểm tra kỹ các constraint, validation, và các trường hợp đặc biệt.
// Nếu dùng cho production, nên xây dựng bộ test FHIR validator riêng hoặc tích hợp với các validator open-source. 