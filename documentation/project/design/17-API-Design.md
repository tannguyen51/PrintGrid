# API Design - PrintGrid Platform

## Overview
PrintGrid exposes a RESTful API built with ASP.NET Core 8.0 Web API.

**Base URL:** `https://api.printgrid.com/api/v1`  
**Authentication:** JWT Bearer Token  
**Content-Type:** `application/json`  
**API Documentation:** Swagger/OpenAPI 3.0

---

## Authentication

### POST /auth/register
Register a new customer account.

**Request:**
```json
{
  "email": "customer@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe"
}
```

**Response:** `201 Created`
```json
{
  "id": "uuid",
  "email": "customer@example.com",
  "fullName": "John Doe",
  "createdAt": "2026-09-01T10:00:00Z"
}
```

**Errors:**
- `400 Bad Request`: Validation failed
- `409 Conflict`: Email already exists

---

### POST /auth/login
Authenticate and receive JWT token.

**Request:**
```json
{
  "email": "customer@example.com",
  "password": "SecurePass123!"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "uuid",
    "email": "customer@example.com",
    "fullName": "John Doe",
    "role": "CUSTOMER"
  }
}
```

**Errors:**
- `401 Unauthorized`: Invalid credentials

---

### POST /auth/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "new_access_token",
  "refreshToken": "new_refresh_token",
  "expiresIn": 3600
}
```

---

## Customer - Models

### POST /models/upload
Upload a 3D model file.

**Authorization:** Bearer Token (Customer role)  
**Content-Type:** `multipart/form-data`

**Request:**
```
file: [binary STL/OBJ/3MF file, max 50MB]
```

**Response:** `202 Accepted` (async processing)
```json
{
  "modelId": "uuid",
  "fileName": "part.stl",
  "status": "processing",
  "uploadedAt": "2026-09-01T10:00:00Z"
}
```

**Errors:**
- `400 Bad Request`: Invalid file format or size
- `413 Payload Too Large`: File exceeds 50MB

---

### GET /models/{modelId}
Get model details and validation status.

**Response:** `200 OK`
```json
{
  "id": "uuid",
  "fileName": "part.stl",
  "fileSize": 2048576,
  "status": "valid",
  "boundingBox": {
    "x": 100.5,
    "y": 100.0,
    "z": 50.2
  },
  "volume": 125000.5,
  "validationResult": {
    "isWatertight": true,
    "hasNonManifoldEdges": false,
    "feasibleInNetwork": true,
    "issues": []
  },
  "thumbnailUrl": "https://cdn.printgrid.com/thumbnails/uuid.jpg",
  "uploadedAt": "2026-09-01T10:00:00Z"
}
```

**Errors:**
- `404 Not Found`: Model not found

---

### GET /models
List customer's uploaded models.

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `sortBy` (string: "uploadedAt" | "fileName")
- `sortOrder` (string: "asc" | "desc")

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "uuid",
      "fileName": "part1.stl",
      "boundingBox": {"x": 100, "y": 100, "z": 50},
      "thumbnailUrl": "https://...",
      "uploadedAt": "2026-09-01T10:00:00Z"
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

---

### DELETE /models/{modelId}
Delete a model.

**Response:** `204 No Content`

**Errors:**
- `404 Not Found`: Model not found
- `409 Conflict`: Model is used in active orders

---

## Customer - Quotes

### POST /quotes
Request a quote (async).

**Request:**
```json
{
  "modelId": "uuid",
  "configuration": {
    "material": "PLA",
    "color": "Black",
    "qualityGrade": "Standard",
    "infillDensity": 30,
    "quantity": 1,
    "postProcessing": ["support_removal"]
  },
  "requiredDeliveryDate": "2026-09-15"
}
```

**Response:** `202 Accepted`
```json
{
  "quoteId": "uuid",
  "status": "processing",
  "estimatedCompletionTime": "2026-09-01T10:01:00Z"
}
```

**Errors:**
- `400 Bad Request`: Invalid configuration
- `404 Not Found`: Model not found

---

### GET /quotes/{quoteId}
Get quote details.

**Response:** `200 OK`
```json
{
  "id": "uuid",
  "status": "completed",
  "modelId": "uuid",
  "configuration": {
    "material": "PLA",
    "color": "Black",
    "qualityGrade": "Standard",
    "infillDensity": 30,
    "quantity": 1,
    "postProcessing": ["support_removal"]
  },
  "pricing": {
    "total": 47.50,
    "currency": "USD",
    "breakdown": {
      "material": 8.00,
      "printTime": 24.50,
      "postProcessing": 5.00,
      "inspection": 5.00,
      "shipping": 5.00
    }
  },
  "deliveryDate": "2026-09-08",
  "estimatedPrintTime": "3h 28min",
  "expiresAt": "2026-09-03T10:00:00Z",
  "createdAt": "2026-09-01T10:00:00Z"
}
```

**Status Values:**
- `processing`: Quote being generated
- `completed`: Quote ready
- `failed`: Quote generation failed
- `expired`: Quote expired

---

### GET /quotes
List customer's quotes.

**Query Parameters:**
- `status` (string, optional)
- `page`, `pageSize`, `sortBy`, `sortOrder`

**Response:** `200 OK` (paginated list)

---

## Customer - Orders

### POST /orders
Place an order from a quote.

**Request:**
```json
{
  "quoteId": "uuid",
  "deliveryAddress": {
    "street": "123 Main St",
    "city": "San Francisco",
    "state": "CA",
    "zipCode": "94102",
    "country": "USA"
  },
  "paymentMethod": {
    "type": "credit_card",
    "token": "stripe_payment_method_token"
  }
}
```

**Response:** `201 Created`
```json
{
  "orderId": "uuid",
  "orderNumber": "ORD-12345",
  "status": "confirmed",
  "totalPrice": 47.50,
  "deliveryDate": "2026-09-08",
  "createdAt": "2026-09-01T10:05:00Z"
}
```

**Errors:**
- `400 Bad Request`: Invalid request or quote expired
- `402 Payment Required`: Payment failed
- `404 Not Found`: Quote not found

---

### GET /orders/{orderId}
Get order details.

**Response:** `200 OK`
```json
{
  "id": "uuid",
  "orderNumber": "ORD-12345",
  "status": "in_production",
  "totalPrice": 47.50,
  "currency": "USD",
  "deliveryDate": "2026-09-08",
  "deliveryAddress": {
    "street": "123 Main St",
    "city": "San Francisco",
    "state": "CA",
    "zipCode": "94102",
    "country": "USA"
  },
  "items": [
    {
      "id": "uuid",
      "modelId": "uuid",
      "modelFileName": "part.stl",
      "configuration": {...},
      "quantity": 1,
      "unitPrice": 47.50
    }
  ],
  "tracking": {
    "currentStage": "in_production",
    "stages": [
      {"name": "confirmed", "completedAt": "2026-09-01T10:05:00Z"},
      {"name": "assigned", "completedAt": "2026-09-01T10:06:00Z"},
      {"name": "in_production", "current": true, "estimatedCompletion": "2h 30min"}
    ]
  },
  "createdAt": "2026-09-01T10:05:00Z"
}
```

**Status Values:**
- `payment_pending`, `confirmed`, `assigned`, `in_production`, `quality_inspection`, `packing`, `shipped`, `delivered`

---

### GET /orders
List customer's orders.

**Query Parameters:**
- `status` (string, optional)
- `page`, `pageSize`, `sortBy`, `sortOrder`

**Response:** `200 OK` (paginated list)

---

### POST /orders/{orderId}/reprint-request
Request a reprint for quality issues.

**Request:**
```json
{
  "reason": "dimensional_inaccuracy",
  "description": "Part dimensions off by 2mm",
  "photos": [
    "data:image/jpeg;base64,..."
  ]
}
```

**Response:** `201 Created`
```json
{
  "requestId": "uuid",
  "status": "under_review",
  "createdAt": "2026-09-01T10:00:00Z"
}
```

---

## Lab - Registration & Profile

### POST /labs/register
Register a new lab (requires admin approval).

**Request:**
```json
{
  "name": "TechLab Maker Space",
  "location": "San Francisco, CA",
  "transferTimeToHubMinutes": 120,
  "contactEmail": "contact@techlab.com",
  "contactPhone": "+1-555-0100",
  "businessLicense": "data:application/pdf;base64,..."
}
```

**Response:** `201 Created`
```json
{
  "labId": "uuid",
  "status": "pending_approval",
  "createdAt": "2026-09-01T10:00:00Z"
}
```

---

### GET /labs/{labId}
Get lab details.

**Response:** `200 OK`
```json
{
  "id": "uuid",
  "name": "TechLab Maker Space",
  "location": "San Francisco, CA",
  "transferTimeToHubMinutes": 120,
  "status": "active",
  "performanceScore": 0.87,
  "createdAt": "2026-08-15T10:00:00Z"
}
```

---

### PUT /labs/{labId}
Update lab details.

**Request:**
```json
{
  "transferTimeToHubMinutes": 150,
  "contactEmail": "newemail@techlab.com"
}
```

**Response:** `200 OK` (updated lab object)

---

## Lab - Machines

### POST /labs/{labId}/machines
Register a machine.

**Request:**
```json
{
  "name": "Prusa i3 MK3S #1",
  "brand": "Prusa",
  "model": "i3 MK3S",
  "technology": "FDM",
  "buildVolume": {
    "x": 250,
    "y": 210,
    "z": 210
  },
  "layerHeightRange": {
    "min": 0.05,
    "max": 0.35
  },
  "achievableToleranceMm": 0.2
}
```

**Response:** `201 Created`
```json
{
  "machineId": "uuid",
  "name": "Prusa i3 MK3S #1",
  "status": "operational",
  "createdAt": "2026-09-01T10:00:00Z"
}
```

---

### GET /labs/{labId}/machines
List lab's machines.

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "uuid",
      "name": "Prusa i3 MK3S #1",
      "technology": "FDM",
      "buildVolume": {"x": 250, "y": 210, "z": 210},
      "status": "operational"
    }
  ]
}
```

