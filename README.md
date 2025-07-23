# HL7-JP-CLINS Project

A comprehensive .NET solution for converting hospital data to HL7 FHIR R4 format according to JP-CLINS v1.11.0 specification.

## 🏗️ Project Architecture

This solution follows a 3-layer architecture:

```
HL7-JP-CLINS-Git/
├── HL7-JP-CLINS-Core/          # 📚 Core Models & Utilities
├── HL7-JP-CLINS-Transforms/    # 🔄 Data Transformation Logic  
├── HL7-JP-CLINS-API/           # 🌐 REST API Endpoints
└── HL7-JP-CLINS-Angular/       # 🖥️ Frontend (Placeholder)
```

### 📚 HL7-JP-CLINS-Core

**Foundation layer containing:**

- **FHIR Models**: Patient, Practitioner, Organization, etc.
- **Document Models**: EReferralDocument, EDischargeSummaryDocument, ECheckupDocument
- **Input Models**: Simplified POCOs for hospital data input
- **Utilities**: Validation, JP-CLINS compliance helpers
- **Constants**: Japanese medical coding systems (YJ codes, JLAC10, etc.)

### 🔄 HL7-JP-CLINS-Transforms

**Business logic layer containing:**

- **Transformers**: Convert input models to FHIR Bundles
- **Mappers**: Map specific resource types (Patient, Practitioner)
- **Utilities**: Safe dynamic property access, Japanese validation
- **Base Classes**: Common transformation patterns

### 🌐 HL7-JP-CLINS-API

**Presentation layer containing:**

- **REST Controllers**: Conversion endpoints
- **Services**: FHIR serialization (JSON/XML)
- **Models**: API request/response wrappers
- **Configuration**: Dependency injection, CORS, Swagger

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- (Optional) Postman for API testing

### Build & Run

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd HL7-JP-CLINS-Git
   ```

2. **Build the solution**

   ```bash
   # Build all projects
   dotnet build HL7-JP-CLINS-API.sln
   
   # Or build individually
   cd HL7-JP-CLINS-Core && dotnet build
   cd ../HL7-JP-CLINS-Transforms && dotnet build  
   cd ../HL7-JP-CLINS-API && dotnet build
   ```

3. **Run the API**

   ```bash
   cd HL7-JP-CLINS-API
   dotnet run
   ```

4. **Access Swagger UI**

   ```
   https://localhost:7000
   ```

## 📋 API Endpoints

### Document Conversion

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/hl7conversion/ereferral` | POST | Convert eReferral documents |
| `/api/hl7conversion/dischargesummary` | POST | Convert eDischargeSummary documents |
| `/api/hl7conversion/checkup` | POST | Convert eCheckup documents |

### System Information  

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/hl7conversion/health` | GET | API health status |
| `/api/hl7conversion/capabilities` | GET | Supported features |

### Query Parameters

- `format`: `json` or `xml` (default: `json`)
- `prettyFormat`: `true` or `false` (default: `true`)

## 🇯🇵 JP-CLINS v1.11.0 Compliance

### Supported Japanese Medical Codes

- **YJ Codes**: 薬価基準コード (4桁数字+2桁英字+3桁数字+1桁英字+1桁数字)
- **HOT Codes**: 医薬品ホットコード (9桁数字)  
- **JLAC10**: 臨床検査項目コード (17文字)
- **ICD-10-CM-JP**: 日本版疾病分類
- **JJ1017**: 日本の手術・処置コード

### Japanese Healthcare Features

- **医療従事者免許**: 医師(6桁)、看護師(8桁)、薬剤師(1英字+6桁)
- **日本語氏名**: 漢字・カナ対応
- **住所**: 郵便番号(7桁)、都道府県コード
- **電話番号**: 携帯(090/080/070)、固定電話
- **職域健診**: 労働安全衛生法対応

### Document Types

- **eReferral**: 電子診療情報提供書
- **eDischargeSummary**: 電子退院時サマリー  
- **eCheckup**: 電子健診結果報告書

## 🧪 Usage Examples

### eReferral Conversion

```bash
curl -X POST "https://localhost:7000/api/hl7conversion/ereferral?format=json" \
  -H "Content-Type: application/json" \
  -d '{
    "patientReference": {
      "reference": "Patient/jp-patient-001"
    },
    "authorReference": {
      "reference": "Practitioner/jp-practitioner-001"
    },
    "organizationReference": {
      "reference": "Organization/jp-organization-001"
    },
    "referralReason": "腹痛の精査",
    "urgency": "routine",
    "serviceRequested": [
      {
        "coding": [
          {
            "system": "http://loinc.org",
            "code": "33747-0",
            "display": "General medicine consultation"
          }
        ]
      }
    ],
    "status": "final"
  }'
