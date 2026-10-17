# Invoice Module API Specification

## Overview

The Invoice Module provides REST endpoints for managing invoices, templates, and their generation. This module handles invoice drafts, PDF templates, invoice previews, and final invoice generation.

**Base URL**: `/api/invoices`

---

## Endpoints

### 1. Get Invoice Settings (All Profiles)

Retrieves all available business profiles for invoice management and selection.

```
GET /api/invoices/settings
```

**Response: 200 OK**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "issuerName": "My Business",
    "issuerEmail": "billing@mybusiness.com",
    "defaultCurrencyCode": "USD",
    "invoicePrefix": "INV",
    "nextSequence": 100,
    "defaultTaxRate": 0.1,
    "taxLabel": "Sales Tax",
    "updatedAtUtc": "2025-01-15T10:00:00Z"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "issuerName": "Secondary Business",
    "issuerEmail": "billing@secondary.com",
    "defaultCurrencyCode": "MYR",
    "invoicePrefix": "INV",
    "nextSequence": 50,
    "defaultTaxRate": 0.06,
    "taxLabel": "GST",
    "updatedAtUtc": "2025-01-10T15:00:00Z"
  }
]
```

**Error Responses**:
- `200 OK with empty array`: If no business profiles are configured

---

### 2. List Invoices

Retrieves all invoices with basic information.

```
GET /api/invoices
```

**Response: 200 OK**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "invoiceNumber": "INV-100",
    "status": 0,
    "currencyCode": "USD",
    "grandTotal": 1500.00,
    "issueDate": "2024-01-15",
    "customerName": "Acme Corp"
  }
]
```

`customerName` is derived from the linked Business Profile name.

**Status Values**:
- `0` = Draft
- `1` = Generated

---

### 3. Create Invoice Draft

Creates a new invoice draft with line items and optional custom columns.

```
POST /api/invoices/drafts
Content-Type: application/json
```

**Request Body**:
```json
{
  "businessProfileId": "550e8400-e29b-41d4-a716-446655440000",
  "notes": "Invoice notes here",
  "currencyCode": "USD",
  "terms": "Payment due within 30 days",
  "issueDate": "2024-01-15",
  "dueDate": "2024-02-15",
  "lineItemColumns": ["description:text", "hours:number", "rate:number", "total:number"],
  "lineItems": [
    {
      "description": "Web Development Services",
      "total": 1500.00,
      "otherColumns": "hours: 10, rate: 150.00"
    }
  ]
}
```