---

### PUT /labs/{labId}/machines/{machineId}/status
Update machine status.

**Request:**
```json
{
  "status": "maintenance",
  "maintenanceUntil": "2026-09-05T18:00:00Z",
  "reason": "Scheduled maintenance"
}
```

**Response:** `200 OK`

---

## Lab - Material Inventory

### POST /labs/{labId}/inventory
Add or update material inventory.

**Request:**
```json
{
  "material": "PLA",
  "color": "Black",
  "quantityGrams": 5000,
  "reorderPointGrams": 500,
  "unitCost": 25.00
}
```

**Response:** `201 Created` or `200 OK` (if updating)

---

### GET /labs/{labId}/inventory
List lab's material inventory.

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "uuid",
      "material": "PLA",
      "color": "Black",
      "quantityGrams": 5000,
      "reorderPointGrams": 500,
      "lowStock": false
    }
  ]
}
```

---

## Lab - Job Management

### GET /labs/{labId}/jobs/assigned
Get jobs assigned and awaiting acceptance.

**Response:** `200 OK`
```json
{
  "items": [
    {
      "jobId": "uuid",
      "machineId": "uuid",
      "machineName": "Prusa i3 MK3S #1",
      "modelPreviewUrl": "https://...",
      "configuration": {...},
      "estimatedDuration": "3h 28min",
      "materialRequired": {"type": "PLA", "color": "Black", "grams": 25},
      "internalDueDate": "2026-09-04T14:00:00Z",
      "assignedAt": "2026-09-01T10:00:00Z",
      "responseDeadline": "2026-09-01T12:00:00Z"
    }
  ]
}
```

---

### POST /labs/{labId}/jobs/{jobId}/accept
Accept a job assignment.

**Response:** `200 OK`
```json
{
  "jobId": "uuid",
  "status": "accepted",
  "modelFileUrl": "https://minio.printgrid.com/models/uuid.stl?expires=...",
  "urlExpiresAt": "2026-09-04T18:00:00Z"
}
```

---

### POST /labs/{labId}/jobs/{jobId}/reject
Reject a job assignment.

**Request:**
```json
{
  "reason": "material_out_of_stock",
  "notes": "PLA Black unexpectedly out of stock"
}
```

**Response:** `200 OK`

---

### GET /labs/{labId}/jobs/queue
Get machine production queues.

**Query Parameters:**
- `machineId` (uuid, optional): Filter by machine

**Response:** `200 OK`
```json
{
  "machines": [
    {
      "machineId": "uuid",
      "machineName": "Prusa i3 MK3S #1",
      "queue": [
        {
          "jobId": "uuid",
          "status": "in_progress",
          "priority": "normal",
          "estimatedDuration": "3h 28min",
          "progress": 45,
          "internalDueDate": "2026-09-04T14:00:00Z"
        },
        {
          "jobId": "uuid2",
          "status": "accepted",
          "priority": "urgent",
          "estimatedDuration": "2h 15min",
          "internalDueDate": "2026-09-04T10:00:00Z"
        }
      ],
      "totalQueueDuration": "5h 43min"
    }
  ]
}
```

---

### POST /labs/{labId}/jobs/{jobId}/start
Mark job as started.

**Response:** `200 OK`
```json
{
  "jobId": "uuid",
  "status": "in_progress",
  "startedAt": "2026-09-01T14:00:00Z"
}
```

---

### POST /labs/{labId}/jobs/{jobId}/complete
Mark job as completed.

**Request:**
```json
{
  "actualDurationSeconds": 12450,
  "actualMaterialGrams": 28.5,
  "notes": "Print completed successfully"
}
```

**Response:** `200 OK`
```json
{
  "jobId": "uuid",
  "status": "completed",
  "completedAt": "2026-09-01T17:30:00Z"
}
```

---

### POST /labs/{labId}/jobs/{jobId}/report-failure
Report print failure.

**Request:**
```json
{
  "reason": "layer_separation",
  "percentComplete": 78,
  "description": "Layer separation at 78% completion",
  "photos": [
    "data:image/jpeg;base64,..."
  ]
}
```

**Response:** `200 OK`
```json
{
  "jobId": "uuid",
  "status": "failed",
  "incidentId": "uuid",
  "reprintJobCreated": true,
  "reprintJobId": "uuid"
}
```

---

## Lab - Performance

### GET /labs/{labId}/performance
Get lab performance metrics.

**Query Parameters:**
- `period` (string: "30d" | "90d" | "1y", default: "90d")

**Response:** `200 OK`
```json
{
  "labId": "uuid",
  "period": "90d",
  "performanceScore": 0.87,
  "standing": "excellent",
  "metrics": {
    "onTimeDeliveryRate": 0.92,
    "firstPassYield": 0.95,
    "acceptanceRate": 0.96,
    "averageUtilization": 0.72,
    "totalJobsCompleted": 145
  },
  "trends": {
    "onTimeDelivery": [
      {"date": "2026-08-01", "value": 0.90},
      {"date": "2026-08-08", "value": 0.92}
    ]
  },
  "networkComparison": {
    "onTimeDeliveryRate": {"lab": 0.92, "networkAvg": 0.88},
    "firstPassYield": {"lab": 0.95, "networkAvg": 0.90}
  },
  "revenueThisMonth": 2340.50
}
```

---

## Hub - Quality Control

### GET /hub/inspections/queue
Get inspection queue.

**Authorization:** Hub QC role

**Response:** `200 OK`
```json
{
  "items": [
    {
      "jobId": "uuid",
      "orderNumber": "ORD-12345",
      "labName": "TechLab",
      "qualityGrade": "Standard",
      "receivedAt": "2026-09-01T16:00:00Z",
      "priorityLevel": "normal"
    }
  ]
}
```

---

### POST /hub/inspections
Submit inspection result.

**Request:**
```json
{
  "jobId": "uuid",
  "overallResult": "fail",
  "checklistResults": [
    {"check": "dimensional_accuracy", "result": "fail", "notes": "±1.2mm, required ±0.5mm"},
    {"check": "surface_quality", "result": "pass"},
    {"check": "correct_material", "result": "pass"}
  ],
  "photos": [
    "data:image/jpeg;base64,..."
  ],
  "defectClassification": "dimensional_inaccuracy",
  "faultAttribution": "lab",
  "notes": "Part dimensions exceed tolerance"
}
```

**Response:** `201 Created`
```json
{
  "inspectionId": "uuid",
  "jobId": "uuid",
  "result": "fail",
  "reprintCreated": true,
  "reprintJobId": "uuid",
  "inspectedAt": "2026-09-02T10:00:00Z"
}
```

---

## Scheduling (Internal APIs)

### POST /scheduling/assign
Assign job to lab/machine (internal, triggered by order confirmation).

**Authorization:** System or Operations Manager

**Request:**
```json
{
  "jobId": "uuid",
  "deliveryDate": "2026-09-08"
}
```

**Response:** `200 OK`
```json
{
  "jobId": "uuid",
  "assignedLabId": "uuid",
  "assignedMachineId": "uuid",
  "internalDueDate": "2026-09-04T14:00:00Z",
  "assignmentScores": {
    "deadlineSlack": 0.85,
    "currentLoad": 0.70,
    "qualityHistory": 0.87,
    "transferTime": 0.90,
    "internalCost": 0.60,
    "composite": 0.78
  },
  "assignedAt": "2026-09-01T10:06:00Z"
}
```

---

### POST /scheduling/reschedule
Trigger rescheduling (internal, event-driven).

**Request:**
```json
{
  "eventType": "print_failure",
  "affectedJobId": "uuid",
  "reason": "Print failed at 78%"
}
```

**Response:** `200 OK`
```json
{
  "reschedulingId": "uuid",
  "affectedJobs": ["uuid1", "uuid2"],
  "rescheduledJobCount": 2,
  "reprintJobCreated": true,
  "completedInSeconds": 12.5,
  "triggeredAt": "2026-09-01T17:35:00Z"
}
```

---

## Analytics & Operations

### GET /analytics/network
Get network-wide analytics.

**Authorization:** Operations Manager or Admin

**Response:** `200 OK`
```json
{
  "snapshot": {
    "totalLabs": 23,
    "activeLabs": 21,
    "totalMachines": 87,
    "operationalMachines": 82,
    "activeOrders": 47,
    "jobsAtRisk": 3,
    "networkLoadPercent": 68
  },
  "sla": {
    "onTimeDeliveryRate": 0.94,
    "qualityPassRate": 0.96,
    "targetOnTime": 0.95,
    "targetQuality": 0.95
  }
}
```

---

### GET /analytics/labs/{labId}/details
Get detailed lab analytics.

**Response:** `200 OK` (comprehensive lab metrics)

---

### POST /operations/labs/{labId}/suspend
Suspend a lab (manual intervention).

**Authorization:** Operations Manager

**Request:**
```json
{
  "reason": "Sustained poor performance",
  "duration": "indefinite"
}
```

**Response:** `200 OK`

---

## Admin - Configuration

### GET /admin/config/{category}
Get configuration.

**Path Parameters:**
- `category`: "pricing" | "assignment_weights" | "scheduling_limits" | "sla_thresholds"

**Response:** `200 OK`
```json
{
  "category": "pricing",
  "version": 5,
  "values": {
    "pla_rate_per_gram": 0.32,
    "petg_rate_per_gram": 0.38,
    "machine_time_fdm_per_hour": 7.00
  },
  "updatedBy": "admin@printgrid.com",
  "updatedAt": "2026-09-01T10:00:00Z"
}
```

---

### PUT /admin/config/{category}
Update configuration (creates new version).

**Request:**
```json
{
  "values": {
    "pla_rate_per_gram": 0.35,
    "petg_rate_per_gram": 0.40
  }
}
```

**Response:** `200 OK`
```json
{
  "category": "pricing",
  "version": 6,
  "values": {...},
  "updatedAt": "2026-09-01T12:00:00Z"
}
```

---

## WebSocket (SignalR)

### Connection
```
wss://api.printgrid.com/hubs/notifications
```

**Authentication:** Pass JWT token in query string
```
wss://api.printgrid.com/hubs/notifications?access_token=jwt_token
```

---

### Server → Client Events

**QuoteReady**
```json
{
  "quoteId": "uuid",
  "status": "completed"
}
```

**OrderStatusChanged**
```json
{
  "orderId": "uuid",
  "newStatus": "in_production",
  "timestamp": "2026-09-01T14:00:00Z"
}
```

**JobAssigned** (Lab)
```json
{
  "jobId": "uuid",
  "machineId": "uuid",
  "responseDeadline": "2026-09-01T12:00:00Z"
}
```

---

## Error Response Format

All errors follow consistent format:

```json
{
  "error": {
    "code": "QUOTE_EXPIRED",
    "message": "Quote has expired",
    "details": {
      "quoteId": "uuid",
      "expiredAt": "2026-09-03T10:00:00Z"
    },
    "timestamp": "2026-09-03T11:00:00Z"
  }
}
```

**Common Error Codes:**
- `VALIDATION_ERROR`: Request validation failed
- `NOT_FOUND`: Resource not found
- `UNAUTHORIZED`: Authentication required
- `FORBIDDEN`: Insufficient permissions
- `CONFLICT`: Business rule conflict
- `QUOTE_EXPIRED`: Quote expired
- `PAYMENT_FAILED`: Payment processing failed
- `INSUFFICIENT_CAPACITY`: No lab can fulfill order

---

## Rate Limiting

**Headers:**
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1630512000
```