```

### Response (FHIR Bundle)

```json
{
  "resourceType": "Bundle",
  "id": "jp-ereferral-bundle-001",
  "meta": {
    "lastUpdated": "2024-01-15T10:30:00+09:00",
    "profile": [
      "http://jpfhir.jp/fhir/clins/StructureDefinition/JP_Bundle_eReferral"
    ]
  },
  "type": "document",
  "entry": [
    {
      "resource": {
        "resourceType": "Composition",
        "id": "jp-composition-ereferral-001",
        "status": "final",
        "type": {
          "coding": [
            {
              "system": "http://loinc.org",
              "code": "57133-1",
              "display": "Referral note"
            }
          ]
        }
      }
    }
  ]
}
```

## 🛠️ Development

### Key Classes

#### Core Models

```csharp
// Document models
EReferralDocument
EDischargeSummaryDocument  
ECheckupDocument

// Base classes
ClinsDocumentBase
IClinsDocument
```

#### Transformers

```csharp
// Main transformers
EReferralTransformer
EDischargeSummaryTransformer
ECheckupTransformer

// Base transformer
BaseTransformer<TInput>
IDocumentTransformer<TInput>
```

#### API Controllers

```csharp
// Main controller
Hl7ConversionController

// Services
IFhirSerializationService
FhirSerializationService
```

### Adding New Document Types

1. **Create document model** in `HL7-JP-CLINS-Core/Models/Documents/`
2. **Create transformer** in `HL7-JP-CLINS-Transforms/Transformers/`
3. **Add API endpoint** in `HL7-JP-CLINS-API/Controllers/`
4. **Register dependencies** in `Program.cs`

### Testing

```bash
# Unit tests (when available)
dotnet test

# Integration tests
dotnet run --project HL7-JP-CLINS-API
# Use Postman or curl to test endpoints
```

## 📖 Documentation

- **API Documentation**: Available at `/` when running (Swagger UI)
- **JP-CLINS Specification**: <https://jpfhir.jp/fhir/clins/igv1/index.html>
- **FHIR R4 Specification**: <https://hl7.org/fhir/R4/>
- **Usage Examples**: See `HL7-JP-CLINS-API/Examples/ApiUsageExamples.md`

## 🔧 Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: API binding URLs

## 🚀 Deployment

### Production Considerations

1. **Authentication**: Implement JWT/OAuth2
2. **Authorization**: Role-based access control
3. **Rate Limiting**: Prevent API abuse
4. **HTTPS**: Enforce secure connections
5. **Audit Logging**: Track healthcare data access
6. **Data Validation**: Comprehensive input validation
7. **Error Handling**: Sanitized error responses

### Docker (Future)

```dockerfile
# Dockerfile example
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY ./publish /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "HL7-JP-CLINS-API.dll"]
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **JP-FHIR**: Japanese FHIR implementation community
- **HL7 International**: FHIR specification
- **Firely**: .NET FHIR library
- **Japanese Ministry of Health**: Healthcare data standards

---

## 📞 Support

For questions or support:

- 📧 Email: <support@example.com>
- 📖 Documentation: <https://your-docs-site.com>
- 🐛 Issues: <https://github.com/your-repo/issues>

---

**Built with ❤️ for Japanese Healthcare Industry**