**Response: 200 OK**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "businessProfileId": "550e8400-e29b-41d4-a716-446655440000",
  "invoiceNumber": "INV-100",
  "status": 0,
  "grandTotal": 1500.00,
  "createdAtUtc": "2024-01-15T10:00:00Z",
  "lineItemColumns": ["description:text", "hours:number", "rate:number", "total:number"]
}
```

**Validation Errors: 400 Bad Request**
```json
{
  "message": "At least one line item is required."
}
```

**Line Item Columns and Values**:
- `lineItemColumns` uses profile-defined `name:type` values (for example `"description:text"`).
- `lineItems[].otherColumns` stores non-core values as aggregated text.
- `lineItems[].otherColumns` format: `"columnName: value, columnName: value"`.
- `lineItems[].total` is required for each line item.
- `invoice.grandTotal` is calculated as the sum of all `lineItems[].total`.

---

### 4. List Invoice Templates

Retrieves all available invoice templates.

```
GET /api/invoices/templates
```

**Response: 200 OK**
```json
[
  {
    "id": "650e8400-e29b-41d4-a716-446655440000",
    "name": "Standard Invoice",
    "description": "Standard invoice template",
    "activeVersionId": "750e8400-e29b-41d4-a716-446655440000",
    "isArchived": false,
    "activeVersionNumber": 1,
    "activeVersionSourceType": "Pdf",
    "updatedAtUtc": "2024-01-10T15:00:00Z"
  }
]
```

---

### 5. Create Invoice Template

Creates a new invoice template with metadata.

```
POST /api/invoices/templates
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "Premium Invoice",
  "description": "Premium template for high-value invoices",
  "sourceStorageKey": "templates/premium-v1.pdf",
  "sourceType": "Pdf"
}
```

**Response: 200 OK**
```json
{
  "id": "650e8400-e29b-41d4-a716-446655440001",
  "name": "Premium Invoice",
  "activeVersionId": "750e8400-e29b-41d4-a716-446655440001",
  "activeVersionNumber": 1,
  "activeVersionLayoutSchemaVersion": 1,
  "createdAtUtc": "2024-01-15T10:00:00Z"
}
```

**Validation Errors: 400 Bad Request**
```json
{
  "message": "Template name is required." | "Source storage key is required."
}
```

---

### 6. Upload Invoice Template File

Uploads a PDF or image template file and creates a new template version.

```
POST /api/invoices/templates/upload
Content-Type: multipart/form-data
```

**Form Parameters**:
- `file` (required): PDF or image file
- `name` (required): Template name
- `description` (optional): Template description
- `selectedPdfPageNumber` (optional): Page number for multi-page PDFs
- `pageWidthPoints` (optional): Page width in points (default: 595)
- `pageHeightPoints` (optional): Page height in points (default: 842)
- `layoutSchemaVersion` (optional): Schema version (default: 1)
- `layoutJson` (optional): Field layout configuration

**Response: 200 OK**
```json
{
  "id": "650e8400-e29b-41d4-a716-446655440002",
  "name": "Uploaded Template",
  "activeVersionId": "750e8400-e29b-41d4-a716-446655440002",
  "activeVersionNumber": 1,
  "activeVersionSourceStorageKey": "storage/template-abc123.pdf",
  "activeVersionSourceType": "Pdf",
  "createdAtUtc": "2024-01-15T10:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: File not provided or invalid
- `500 Internal Server Error`: Upload or storage error

---

### 7. Update Template Layout

Updates the field layout configuration for a template version.

```
POST /api/invoices/templates/{templateId}/layout
Content-Type: application/json
```

**Path Parameters**:
- `templateId` (required): Template identifier

**Request Body**:
```json
{
  "schemaVersion": 1,
  "fields": [
    {
      "id": "issuer-name",
      "x": 50,
      "y": 20,
      "width": 200,
      "height": 20,
      "fontSize": 14
    },
    {
      "id": "customer-address",
      "x": 50,
      "y": 100,
      "width": 250,
      "height": 60,
      "fontSize": 11
    }
  ]
}
```

**Response: 200 OK**
```json
{
  "id": "750e8400-e29b-41d4-a716-446655440003",
  "templateId": "650e8400-e29b-41d4-a716-446655440002",
  "versionNumber": 2,
  "layoutSchemaVersion": 1,
  "layoutJson": "{\"schemaVersion\":1,\"fields\":[...]}",
  "createdAtUtc": "2024-01-15T10:30:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid layout configuration
- `404 Not Found`: Template not found

---

### 8. Preview Template

Generates a PDF preview of the template without invoice data.

```
POST /api/invoices/templates/{templateId}/preview
```

**Path Parameters**:
- `templateId` (required): Template identifier

**Response: 200 OK**
```
Content-Type: application/pdf
[PDF binary data]
```

**Error Responses**:
- `404 Not Found`: Template not found
- `400 Bad Request`: No active template version

---

### 9. Preview Invoice

Generates a PDF preview of an invoice draft using the active template.

```
POST /api/invoices/{invoiceId}/preview
```

**Path Parameters**:
- `invoiceId` (required): Invoice identifier (UUID)

**Response: 200 OK**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="invoice-preview.pdf"
[PDF binary data]
```

**Error Responses**:
- `400 Bad Request`: Invoice not found or template not configured
- `500 Internal Server Error`: PDF generation failed

---

### 10. Generate Invoice

Generates a final invoice PDF and marks the invoice as generated.

```
POST /api/invoices/{invoiceId}/generate
```

**Path Parameters**:
- `invoiceId` (required): Invoice identifier (UUID)

**Response: 200 OK**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "invoiceNumber": "INV-100",
  "status": 1,
  "pdfStorageKey": "invoices/generated/INV-100-abc123.pdf",
  "pdfSha256": "abc123def456...",
  "generatedAtUtc": "2024-01-15T10:45:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Invoice not found or generation failed
- `500 Internal Server Error`: PDF generation or storage error

---

### 11. Delete Invoice

Deletes a draft invoice. Only invoices with Draft status can be deleted. Deleting an invoice also removes all associated line items due to CASCADE constraints.

```
DELETE /api/invoices/{invoiceId}
```

**Path Parameters**:
- `invoiceId` (required): Invoice identifier (UUID)

**Response: 204 No Content**

Empty response body on successful deletion.

**Error Responses**:
- `400 Bad Request`: Invoice not found or not a draft invoice
  ```json
  {
    "message": "Invoice not found." | "Only draft invoices can be deleted."
  }
  ```
- `500 Internal Server Error`: Database or deletion error

---

### 12. List Business Profiles

Retrieves all business profiles.

```
GET /api/invoices/profiles
```

**Query Parameters**: None

**Response: 200 OK**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "issuerName": "Acme Corporation",
    "issuerEmail": "billing@acme.com",
    "defaultCurrencyCode": "USD",
    "invoicePrefix": "INV",
    "nextSequence": 1,
    "defaultTaxRate": 0.10,
    "taxLabel": "Sales Tax",
    "updatedAtUtc": "2025-01-01T12:00:00Z"
  }
]
```

**Error Responses**:
- `500 Internal Server Error`: Database retrieval error

---

### 13. Get Business Profile

Retrieves a specific business profile by ID with full details.

```
GET /api/invoices/profiles/{profileId}
```

**Path Parameters**:
- `profileId` (required): Business profile identifier (UUID)

**Response: 200 OK**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "issuerName": "Acme Corporation",
  "issuerAddress": "123 Business Street, New York, NY 10001",
  "issuerEmail": "billing@acme.com",
  "issuerPhone": "+1 (555) 123-4567",
  "issuerRegistrationNumber": "ABC123456",
  "issuerTaxNumber": "TAX123456",
  "taxLabel": "Sales Tax",
  "defaultCurrencyCode": "USD",
  "invoicePrefix": "INV",
  "nextSequence": 1,
  "defaultTaxRate": 0.10,
  "defaultTerms": "Payment due within 30 days",
  "lineItemColumns": ["Description", "Quantity", "UnitPrice", "Discount", "Tax", "Total"],
  "createdAtUtc": "2025-01-01T10:00:00Z",
  "updatedAtUtc": "2025-01-01T12:00:00Z"
}
```