**Limits:**
- Authenticated: 1000 requests/hour
- Unauthenticated: 100 requests/hour

---

## API Versioning

**URL Versioning:** `/api/v1/...`

**Deprecation Header:**
```
Deprecation: Sun, 01 Mar 2027 00:00:00 GMT
Link: </api/v2/resource>; rel="successor-version"
```

---

## Summary

**Total Endpoints:** ~60

| Category | Endpoints |
|----------|-----------|
| Authentication | 3 |
| Customer - Models | 4 |
| Customer - Quotes | 3 |
| Customer - Orders | 4 |
| Lab - Profile | 3 |
| Lab - Machines | 4 |
| Lab - Inventory | 2 |
| Lab - Jobs | 8 |
| Lab - Performance | 1 |
| Hub - Quality Control | 2 |
| Scheduling (Internal) | 2 |
| Analytics & Operations | 3 |
| Admin - Configuration | 2 |
| WebSocket Events | 3 |

**API Features:**
- ✅ RESTful design
- ✅ JWT authentication
- ✅ Role-based authorization
- ✅ Async processing (quotes, uploads)
- ✅ Real-time updates (SignalR)
- ✅ Pagination
- ✅ Rate limiting
- ✅ Error handling
- ✅ API versioning
- ✅ OpenAPI/Swagger documentation
