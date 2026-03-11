# WASP Asset Cloud API — C# Implementation Plan

## Problem Statement
The current project (`DownloadAssets`) is a single-purpose .NET 10.0 console app that only calls one endpoint (`POST public-api/assets/streamgridrequestcsvflat`). We need to build a comprehensive C# client library with controllers and models covering all ~100+ endpoints across 19 API sections documented in `files/wasp-api-reference.md`.

## Approach
Build a structured C# class library alongside the existing console app. Organize code by API section into controllers (service clients), shared models, and a common HTTP infrastructure layer.

## Architecture

```
DownloadAssets/                    ← existing console app (untouched)
WaspApiClient/                     ← NEW class library project
├── WaspApiClient.csproj
├── WaspHttpClient.cs              ← Base HTTP client (auth, serialization, error handling)
├── Models/
│   ├── Common/
│   │   ├── WaspResult.cs           ← WaspResult<T> envelope
│   │   ├── WtResult.cs             ← Message/error object
│   │   ├── AdvancedSearchParameters.cs
│   │   ├── TopLevelFilterType.cs
│   │   ├── DcfValueInfo.cs         ← Custom fields
│   │   ├── NoteInfo.cs
│   │   ├── AddressInfo.cs
│   │   ├── PhoneInfo.cs
│   │   └── RecordStatus.cs
│   ├── Assets/
│   │   ├── AssetInfo.cs
│   │   ├── InventoriedAssetInfo.cs
│   │   ├── AssetCheckOutStatus.cs
│   │   └── GridStreamRequestModel.cs
│   ├── AssetTypes/
│   │   └── AssetTypeInfo.cs
│   ├── Attachments/
│   │   └── AttachmentMetadata.cs
│   ├── Contracts/
│   │   └── ContractInfo.cs
│   ├── Customers/
│   │   └── CustomerInfo.cs
│   ├── Departments/
│   │   └── DepartmentInfo.cs
│   ├── Employees/
│   │   ├── EmployeeInfo.cs
│   │   └── RfScanModel.cs
│   ├── Funding/
│   │   └── FundingInfo.cs
│   ├── Locations/
│   │   └── LocationModelInfo.cs
│   ├── Manufacturers/
│   │   └── ManufacturerInfo.cs
│   ├── PurchaseOrders/
│   │   ├── AssetPurchaseOrderInfo.cs
│   │   └── AssetPurchaseOrderLineInfo.cs
│   ├── Sites/
│   │   └── SiteInfo.cs
│   ├── Transactions/
│   │   ├── AssetCheckInModel.cs
│   │   ├── AssetCheckOutModel.cs
│   │   ├── AssetMoveModel.cs
│   │   ├── AssetDisposeModel.cs
│   │   ├── AssetTransactionSearch.cs
│   │   └── AssetTransactionModel.cs
│   └── Vendors/
│       └── VendorInfo.cs
├── Controllers/
│   ├── AddressController.cs        ← 4 active endpoints (customer/employee/manufacturer/supplier save)
│   ├── AssetController.cs          ← 15 endpoints (CRUD, search, CSV streams)
│   ├── AssetTypeController.cs      ← 7 endpoints
│   ├── AttachmentController.cs     ← 5 active endpoints (upload, download, metadata)
│   ├── ContractController.cs       ← 9 endpoints
│   ├── CustomerController.cs       ← 7 active endpoints (skip LDAP sync)
│   ├── DepartmentController.cs     ← 4 endpoints
│   ├── EmployeeController.cs       ← 10 active endpoints (skip LDAP sync)
│   ├── FundingController.cs        ← 10 endpoints
│   ├── LocationController.cs       ← 7 endpoints
│   ├── ManufacturerController.cs   ← 2 endpoints
│   ├── PhoneController.cs          ← 4 endpoints
│   ├── PurchaseOrderController.cs  ← 7 active endpoints
│   ├── SiteController.cs           ← 7 active endpoints
│   ├── SystemInfoController.cs     ← 1 endpoint
│   ├── TransactionController.cs    ← 15 active endpoints
│   └── VendorController.cs         ← 5 endpoints
└── Extensions/
    └── WaspResultExtensions.cs     ← Helper methods (EnsureSuccess, paging helpers)
```

## Todos (Implementation Order)

### Phase 1 — Foundation
1. **create-project** — Create `WaspApiClient.csproj` class library targeting net10.0
2. **common-models** — Build all shared models: `WaspResult<T>`, `WtResult`, `AdvancedSearchParameters`, `TopLevelFilterType`, `DcfValueInfo`, `NoteInfo`, `AddressInfo`, `PhoneInfo`, `RecordStatus`
3. **wasp-http-client** — Build `WaspHttpClient` base class with bearer token auth, JSON serialization (System.Text.Json), POST/GET helpers, stream support for CSV endpoints, error handling

### Phase 2 — Entity Models
4. **asset-models** — `AssetInfo`, `InventoriedAssetInfo`, `AssetCheckOutStatus`, `GridStreamRequestModel`
5. **supporting-entity-models** — `AssetTypeInfo`, `ContractInfo`, `CustomerInfo`, `DepartmentInfo`, `EmployeeInfo`, `RfScanModel`, `FundingInfo`, `LocationModelInfo`, `ManufacturerInfo`, `SiteInfo`, `VendorInfo`, `AttachmentMetadata`
6. **po-models** — `AssetPurchaseOrderInfo`, `AssetPurchaseOrderLineInfo`
7. **transaction-models** — `AssetCheckInModel`, `AssetCheckOutModel`, `AssetMoveModel`, `AssetDisposeModel`, `AssetTransactionSearch`, `AssetTransactionModel`

### Phase 3 — Controllers
8. **asset-controller** — 15 methods: createFixedAssets, updateFixedAssets, partialUpdate, createMQA, updateMQA, addQuantity, getByTags, getByTagsLight, infoSearch, advancedInfoSearch, checkoutStatus, ldapSync, streamCSVFlat, streamCSVUnique, streamCSVMQA
9. **transaction-controller** — 15 methods: checkIn, checkOut, move, dispose, getDisposeReasons, history, auditCount/v2, reconcile/v2, rfScan, streamCSV, streamArchiveCSV, historyGrid, historyNotes, historyGridV2
10. **entity-controllers-batch1** — AddressController (4), PhoneController (4), DepartmentController (4), ManufacturerController (2), SystemInfoController (1)
11. **entity-controllers-batch2** — CustomerController (7), EmployeeController (10), VendorController (5)
12. **entity-controllers-batch3** — ContractController (9), FundingController (10), PurchaseOrderController (7)
13. **entity-controllers-batch4** — AssetTypeController (7), AttachmentController (5), LocationController (7), SiteController (7)

### Phase 4 — Polish
14. **extensions-helpers** — `WaspResultExtensions` (EnsureSuccess, auto-paging helper, batch chunking for 500-record limit)
15. **build-verify** — Build solution, fix any compilation errors

## Key Design Decisions
- **Skip obsolete/deprecated endpoints** — 6 endpoints marked obsolete will not be implemented
- **Skip LDAP sync endpoints** — These are destructive (delete records not in list); include stubs with warnings only
- **Bearer token auth** — Match existing project pattern (read from token.txt or accept via constructor)
- **System.Text.Json** — Consistent with existing project; no Newtonsoft dependency
- **Async-first** — All controller methods return `Task<WaspResult<T>>`
- **500-record batching** — Controllers auto-chunk lists > 500 items where applicable
- **CSV streaming** — Return `Stream` for CSV endpoints, consistent with existing `streamgridrequestcsvflat` usage