**Error Responses**:
- `404 Not Found`: Profile not found
  ```json
  {
    "message": "Business profile not found."
  }
  ```
- `500 Internal Server Error`: Database retrieval error

---

### 14. Create Business Profile

Creates a new business profile. Issuer name is required.

```
POST /api/invoices/profiles
```

**Request Body**:
```json
{
  "issuerName": "Acme Corporation",
  "issuerAddress": "123 Business Street, New York, NY 10001",
  "issuerEmail": "billing@acme.com",
  "issuerPhone": "+1 (555) 123-4567",
  "issuerRegistrationNumber": "ABC123456",
  "issuerTaxNumber": "TAX123456",
  "taxLabel": "Sales Tax",
  "defaultCurrencyCode": "USD",
  "invoicePrefix": "INV",
  "defaultTaxRate": 0.10,
  "defaultTerms": "Payment due within 30 days"
}
```

**Response: 201 Created**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "issuerName": "Acme Corporation",
  "issuerEmail": "billing@acme.com",
  "defaultCurrencyCode": "USD",
  "createdAtUtc": "2025-01-01T10:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Validation error (missing required fields)
  ```json
  {
    "message": "Issuer name is required."
  }
  ```
- `500 Internal Server Error`: Database or creation error

---

### 15. Update Business Profile

Updates an existing business profile. Issuer name is required.

```
PUT /api/invoices/profiles/{profileId}
```

**Path Parameters**:
- `profileId` (required): Business profile identifier (UUID)

**Request Body**:
```json
{
  "issuerName": "Acme Corporation Updated",
  "issuerAddress": "456 New Court, New York, NY 10001",
  "issuerEmail": "updated@acme.com",
  "issuerPhone": "+1 (555) 987-6543",
  "issuerRegistrationNumber": "ABC123456",
  "issuerTaxNumber": "TAX123456",
  "taxLabel": "GST",
  "defaultCurrencyCode": "USD",
  "invoicePrefix": "INV",
  "defaultTaxRate": 0.05,
  "defaultTerms": "Payment due within 45 days"
}
```

**Response: 200 OK**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "issuerName": "Acme Corporation Updated",
  "issuerEmail": "updated@acme.com",
  "defaultCurrencyCode": "USD",
  "updatedAtUtc": "2025-01-01T14:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Validation error
  ```json
  {
    "message": "Issuer name is required."
  }
  ```
- `404 Not Found`: Profile not found
  ```json
  {
    "message": "Business profile not found."
  }
  ```
- `500 Internal Server Error`: Database or update error

---

### 16. Delete Business Profile

Deletes a business profile. A profile can only be deleted if it has no associated invoices.

```
DELETE /api/invoices/profiles/{profileId}
```

**Path Parameters**:
- `profileId` (required): Business profile identifier (UUID)

**Response: 204 No Content**

No response body on success.

**Error Responses**:
- `400 Bad Request`: Profile cannot be deleted because it has associated invoices
  ```json
  {
    "message": "Cannot delete this profile because it has 5 associated invoice(s). Please delete or reassign the invoices first."
  }
  ```
- `404 Not Found`: Profile not found
  ```json
  {
    "message": "Business profile not found."
  }
  ```
- `500 Internal Server Error`: Database or deletion error

---

## Common Error Responses

### 400 Bad Request
```json
{
  "message": "Descriptive error message"
}
```

### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while processing your request"
}
```

---

## Data Models

### Invoice Status Enum
| Value | Name |
|-------|------|
| 0 | Draft |
| 1 | Generated |

### Template Source Type
| Value | Description |
|-------|-------------|
| Pdf | PDF template |
| Image | Image template (PNG/JPG) |

### Line Item Object
```json
{
  "description": "string",
  "total": "number",
  "otherColumns": "string (format: \"columnName: value, columnName: value\")"
}
```

### Business Profile
Acts as the customer profile and default settings source for invoices linked to that profile.

---

## Best Practices

1. **Always provide required fields** - A valid `businessProfileId` and at least one line item are mandatory for draft creation
2. **Use UTC timestamps** - All timestamps are in UTC timezone
3. **Handle pagination** - Currently list endpoints return all records; implement pagination on large datasets
4. **Idempotency** - POST endpoints are idempotent; retrying with same data creates duplicates
5. **File uploads** - Templates support PDF and image formats; max file size depends on storage backend

---

## Rate Limiting & Authentication

- All endpoints require authentication (to be implemented)
- Rate limiting applies to file upload endpoints
- Contact API administrator for rate limit details

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-01-15 | Initial API specification |
