# HL7-JP-CLINS API Usage Examples

このドキュメントは、HL7-JP-CLINS API の使用方法を説明します。

## API エンドポイント

### Base URL

```
https://localhost:7000/api/hl7conversion
```

## 1. eReferral 変換

### エンドポイント

```
POST /api/hl7conversion/ereferral
```

### パラメータ

- `format`: "json" または "xml" (デフォルト: "json")
- `prettyFormat`: true/false (デフォルト: true)

### リクエスト例 (JSON)

```bash
curl -X POST "https://localhost:7000/api/hl7conversion/ereferral?format=json&prettyFormat=true" \
  -H "Content-Type: application/json" \
  -d '{
    "patientReference": {
      "reference": "Patient/jp-patient-001"
    },
    "authorReference": {
      "reference": "Practitioner/jp-practitioner-001"
    },
    "custodianReference": {
      "reference": "Organization/jp-organization-001"
    },
    "encounter": {
      "reference": "Encounter/jp-encounter-001"
    },
    "sections": [
      {
        "title": "Chief Complaint",
        "code": {
          "coding": [
            {
              "system": "http://loinc.org",
              "code": "10154-3",
              "display": "Chief complaint Narrative"
            }
          ]
        },
        "text": {
          "status": "generated",
          "div": "<div xmlns=\"http://www.w3.org/1999/xhtml\">腹痛</div>"
        }
      }
    ],
    "documentStatus": "final",
    "createdAt": "2024-01-15T10:30:00+09:00"
  }'
```

### レスポンス例

```json
{
  "resourceType": "Bundle",
  "id": "jp-ereferral-bundle-001",
  "meta": {
    "lastUpdated": "2024-01-15T10:30:00+09:00",
    "profile": [
      "http://jpfhir.jp/fhir/clins/StructureDefinition/JP-Bundle-eReferral"
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
              "code": "18761-7",
              "display": "Provider-unspecified procedure note"
            }
          ]
        }
      }
    }
  ]
}
```

## 2. eDischargeSummary 変換

### エンドポイント

```
POST /api/hl7conversion/dischargesummary
```

### リクエスト例

```bash
curl -X POST "https://localhost:7000/api/hl7conversion/dischargesummary?format=xml" \
  -H "Content-Type: application/json" \
  -d '{
    "patientReference": {
      "reference": "Patient/jp-patient-002"
    },
    "authorReference": {
      "reference": "Practitioner/jp-practitioner-002"
    },
    "custodianReference": {
      "reference": "Organization/jp-hospital-001"
    },
    "encounter": {
      "reference": "Encounter/jp-encounter-002"
    },
    "sections": [
      {
        "title": "Discharge Diagnosis",
        "code": {
          "coding": [
            {
              "system": "http://loinc.org",
              "code": "11535-2",
              "display": "Hospital discharge diagnosis"
            }
          ]
        },
        "text": {
          "status": "generated",
          "div": "<div xmlns=\"http://www.w3.org/1999/xhtml\">急性胃炎</div>"
        }
      }
    ],
    "documentStatus": "final",
    "createdAt": "2024-01-20T16:45:00+09:00"
  }'
```

## 3. eCheckup 変換

### エンドポイント

```
POST /api/hl7conversion/checkup
```

### リクエスト例

```bash
curl -X POST "https://localhost:7000/api/hl7conversion/checkup?format=json" \
  -H "Content-Type: application/json" \
  -d '{
    "patientReference": {
      "reference": "Patient/jp-patient-003"
    },
    "authorReference": {
      "reference": "Practitioner/jp-practitioner-003"
    },
    "custodianReference": {
      "reference": "Organization/jp-clinic-001"
    },
    "encounter": {
      "reference": "Encounter/jp-checkup-001"
    },
    "sections": [
      {
        "title": "Vital Signs",
        "code": {
          "coding": [
            {
              "system": "http://loinc.org",
              "code": "8716-3",
              "display": "Vital signs"
            }
          ]
        },
        "text": {
          "status": "generated",
          "div": "<div xmlns=\"http://www.w3.org/1999/xhtml\">血圧: 120/80 mmHg, 体重: 70 kg</div>"
        }
      }
    ],
    "documentStatus": "final",
    "createdAt": "2024-01-25T09:15:00+09:00"
  }'
```

## 4. Health Check

### エンドポイント

```
GET /api/hl7conversion/health
```

### リクエスト例

```bash
curl -X GET "https://localhost:7000/api/hl7conversion/health"
```

### レスポンス例

```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "version": "1.0.0",
  "service": "HL7-JP-CLINS-API",
  "jpClinsVersion": "v1.11.0"
}
```

## 5. Capabilities

### エンドポイント

```
GET /api/hl7conversion/capabilities
```

### リクエスト例

```bash
curl -X GET "https://localhost:7000/api/hl7conversion/capabilities"
```

### レスポンス例

```json
{
  "supportedDocumentTypes": [
    {
      "type": "eReferral",
      "endpoint": "/api/hl7conversion/ereferral",
      "description": "Electronic referral documents"
    },
    {
      "type": "eDischargeSummary",
      "endpoint": "/api/hl7conversion/dischargesummary",
      "description": "Electronic discharge summary documents"
    },
    {
      "type": "eCheckup",
      "endpoint": "/api/hl7conversion/checkup",
      "description": "Electronic health checkup documents"
    }
  ],
  "supportedFormats": ["json", "xml"],
  "jpClinsVersion": "v1.11.0",
  "fhirVersion": "R4",
  "features": [
    "Japanese medical coding (YJ codes, HOT codes, JLAC10)",
    "Japanese healthcare provider validation",
    "Occupational health compliance",
    "Multi-format output (JSON/XML)"
  ]
}
```

## エラーハンドリング

### 400 Bad Request

```json
{
  "success": false,
  "errorMessage": "Document validation failed",
  "validationErrors": [
    "Patient reference is required",
    "Document status must be 'final' or 'preliminary'"
  ],
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### 500 Internal Server Error

```json
{
  "success": false,
  "errorMessage": "Internal server error during conversion",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

## Content Types

### リクエスト

- `Content-Type: application/json`

### レスポンス

- JSON: `application/fhir+json; charset=utf-8`
- XML: `application/fhir+xml; charset=utf-8`

## ヘッダー

### リクエストヘッダー

- `X-Correlation-ID`: オプション（指定しない場合は自動生成される）

### レスポンスヘッダー

- `X-Correlation-ID`: リクエスト追跡用のID
- `Content-Type`: レスポンスのMIMEタイプ

## JP-CLINS 固有の機能

### 日本の医療コード対応

- **YJ codes**: 薬価基準コード（4桁数字+2桁英字+3桁数字+1桁英字+1桁数字）
- **HOT codes**: 医薬品ホットコード（9桁数字）
- **JLAC10**: 臨床検査項目コード（17文字）

### 日本の医療従事者免許

- **医師**: 6桁数字
- **看護師**: 8桁数字  
- **薬剤師**: 1桁英字+6桁数字

### 日本固有の検証

- 郵便番号（7桁数字）
- 電話番号（携帯: 090/080/070、固定電話）
- 氏名（漢字・カナ対応）
