# WASP Asset Cloud — Backend Façade + Client API Spec

> **Audience:** Frontend engineers calling Outermind WASP routes, plus backend engineers maintaining the WASP integration  
> **Backend façade routes:** `Outermind.Web\Controllers\Wasp\`  
> **Internal client services:** `Outermind.Web\Wasp\`  
> **Shared DTOs:** `Outermind\Wasp\Models\`  
> **Last updated:** March 2026

---

## Table of Contents

1. [Setup & Configuration](#1-setup--configuration)
2. [Response Envelope — `WaspResult<T>`](#2-response-envelope--waspresultt)
3. [Searching & Paging — `AdvancedSearchParameters`](#3-searching--paging--advancedsearchparameters)
4. [Common Models](#4-common-models)
5. [Assets](#5-assets)
6. [Asset Types](#6-asset-types)
7. [Transactions](#7-transactions)
8. [Employees](#8-employees)
9. [Customers](#9-customers)
10. [Vendors](#10-vendors)
11. [Sites](#11-sites)
12. [Locations](#12-locations)
13. [Departments](#13-departments)
14. [Manufacturers](#14-manufacturers)
15. [Contracts](#15-contracts)
16. [Funding](#16-funding)
17. [Purchase Orders](#17-purchase-orders)
18. [Attachments](#18-attachments)
19. [Addresses & Phones](#19-addresses--phones)
20. [System Info](#20-system-info)
21. [Helpers & Extensions](#21-helpers--extensions)
22. [Error Handling](#22-error-handling)
23. [Batch Limits](#23-batch-limits)

---

## 1. Setup & Configuration

### Frontend contract

The frontend should call the **Outermind backend façade routes**, not the external WASP host directly.

For the current Quasar WASP Explorer, the implemented first-pass routes are:

| Method | Route | Returns | Backed by |
|--------|-------|---------|-----------|
| `POST` | `/public-api/assets/assetinfosearch` | `WaspResult<List<AssetInfo>>` | `AssetController.InfoSearchAsync()` |
| `POST` | `/public-api/contracts/infosearch` | `WaspResult<List<ContractInfo>>` | `ContractController.InfoSearchAsync()` |
| `POST` | `/public-api/customers/advancedinfosearch` | `WaspResult<List<CustomerInfo>>` | `CustomerController.AdvancedSearchAsync()` |
| `POST` | `/public-api/departments/infosearch` | `WaspResult<List<DepartmentInfo>>` | `DepartmentController.InfoSearchAsync()` |
| `POST` | `/public-api/locations/infosearch` | `WaspResult<List<LocationModelInfo>>` | `LocationController.InfoSearchAsync()` |
| `POST` | `/public-api/sites/infosearch` | `WaspResult<List<SiteInfo>>` | `SiteController.InfoSearchAsync()` |
| `POST` | `/public-api/transactions/grid-query/transaction-history-v2` | `WaspResult<List<AssetTransactionModel>>` | `TransactionController.HistoryGridV2Async()` |

These routes currently cover the read-only Explorer/search experience in the Quasar frontend.

For the `InfoSearch` routes, the frontend sends the raw search term as the request body JSON string, for example:

```json
"Hello world"
```

The Outermind backend then wraps that value before calling WASP:

```json
{
  "SearchPattern": "Hello world"
}
```

An empty frontend search string is forwarded as:

```json
{
  "SearchPattern": ""
}
```

### Backend registration

The backend registers the internal WASP client services during startup:

```csharp
services.AddWaspApi(configuration);
```

Current backend defaults:

- Base URL defaults to `https://thecrowleycompany.waspassetcloud.com`
- Bearer token is read from `token.txt` in the application directory
- Optional overrides still exist via `Wasp:BaseUrl` and `Wasp:Token` in `IConfiguration`

Example override configuration:

```json
{
  "Wasp": {
    "BaseUrl": "https://your-wasp-instance.com",
    "Token": "your-bearer-token"
  }
}
```

### Internal service usage

Backend code can still inject the typed WASP client services directly:

```csharp
public class MyService
{
    private readonly AssetController _assets;
    public MyService(AssetController assets) => _assets = assets;
}
```

### Available internal services

| Service | DI Type | Section |
|---------|---------|---------|
| `AssetController` | Typed HttpClient | [Assets](#5-assets) |
| `AssetTypeController` | Typed HttpClient | [Asset Types](#6-asset-types) |
| `TransactionController` | Typed HttpClient | [Transactions](#7-transactions) |
| `EmployeeController` | Typed HttpClient | [Employees](#8-employees) |
| `CustomerController` | Typed HttpClient | [Customers](#9-customers) |
| `VendorController` | Typed HttpClient | [Vendors](#10-vendors) |
| `SiteController` | Typed HttpClient | [Sites](#11-sites) |
| `LocationController` | Typed HttpClient | [Locations](#12-locations) |
| `DepartmentController` | Typed HttpClient | [Departments](#13-departments) |
| `ManufacturerController` | Typed HttpClient | [Manufacturers](#14-manufacturers) |
| `ContractController` | Typed HttpClient | [Contracts](#15-contracts) |
| `FundingController` | Typed HttpClient | [Funding](#16-funding) |
| `PurchaseOrderController` | Typed HttpClient | [Purchase Orders](#17-purchase-orders) |
| `AttachmentController` | Typed HttpClient | [Attachments](#18-attachments) |
| `AddressController` | Typed HttpClient | [Addresses & Phones](#19-addresses--phones) |
| `PhoneController` | Typed HttpClient | [Addresses & Phones](#19-addresses--phones) |
| `SystemInfoController` | Typed HttpClient | [System Info](#20-system-info) |

---

## 2. Response Envelope — `WaspResult<T>`

**Every** call returns `WaspResult<T>`. Always check `HasError` before using `Data`.

```csharp
public class WaspResult<T>
{
    T Data                                  // The payload
    List<WtResult> Messages                 // Error/info messages
    int? BatchNumber                        // Grouped transaction ID (may be null)
    bool HasError                           // true if any error occurred
    bool HasHttpError                       // true if HTTP-layer failure
    bool HasMessage                         // true if any message exists
    bool HasSuccessWithMoreDataRemaining    // true when paging — more pages available
    long TotalRecordsLongCount              // Total matching records (for paging)
}
```

### `WtResult` (individual message)

```csharp
public class WtResult
{
    int ResultCode      // 0 = success
    string Message      // Human-readable message
    int HttpStatusCode  // HTTP status code
    string FieldName    // Which field caused the error (if applicable)
}
```

### Checking for errors

```csharp
var result = await assets.GetByTagsAsync(new[] { "LAPTOP-001" });

if (result.HasError)
{
    foreach (var msg in result.Messages)
        Console.WriteLine($"Error on {msg.FieldName}: {msg.Message}");
    return;
}

// Safe to use result.Data
```

Or use the extension:

```csharp
var result = (await assets.GetByTagsAsync(tags)).EnsureSuccess();
// Throws WaspApiException if HasError is true
```

---

## 3. Searching & Paging — `AdvancedSearchParameters`

Most `AdvancedSearch` / `Search` methods accept this object.

```csharp
public class AdvancedSearchParameters
{
    int PageSize = 100                          // Records per page (recommended 100–500)
    int PageNumber = 1                          // 1-based page number
    long? TotalCountFromPriorFetch              // Pass TotalRecordsLongCount from prior call to skip re-count
    int AdditionalSkipCount                     // Extra records to skip
    List<SortDescriptor> Sort                   // Sort criteria
    TopLevelFilterType Filter                   // Filter expression tree
    string ClientUtcOffset                      // Timezone ID (e.g. "Eastern Standard Time")
    int? FilterBehavior                         // Internal filter flags
    bool IgnoreAttachments                      // Skip attachment data (performance)
    bool IgnoreGeoLocation                      // Skip geo data (performance)
    string WorkingSiteIdCsvList                 // Comma-separated site IDs to narrow results
}
```

### Sorting

```csharp
var search = new AdvancedSearchParameters
{
    PageSize = 100,
    PageNumber = 1,
    Sort = new List<SortDescriptor>
    {
        new() { Field = "AssetTag", Dir = "asc" }
    }
};
```

### Filtering

Filters use an expression tree. Operators: `eq`, `neq`, `contains`, `startswith`, `endswith`, `gt`, `gte`, `lt`, `lte`.

```csharp
// Simple: AssetTypeNumber == "LAPTOP"
var search = new AdvancedSearchParameters
{
    Filter = new TopLevelFilterType
    {
        Field = "AssetTypeNumber",
        Operator = "eq",
        Value = "LAPTOP"
    }
};

// Compound: AssetTypeNumber == "LAPTOP" AND SiteName == "Main Campus"
var search = new AdvancedSearchParameters
{
    Filter = new TopLevelFilterType
    {
        Logic = "and",
        Filters = new List<TopLevelFilterType>
        {
            new() { Field = "AssetTypeNumber", Operator = "eq", Value = "LAPTOP" },
            new() { Field = "SiteName", Operator = "eq", Value = "Main Campus" }
        }
    }
};
```

### Paging through all results

```csharp
// Manual paging
int page = 1;
long? total = null;
var all = new List<AssetInfo>();

do
{
    var result = await assets.AdvancedSearchAsync(new AdvancedSearchParameters
    {
        PageSize = 100,
        PageNumber = page++,
        TotalCountFromPriorFetch = total
    });

    total = result.TotalRecordsLongCount;
    all.AddRange(result.Data);

} while (result.HasSuccessWithMoreDataRemaining);

// Or use the helper:
var allAssets = await WaspResultExtensions.GetAllPagesAsync(
    search => assets.AdvancedSearchAsync(search),
    pageSize: 100);
```

---

## 4. Common Models

### `DcfValueInfo` — Custom Fields

```csharp
public class DcfValueInfo
{
    int ImportRowNumber         // Row reference for error reporting
    string DcfLabel             // Custom field label name
    int DCFDataType             // 1 = text, (other types TBD)
    string DcfTextValue         // Value if text
    decimal? DcfNumberValue     // Value if number
    DateTime? DcfDateValue      // Value if date
    int DcfValueRecordStatus    // 0 = active
}
```

### `NoteInfo`

```csharp
public class NoteInfo
{
    string NoteText
    DateTime? CreatedDate
    string CreatedBy
}
```

### `AddressInfo`

```csharp
public class AddressInfo
{
    string CustomerNumber       // Set for customer addresses
    string EmployeeNumber       // Set for employee addresses
    string AddressType          // Must match existing Address Type in WASP
    string Address1
    string Address2
    string City
    string State
    string ZipCode
    string Country
}
```

### `PhoneInfo`

```csharp
public class PhoneInfo
{
    string CustomerNumber
    string EmployeeNumber
    string PhoneType            // Must match existing Phone Type in WASP
    string PhoneNumber
}
```

### `RecordStatus` (enum)

| Value | Meaning |
|-------|---------|
| `0` | Active |
| `1` | Disposed |
| `2` | Deleted |

---

## 5. Assets

**Service:** `AssetController`

### Models

#### `AssetInfo` — Fixed Asset (full detail)

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | Row reference for error correlation |
| `AssetTag` | `string` | **Required** — primary unique identifier |
| `AssetDescription` | `string` | |
| `AssetClassId` | `int?` | 1 = Fixed, 2 = Multi-Quantity |
| `AssetTypeNumber` | `string` | **Required** — asset type identifier |
| `AssetTypeDescription` | `string` | *Read-only* |
| `DepartmentCode` | `string` | |
| `DepartmentName` | `string` | |
| `SiteName` | `string` | **Required** |
| `SiteDescription` | `string` | *Read-only* |
| `LocationCode` | `string` | **Required** |
| `GroupTag` | `string` | Parent group tag |
| `IsGroup` | `bool` | Asset is a group container |
| `TransactAsWhole` | `bool` | |
| `AuditAsWhole` | `bool` | |
| `SupplierNumber` | `string` | |
| `SupplierContactEmail` | `string` | *Read-only* |
| `SupplierContactName` | `string` | *Read-only* |
| `SupplierEmail` | `string` | *Read-only* |
| `SupplierName` | `string` | *Read-only* |
| `SupplierWebsite` | `string` | *Read-only* |
| `AssetSerialNumber` | `string` | |
| `ConditionDescription` | `string` | Must match existing condition |
| `ManufacturerName` | `string` | |
| `AssetModelName` | `string` | |
| `CategoryDescription` | `string` | Must match existing category |
| `CheckoutLength` | `int?` | Checkout duration |
| `CheckoutLeadTime` | `int?` | Lead time |
| `AssetIsCheckedOut` | `bool` | *Read-only* |
| `AssetShouldDepreciate` | `bool` | |
| `AssetSalvageValue` | `decimal?` | |
| `AssetDepreciatedValue` | `decimal?` | *Read-only* |
| `BookValue` | `decimal?` | *Read-only* |
| `AssetDepreciationBeginDate` | `DateTime?` | |
| `AssetDefaultCost` | `decimal?` | Initial cost |
| `AssetAssignee` | `string` | |
| `AssetAssigneeNumber` | `string` | |
| `AssetRecordStatus` | `int` | 0 = active |
| `NewDefaultAttachment` | `string` | GUID of default attachment |
| `HasAttachment` | `bool` | *Read-only* |
| `AssetTransQuantity` | `decimal?` | |
| `OwnerName` | `string` | |
| `OwnerNumber` | `string` | |
| `OwnerFirstName` | `string` | |
| `OwnerLastName` | `string` | |
| `WarrantyProvider` | `string` | |
| `WarrantyBeginDate` | `DateTime?` | |
| `WarrantyEndDate` | `DateTime?` | |
| `AssetPdPoNumber` | `string` | Purchase order number |
| `AssetPdPurchaseDate` | `DateTime?` | |
| `AssetPdCost` | `decimal?` | |
| `Latitude` | `decimal?` | Geo-location |
| `Longitude` | `decimal?` | Geo-location |
| `Altitude` | `decimal?` | Geo-location |
| `TimeUTC` | `DateTime?` | Geo-location timestamp |
| `Accuracy` | `decimal?` | Geo-location accuracy |
| `CustomFields` | `List<DcfValueInfo>` | |
| `AttachmentsToAdd` | `List<string>` | GUIDs to associate |
| `AttachmentsToDelete` | `List<string>` | GUIDs to remove |
| `AttachmentNames` | `List<KeyValuePair<string, string>>` | GUID → FileName |
| `AssetDueDate` | `DateTime?` | |
| `AssetCreatedDate` | `DateTime?` | *Read-only* |
| `AssetLastUpdatedDate` | `DateTime?` | *Read-only* |

#### `InventoriedAssetInfo` — Multi-Quantity Asset

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `AssetTag` | `string` | **Required** |
| `AssetTypeNumber` | `string` | **Required** |
| `AssetTypeDescription` | `string` | |
| `AssetClass` | `int?` | *Read-only* |
| `DepreciationClassName` | `string` | Must match existing class |
| `ManufacturerName` | `string` | Must match existing |
| `CategoryDescription` | `string` | Must match existing |
| `SupplierNumber` | `string` | Must match existing |
| `AssetTypeModelNumber` | `string` | |
| `AssetTypeCheckOutDuration` | `int?` | Minutes |
| `AssetTypeLeadTime` | `int?` | Minutes |
| `AssetTypeAutoFillData` | `bool` | |
| `AssetDefaultCost` | `decimal?` | **Required** |
| `AssetTypeRecordStatus` | `int?` | *Read-only* |
| `AssetCount` | `int` | *Read-only* — number of assets of this type |
| `AttachmentsToAdd` | `List<string>` | |
| `AttachmentsToDelete` | `List<string>` | |
| `AttachmentNames` | `List<KeyValuePair<string, string>>` | |
| `CustomFields` | `List<DcfValueInfo>` | |
| `NewDefaultAttachment` | `string` | |
| `HasAttachment` | `bool` | *Read-only* |
| `IsGroup` | `bool` | |
| `TransactAsWhole` | `bool` | |
| `AssetRecordStatus` | `int` | *Read-only* |

#### `AssetCheckOutStatus`

| Property | Type |
|----------|------|
| `AssetTag` | `string` |
| `AssetDescription` | `string` |
| `CheckedOutTo` | `string` |
| `CheckedOutToNumber` | `string` |
| `CheckOutDate` | `DateTime?` |
| `DueDate` | `DateTime?` |
| `SiteName` | `string` |
| `LocationCode` | `string` |

#### `GridStreamRequestModel` — for CSV streaming endpoints

```csharp
public class GridStreamRequestModel
{
    AdvancedSearchParameters GridRequest     // Filter/sort params
    List<string> FieldTitles                // Specify columns; null = all columns
}
```

> ⚠️ Always provide `FieldTitles` — column names/order may change between WASP versions if left null.

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateFixedAssetsAsync** | `(IReadOnlyList<AssetInfo>) → WaspResult<List<AssetInfo>>` | Create new fixed assets |
| **UpdateFixedAssetsAsync** | `(IReadOnlyList<AssetInfo>) → WaspResult<List<AssetInfo>>` | Update existing fixed assets by AssetTag |
| **PartialUpdateFixedAssetsAsync** | `(IReadOnlyList<AssetInfo>) → WaspResult<List<AssetInfo>>` | Partial update — only populate fields to change |
| **CreateMultiQuantityAssetsAsync** | `(IReadOnlyList<InventoriedAssetInfo>) → WaspResult<List<InventoriedAssetInfo>>` | Create new MQA assets |
| **UpdateMultiQuantityAssetsAsync** | `(IReadOnlyList<InventoriedAssetInfo>) → WaspResult<List<InventoriedAssetInfo>>` | Update existing MQA assets |
| **AddAssetQuantityAsync** | `(IReadOnlyList<InventoriedAssetInfo>) → WaspResult<List<InventoriedAssetInfo>>` | Add quantity to MQA assets |
| **GetByTagsAsync** | `(IReadOnlyList<string>) → WaspResult<List<WaspResult<AssetInfo>>>` | Get full asset details by tags (nested results per asset) |
| **GetByTagsLightAsync** | `(IReadOnlyList<string>) → WaspResult<List<WaspResult<AssetInfo>>>` | Lightweight version of GetByTags |
| **InfoSearchAsync** | `(string) → WaspResult<List<AssetInfo>>` | Text search on tag/description |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<AssetInfo>>` | Paged search with filter/sort |
| **GetCheckoutStatusAsync** | `(string assetTag) → WaspResult<AssetCheckOutStatus>` | Check-out info for one asset |
| **StreamCsvFlatAsync** | `(GridStreamRequestModel) → Stream` | Stream all fixed inventory as CSV |
| **StreamCsvUniqueAsync** | `(GridStreamRequestModel) → Stream` | Stream unique fixed assets as CSV |
| **StreamCsvMqaAsync** | `(GridStreamRequestModel) → Stream` | Stream all MQA inventory as CSV |

### Usage Examples

```csharp
// Create a fixed asset
var result = await assets.CreateFixedAssetsAsync(new[]
{
    new AssetInfo
    {
        AssetTag = "LAPTOP-042",
        AssetTypeNumber = "LAPTOP",
        SiteName = "Main Campus",
        LocationCode = "ROOM-101",
        AssetDescription = "Dell Latitude 7440",
        AssetDefaultCost = 1299.99m
    }
});

// Search with filters
var result = await assets.AdvancedSearchAsync(new AdvancedSearchParameters
{
    PageSize = 50,
    PageNumber = 1,
    Sort = new() { new SortDescriptor { Field = "AssetTag", Dir = "asc" } },
    Filter = new TopLevelFilterType
    {
        Field = "SiteName",
        Operator = "eq",
        Value = "Main Campus"
    },
    IgnoreAttachments = true
});

// Get by tags — note the nested WaspResult per asset
var result = await assets.GetByTagsAsync(new[] { "LAPTOP-042", "PHONE-007" });
foreach (var wrapper in result.Data)
{
    if (!wrapper.HasError)
        Console.WriteLine(wrapper.Data.AssetDescription);
}

// Stream CSV export
using var csvStream = await assets.StreamCsvFlatAsync(new GridStreamRequestModel
{
    FieldTitles = new() { "AssetTag", "AssetDescription", "SiteName", "LocationCode" }
});
using var reader = new StreamReader(csvStream);
var csv = await reader.ReadToEndAsync();
```

---

## 6. Asset Types

**Service:** `AssetTypeController`

### Model — `AssetTypeInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `AssetTypeNumber` | `string` | Unique identifier |
| `AssetTypeDescription` | `string` | |
| `AssetClass` | `int?` | |
| `DepreciationClassName` | `string` | |
| `ManufacturerName` | `string` | |
| `CategoryDescription` | `string` | |
| `SupplierNumber` | `string` | |
| `AssetTypeModelNumber` | `string` | |
| `AssetTypeCheckOutDuration` | `int?` | Minutes |
| `AssetTypeLeadTime` | `int?` | Minutes |
| `AssetTypeAutoFillData` | `bool` | |
| `AssetDefaultCost` | `decimal?` | |
| `AssetTypeRecordStatus` | `int?` | |
| `CustomFields` | `List<DcfValueInfo>` | |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<AssetTypeInfo>) → WaspResult<List<AssetTypeInfo>>` | Create asset types |
| **UpdateAsync** | `(IReadOnlyList<AssetTypeInfo>) → WaspResult<List<AssetTypeInfo>>` | Update by type number |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<AssetTypeInfo>>` | Get by type numbers |
| **InfoSearchAsync** | `(string) → WaspResult<List<AssetTypeInfo>>` | Text search on number/description |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<AssetTypeInfo>>` | Paged search |
| **DeleteByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by type numbers |
| **GetAllSimpleAsync** | `() → WaspResult<List<AssetTypeInfo>>` | Get all types (simplified) |

---

## 7. Transactions

**Service:** `TransactionController`

### Models

#### `AssetCheckOutModel`

| Property | Type | Notes |
|----------|------|-------|
| `AssetTag` | `string` | **Required** |
| `FromSiteName` | `string` | MQA only |
| `FromLocationCode` | `string` | MQA only |
| `FromGroupTag` | `string` | MQA only |
| `ToSiteName` | `string` | New site after checkout |
| `ToLocationCode` | `string` | New location |
| `ToGroupTag` | `string` | New group |
| `DueDate` | `DateTime?` | Due-back date |
| `CheckOutDate` | `DateTime?` | Defaults to now |
| `VendorNumber` | `string` | One assignee required for MQA |
| `CustomerNumber` | `string` | One assignee required for MQA |
| `EmployeeNumber` | `string` | One assignee required for MQA |
| `Quantity` | `decimal?` | MQA only |
| `RecordSource` | `string` | Device identifier |
| `Note` | `string` | |
| `Condition` | `string` | Updates asset condition |

#### `AssetCheckInModel`

| Property | Type | Notes |
|----------|------|-------|
| `AssetTag` | `string` | **Required** |
| `FromSiteName` | `string` | MQA only |
| `FromLocationCode` | `string` | MQA only |
| `FromGroupTag` | `string` | MQA only |
| `ToSiteName` | `string` | Destination site |
| `ToLocationCode` | `string` | Destination location |
| `ToGroupTag` | `string` | Destination group |
| `VendorNumber` | `string` | Who is checking in (MQA) |
| `CustomerNumber` | `string` | Who is checking in (MQA) |
| `EmployeeNumber` | `string` | Who is checking in (MQA) |
| `CheckInDate` | `DateTime?` | Defaults to now |
| `Quantity` | `decimal?` | MQA only |
| `RecordSource` | `string` | |
| `Note` | `string` | |
| `Condition` | `string` | |

#### `AssetMoveModel`

| Property | Type | Notes |
|----------|------|-------|
| `AssetTag` | `string` | **Required** |
| `FromSiteName` | `string` | MQA only |
| `FromLocationCode` | `string` | MQA only |
| `FromGroupTag` | `string` | MQA only |
| `ToSiteName` | `string` | **Required** |
| `ToLocationCode` | `string` | |
| `ToGroupTag` | `string` | |
| `Quantity` | `decimal?` | MQA only |
| `RecordSource` | `string` | |
| `Note` | `string` | |
| `Condition` | `string` | |

#### `AssetDisposeModel`

```csharp
public class AssetDisposeModel
{
    string DisposeReason            // Required — must match existing reason
    DateTime? TransactionDate       // Defaults to now
    string RecordSource
    string Note
    List<AssetsToDispose> Assets     // Required
}

public class AssetsToDispose
{
    string AssetTag                 // Required
    string SiteName                 // MQA only
    string LocationCode             // MQA only
    string GroupTag                 // MQA only
    decimal? Quantity               // MQA only
}
```

#### `AssetTransactionSearch` — for history queries

```csharp
public class AssetTransactionSearch
{
    string AssetTag         // Required
    int PageNumber          // 0-based, default 0
    int PageSize = 20       // Records per page
    DateTime? StartDate     // Oldest transaction
    DateTime? EndDate       // Newest transaction
}
```

#### `AssetTransactionModel` — history result

| Property | Type |
|----------|------|
| `AssetTag` | `string` |
| `AssetDescription` | `string` |
| `TransactionType` | `string` |
| `TransactionDate` | `DateTime?` |
| `SiteName` | `string` |
| `LocationCode` | `string` |
| `AssigneeName` | `string` |
| `AssigneeNumber` | `string` |
| `Quantity` | `decimal?` |
| `Note` | `string` |
| `RecordSource` | `string` |
| `TransactionUniqueIdentifier` | `string` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **GetDisposeReasonsAsync** | `() → WaspResult<List<string>>` | List all active dispose reasons |
| **DisposeAsync** | `(AssetDisposeModel) → WaspResult<int>` | Dispose assets; returns count |
| **MoveAsync** | `(IReadOnlyList<AssetMoveModel>) → WaspResult<List<int>>` | Move assets; returns transaction IDs |
| **CheckOutAsync** | `(IReadOnlyList<AssetCheckOutModel>) → WaspResult<List<int>>` | Check out; returns transaction IDs |
| **CheckInAsync** | `(IReadOnlyList<AssetCheckInModel>) → WaspResult<List<int>>` | Check in; returns transaction IDs |
| **GetHistoryAsync** | `(AssetTransactionSearch) → WaspResult<List<AssetTransactionModel>>` | Paginated history for one asset |
| **AuditCountAsync** | `(object) → WaspResult<List<WtResult>>` | Audit count transaction |
| **AuditCountV2Async** | `(object) → WaspResult<List<WtResult>>` | V2 with Container support |
| **ReconcileAsync** | `(object) → WaspResult<List<WtResult>>` | Reconcile audit discrepancies |
| **ReconcileV2Async** | `(object) → WaspResult<List<WtResult>>` | V2 with Container support |
| **RfScanAsync** | `(object) → WaspResult<object>` | RF scan transaction |
| **StreamCsvAsync** | `(GridStreamRequestModel) → Stream` | Stream transaction history as CSV |
| **StreamArchiveCsvAsync** | `(GridStreamRequestModel) → Stream` | Stream archived history as CSV |
| **HistoryGridAsync** | `(object) → WaspResult<List<AssetTransactionModel>>` | Filtered transaction history |
| **HistoryNotesAsync** | `(object) → WaspResult<object>` | Get notes/attachments for transactions |
| **HistoryGridV2Async** | `(AdvancedSearchParameters) → WaspResult<List<AssetTransactionModel>>` | V2 with filter/sort/paging |

### Usage Examples

```csharp
// Check out a laptop to an employee
var result = await transactions.CheckOutAsync(new[]
{
    new AssetCheckOutModel
    {
        AssetTag = "LAPTOP-042",
        EmployeeNumber = "EMP-007",
        DueDate = DateTime.UtcNow.AddDays(30),
        Note = "Assigned for Q3 project"
    }
});
// result.Data = [12345] (transaction IDs)

// Check it back in
var result = await transactions.CheckInAsync(new[]
{
    new AssetCheckInModel
    {
        AssetTag = "LAPTOP-042",
        EmployeeNumber = "EMP-007",
        ToSiteName = "Main Campus",
        ToLocationCode = "STORAGE-01"
    }
});

// Move an asset
var result = await transactions.MoveAsync(new[]
{
    new AssetMoveModel
    {
        AssetTag = "LAPTOP-042",
        ToSiteName = "Campus B",
        ToLocationCode = "OFFICE-22",
        Note = "Relocated by IT"
    }
});

// Dispose assets
var result = await transactions.DisposeAsync(new AssetDisposeModel
{
    DisposeReason = "End of Life",
    Note = "Scheduled disposal",
    Assets = new()
    {
        new AssetsToDispose { AssetTag = "LAPTOP-001" },
        new AssetsToDispose { AssetTag = "PHONE-002", SiteName = "Main", LocationCode = "WH-1", Quantity = 5.0m }
    }
});

// Get transaction history
var history = await transactions.GetHistoryAsync(new AssetTransactionSearch
{
    AssetTag = "LAPTOP-042",
    PageSize = 50,
    StartDate = new DateTime(2025, 1, 1)
});
```

---

## 8. Employees

**Service:** `EmployeeController`

### Model — `EmployeeInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `EmployeeId` | `int?` | *Read-only* — assigned on creation |
| `EmployeeNumber` | `string` | Unique identifier |
| `EmployeeName` | `string` | |
| `FirstName` | `string` | |
| `LastName` | `string` | |
| `Email` | `string` | |
| `Department` | `string` | |
| `SiteName` | `string` | |
| `Title` | `string` | |
| `EmployeeRecordStatus` | `int?` | 0 = active |
| `CustomFields` | `List<DcfValueInfo>` | |
| `ApplicableFields` | `List<string>` | For partial updates: list field names to update, or null for all |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateNewAsync** | `(IReadOnlyList<EmployeeInfo>) → WaspResult<List<EmployeeInfo>>` | Create (ignores existing numbers) |
| **UpdateExistingAsync** | `(IReadOnlyList<EmployeeInfo>) → WaspResult<List<EmployeeInfo>>` | Update by number; respects `ApplicableFields` |
| **SaveAsync** | `(IReadOnlyList<EmployeeInfo>) → WaspResult<List<EmployeeInfo>>` | Upsert — creates or updates |
| **DeleteAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by number |
| **SearchExactAsync** | `(string) → WaspResult<EmployeeInfo>` | Exact match in specified site |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<EmployeeInfo>>` | Paged search |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<EmployeeInfo>>` | Get by numbers |
| **GetByNumberV2Async** | `(IReadOnlyList<string>) → WaspResult<List<EmployeeInfo>>` | V2 of above |
| **InfoSearchAsync** | `(string) → WaspResult<List<EmployeeInfo>>` | Text search on number/name |
| **GetCheckoutStatusAsync** | `(string) → WaspResult<List<AssetCheckOutStatus>>` | Assets checked out to this employee |
| **GetByRfScanAsync** | `(RfScanModel) → WaspResult<EmployeeInfo>` | Lookup by RF scan identifier |

> **Note:** Use `AddressController.SaveEmployeeAddressesAsync` for addresses and `PhoneController.SaveEmployeePhonesAsync` for phones.

### Partial Updates with `ApplicableFields`

```csharp
// Only update the email — all other fields left untouched
await employees.UpdateExistingAsync(new[]
{
    new EmployeeInfo
    {
        EmployeeNumber = "EMP-007",
        Email = "new@example.com",
        ApplicableFields = new() { "Email" }
    }
});
```

---

## 9. Customers

**Service:** `CustomerController`

### Model — `CustomerInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `CustomerId` | `int?` | *Read-only* |
| `CustomerNumber` | `string` | Unique identifier |
| `CustomerName` | `string` | |
| `FirstName` | `string` | |
| `LastName` | `string` | |
| `Email` | `string` | |
| `Department` | `string` | |
| `SiteName` | `string` | |
| `CustomerRecordStatus` | `int?` | 0 = active |
| `CustomFields` | `List<DcfValueInfo>` | |
| `ApplicableFields` | `List<string>` | Partial update field list |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateNewAsync** | `(IReadOnlyList<CustomerInfo>) → WaspResult<List<CustomerInfo>>` | Create (ignores existing) |
| **UpdateExistingAsync** | `(IReadOnlyList<CustomerInfo>) → WaspResult<List<CustomerInfo>>` | Update; respects `ApplicableFields` |
| **SaveAsync** | `(IReadOnlyList<CustomerInfo>) → WaspResult<List<CustomerInfo>>` | Upsert |
| **DeleteAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by number |
| **GetCheckoutStatusAsync** | `(string) → WaspResult<List<AssetCheckOutStatus>>` | Assets checked out to customer |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<CustomerInfo>>` | Paged search (no phone/address) |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<CustomerInfo>>` | Get by numbers (no phone/address) |

---

## 10. Vendors

**Service:** `VendorController`

### Model — `VendorInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `VendorNumber` | `string` | Unique identifier |
| `VendorName` | `string` | |
| `VendorDescription` | `string` | |
| `Website` | `string` | |
| `Email` | `string` | |
| `ContactName` | `string` | |
| `ContactEmail` | `string` | |
| `VendorRecordStatus` | `int?` | |
| `CustomFields` | `List<DcfValueInfo>` | |
| `ApplicableFields` | `List<string>` | |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **GetCheckoutStatusAsync** | `(string) → WaspResult<List<AssetCheckOutStatus>>` | Assets checked out to vendor |
| **CreateNewAsync** | `(IReadOnlyList<VendorInfo>) → WaspResult<List<VendorInfo>>` | Create (ignores existing) |
| **UpdateExistingAsync** | `(IReadOnlyList<VendorInfo>) → WaspResult<List<VendorInfo>>` | Update by vendor number |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<VendorInfo>>` | Paged search |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<VendorInfo>>` | Get by numbers |

---

## 11. Sites

**Service:** `SiteController`

### Model — `SiteInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `SiteName` | `string` | Unique identifier |
| `SiteDescription` | `string` | |
| `SiteRecordStatus` | `int?` | |
| `CustomFields` | `List<DcfValueInfo>` | |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<SiteInfo>) → WaspResult<List<SiteInfo>>` | Create sites |
| **UpdateAsync** | `(IReadOnlyList<SiteInfo>) → WaspResult<List<SiteInfo>>` | Update sites |
| **SearchExactAsync** | `(string) → WaspResult<SiteInfo>` | Exact name match |
| **GetByNameAsync** | `(IReadOnlyList<string>) → WaspResult<List<SiteInfo>>` | Get by names |
| **InfoSearchAsync** | `(string) → WaspResult<List<SiteInfo>>` | Text search |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<SiteInfo>>` | Paged search |
| **DeleteByNameAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by names |

---

## 12. Locations

**Service:** `LocationController`

### Model — `LocationModelInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `SiteName` | `string` | **Required** |
| `ZoneName` | `string` | InventoryCloud only |
| `LocationCode` | `string` | **Required** — unique within site |
| `LocationDescription` | `string` | |
| `UsageTypeName` | `string` | InventoryCloud only |
| `LocationSaleable` | `int?` | InventoryCloud only |
| `DefaultLocation` | `bool` | |
| `LocationRecordStatus` | `int?` | *Read-only* |
| `LocationNotes` | `string` | New note to add |
| `AllLocationNotes` | `List<NoteInfo>` | *Read-only* — all existing notes |
| `CustomFields` | `List<DcfValueInfo>` | |
| `LocationSequence` | `decimal?` | |
| `LocationWidth` | `decimal?` | |
| `LocationHeight` | `decimal?` | |
| `LocationDepth` | `decimal?` | |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<LocationModelInfo>) → WaspResult<List<LocationModelInfo>>` | Create locations |
| **UpdateAsync** | `(IReadOnlyList<LocationModelInfo>) → WaspResult<List<LocationModelInfo>>` | Update locations |
| **SearchExactAsync** | `(string) → WaspResult<LocationModelInfo>` | Exact match |
| **AdvancedSearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<LocationModelInfo>>` | Paged search |
| **GetByCodeAsync** | `(IReadOnlyList<string>) → WaspResult<List<LocationModelInfo>>` | Get by codes |
| **InfoSearchAsync** | `(string) → WaspResult<List<LocationModelInfo>>` | Text search |
| **DeleteByCodeAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by codes |

---

## 13. Departments

**Service:** `DepartmentController`

### Model — `DepartmentInfo`

| Property | Type |
|----------|------|
| `RowNumber` | `int` |
| `DepartmentCode` | `string` |
| `DepartmentName` | `string` |
| `DepartmentRecordStatus` | `int?` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<DepartmentInfo>) → WaspResult<List<DepartmentInfo>>` | Create |
| **UpdateAsync** | `(IReadOnlyList<DepartmentInfo>) → WaspResult<List<DepartmentInfo>>` | Update |
| **InfoSearchAsync** | `(string) → WaspResult<List<DepartmentInfo>>` | Text search |
| **DeleteByCodeAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete |

---

## 14. Manufacturers

**Service:** `ManufacturerController`

### Model — `ManufacturerInfo`

| Property | Type |
|----------|------|
| `RowNumber` | `int` |
| `ManufacturerName` | `string` |
| `ManufacturerDescription` | `string` |
| `Website` | `string` |
| `Email` | `string` |
| `ContactName` | `string` |
| `ManufacturerRecordStatus` | `int?` |
| `CustomFields` | `List<DcfValueInfo>` |
| `ApplicableFields` | `List<string>` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateNewAsync** | `(IReadOnlyList<ManufacturerInfo>) → WaspResult<List<ManufacturerInfo>>` | Create |
| **UpdateExistingAsync** | `(IReadOnlyList<ManufacturerInfo>) → WaspResult<List<ManufacturerInfo>>` | Update |

> **Note:** Use `AddressController.SaveManufacturerAddressesAsync` for addresses.

---

## 15. Contracts

**Service:** `ContractController`

### Model — `ContractInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `ContractNumber` | `string` | Unique identifier |
| `ContractDescription` | `string` | |
| `ContractType` | `string` | |
| `VendorNumber` | `string` | |
| `VendorName` | `string` | |
| `StartDate` | `DateTime?` | |
| `EndDate` | `DateTime?` | |
| `Cost` | `decimal?` | |
| `Status` | `string` | |
| `Notes` | `List<NoteInfo>` | Only returned by `GetByNumberV2Async` |
| `CustomFields` | `List<DcfValueInfo>` | |
| `ApplicableFields` | `List<string>` | |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<ContractInfo>) → WaspResult<List<ContractInfo>>` | Create |
| **UpdateAsync** | `(IReadOnlyList<ContractInfo>) → WaspResult<List<ContractInfo>>` | Update header only (not assets) |
| **DeleteAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete by number |
| **AddAssetsAsync** | `(Dictionary<string, List<string>>) → WaspResult<List<WtResult>>` | Add assets: `{ "CONTRACT-001": ["ASSET-A", "ASSET-B"] }` |
| **RemoveAssetsAsync** | `(Dictionary<string, List<string>>) → WaspResult<List<WtResult>>` | Remove assets (same format) |
| **SearchExactAsync** | `(string) → WaspResult<ContractInfo>` | Exact match |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<ContractInfo>>` | Get by numbers (no notes) |
| **GetByNumberV2Async** | `(IReadOnlyList<string>) → WaspResult<List<ContractInfo>>` | Get by numbers (includes notes) |
| **InfoSearchAsync** | `(string) → WaspResult<List<ContractInfo>>` | Text search |

---

## 16. Funding

**Service:** `FundingController`

### Model — `FundingInfo`

| Property | Type |
|----------|------|
| `RowNumber` | `int` |
| `FundingName` | `string` |
| `FundingDescription` | `string` |
| `FundingType` | `string` |
| `FundingAmount` | `decimal?` |
| `StartDate` | `DateTime?` |
| `EndDate` | `DateTime?` |
| `FundingRecordStatus` | `int?` |
| `CustomFields` | `List<DcfValueInfo>` |
| `ApplicableFields` | `List<string>` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<FundingInfo>) → WaspResult<List<FundingInfo>>` | Create |
| **UpdateAsync** | `(IReadOnlyList<FundingInfo>) → WaspResult<List<FundingInfo>>` | Update header only |
| **DeleteAsync** | `(IReadOnlyList<FundingInfo>) → WaspResult<List<WtResult>>` | Delete |
| **AddAssetsAsync** | `(object) → WaspResult<List<WtResult>>` | Add assets to funding |
| **RemoveAssetsAsync** | `(object) → WaspResult<List<WtResult>>` | Remove assets |
| **AddRestrictedSitesAsync** | `(object) → WaspResult<List<WtResult>>` | Add restricted sites |
| **RemoveRestrictedSitesAsync** | `(object) → WaspResult<List<WtResult>>` | Remove restricted sites |
| **SearchExactAsync** | `(string) → WaspResult<FundingInfo>` | Exact name match |
| **GetByNameAsync** | `(IReadOnlyList<string>) → WaspResult<List<FundingInfo>>` | Get by names (details only) |
| **InfoSearchAsync** | `(string) → WaspResult<List<FundingInfo>>` | Text search |

---

## 17. Purchase Orders

**Service:** `PurchaseOrderController`

### Models

#### `AssetPurchaseOrderInfo`

| Property | Type | Notes |
|----------|------|-------|
| `RowNumber` | `int` | |
| `OrderId` | `int?` | |
| `PurchaseOrderNumber` | `string` | PO identifier |
| `DescriptionText` | `string` | |
| `ReferenceNumber` | `string` | |
| `DateStarted` | `DateTime?` | |
| `DateDue` | `DateTime?` | |
| `VendorNumber` | `string` | |
| `OrderStatus` | `string` | "Draft", "Open", "Closed" |
| `OrderStatusReasonCode` | `string` | See valid combos below |
| `ShipMethod` | `string` | |
| `PayMethod` | `string` | |
| `ShippingCost` | `decimal?` | |
| `TaxCost` | `decimal?` | |
| `TotalCost` | `decimal?` | |
| `ShipToContact` | `AddressInfo` | |
| `VendorContact` | `AddressInfo` | |
| `PurchaseOrderLines` | `List<AssetPurchaseOrderLineInfo>` | Line items |
| `PurchaseOrderNotes` | `List<NoteInfo>` | |
| `Quantity` | `decimal?` | |
| `LocationId` | `int?` | |
| `SiteName` | `string` | |
| `ContainerId` | `int?` | |
| `PurchaseDate` | `DateTime?` | |
| `SupplierId` | `int?` | |
| `SupplierName` | `string` | |
| `PurchaseCost` | `decimal?` | |
| `Accrual` | `decimal?` | |
| `Outstanding` | `decimal?` | |

**Valid OrderStatus + ReasonCode combos:**
- `"Draft"` + `"OK"`
- `"Open"` + `"PoOpenIssued"`
- `"Closed"` + `"ReceiverCancel"`
- `"Closed"` + `"VendorCancel"`

#### `AssetPurchaseOrderLineInfo`

| Property | Type |
|----------|------|
| `LineNumber` | `int` |
| `AssetTag` | `string` |
| `AssetTypeNumber` | `string` |
| `AssetDescription` | `string` |
| `UnitCost` | `decimal?` |
| `Quantity` | `decimal?` |
| `TotalCost` | `decimal?` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **CreateAsync** | `(IReadOnlyList<AssetPurchaseOrderInfo>) → WaspResult<List<AssetPurchaseOrderInfo>>` | Create POs |
| **UpdateAsync** | `(IReadOnlyList<AssetPurchaseOrderInfo>) → WaspResult<List<AssetPurchaseOrderInfo>>` | Update POs |
| **SearchAsync** | `(AdvancedSearchParameters) → WaspResult<List<AssetPurchaseOrderInfo>>` | Paged search |
| **GetByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<AssetPurchaseOrderInfo>>` | Get by PO numbers |
| **DeleteByNumberAsync** | `(IReadOnlyList<string>) → WaspResult<List<WtResult>>` | Delete (Draft only) |
| **UpdateStatusByNumberAsync** | `(object) → WaspResult<List<WtResult>>` | Update PO status |
| **ReceiveAsync** | `(object) → WaspResult<List<WtResult>>` | Receive items on a PO |

> Only POs in **Draft** status can be deleted.

---

## 18. Attachments

**Service:** `AttachmentController`

### Model — `AttachmentMetadata`

| Property | Type |
|----------|------|
| `Guid` | `string` |
| `FileName` | `string` |
| `FormType` | `string` |
| `AssociatedTag` | `string` |
| `FileSize` | `long?` |

### Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **UploadForFormsAsync** | `(object) → WaspResult<List<WtResult>>` | Upload attachment to a form item |
| **UploadBatchForFormsAsync** | `(object) → WaspResult<List<WtResult>>` | Batch upload across items |
| **DownloadAsync** | `(string guid) → Stream` | Download attachment binary by GUID |
| **GetMetadataByTagAsync** | `(object) → WaspResult<List<AttachmentMetadata>>` | Get metadata for an item's attachments |
| **GetByTagsListAsync** | `(object) → WaspResult<List<AttachmentMetadata>>` | Get attachments for multiple tags |

### Workflow: Get then download

```csharp
// Step 1: Get attachment metadata
var meta = await attachments.GetMetadataByTagAsync(new { AssociatedTag = "LAPTOP-042", FormType = "Asset" });

// Step 2: Download each attachment
foreach (var file in meta.Data)
{
    using var stream = await attachments.DownloadAsync(file.Guid);
    // Save or process the stream...
}
```

---

## 19. Addresses & Phones

### AddressController

| Method | Signature | Description |
|--------|-----------|-------------|
| **SaveCustomerAddressesAsync** | `(IReadOnlyList<AddressInfo>) → WaspResult<List<WtResult>>` | Create/update customer addresses |
| **SaveEmployeeAddressesAsync** | `(IReadOnlyList<AddressInfo>) → WaspResult<List<WtResult>>` | Create/update employee addresses |
| **SaveManufacturerAddressesAsync** | `(IReadOnlyList<AddressInfo>) → WaspResult<List<WtResult>>` | Create/update manufacturer addresses |
| **SaveSupplierAddressesAsync** | `(IReadOnlyList<AddressInfo>) → WaspResult<List<WtResult>>` | Create/update supplier addresses |

### PhoneController

| Method | Signature | Description |
|--------|-----------|-------------|
| **SaveCustomerPhonesAsync** | `(IReadOnlyList<PhoneInfo>) → WaspResult<List<WtResult>>` | Create/update customer phones |
| **SaveEmployeePhonesAsync** | `(IReadOnlyList<PhoneInfo>) → WaspResult<List<WtResult>>` | Create/update employee phones |
| **SaveManufacturerPhonesAsync** | `(IReadOnlyList<PhoneInfo>) → WaspResult<List<WtResult>>` | Create/update manufacturer phones |
| **SaveSupplierPhonesAsync** | `(IReadOnlyList<PhoneInfo>) → WaspResult<List<WtResult>>` | Create/update supplier phones |

> Address and Phone Types must already exist in the WASP system before saving.

---

## 20. System Info

**Service:** `SystemInfoController`

| Method | Signature | Description |
|--------|-----------|-------------|
| **GetServerTypeAsync** | `() → WaspResult<string>` | Returns `"MobileAsset"` or `"InventoryControl"` |

---

## 21. Helpers & Extensions

### `WaspResultExtensions.EnsureSuccess()`

Throws `WaspApiException` if the result has errors.

```csharp
var result = (await assets.AdvancedSearchAsync(search)).EnsureSuccess();
```

### `WaspResultExtensions.GetAllPagesAsync()`

Auto-pages through a search endpoint, collecting all results into a single list.

```csharp
var allAssets = await WaspResultExtensions.GetAllPagesAsync(
    searchParams => assets.AdvancedSearchAsync(searchParams),
    baseParams: new AdvancedSearchParameters
    {
        Filter = new TopLevelFilterType { Field = "SiteName", Operator = "eq", Value = "Main" },
        IgnoreAttachments = true
    },
    pageSize: 200
);
// allAssets is List<AssetInfo> with ALL matching results
```

### `WaspApiException`

```csharp
public class WaspApiException : Exception
{
    List<WtResult> Errors       // The individual error messages
}
```

---

## 22. Error Handling

1. **Always check `HasError`** on the result before using `Data`.
2. **Per-record errors** are reported via `WtResult.FieldName` and `WtResult.Message`.
3. **Batch operations** may partially succeed — some records created while others fail. The `Messages` list tells you which failed.
4. **HTTP errors** (network, auth, 500) throw `HttpRequestException` before reaching `WaspResult`.

```csharp
try
{
    var result = await assets.CreateFixedAssetsAsync(assetList);
    
    if (result.HasError)
    {
        // Some assets failed validation
        foreach (var msg in result.Messages.Where(m => m.ResultCode != 0))
            Log.Error($"Row error: {msg.FieldName} — {msg.Message}");
    }
    else
    {
        // All succeeded
        var created = result.Data;
    }
}
catch (HttpRequestException ex)
{
    // Network or auth failure
    Log.Error($"HTTP error: {ex.Message}");
}
```

---

## 23. Batch Limits

All batch endpoints enforce a **maximum of 500 records per request**. The client library **auto-chunks** larger lists — you can pass any size and it will split into 500-record batches internally.

```csharp
// This works even with 2000 assets — internally split into 4 batches of 500
var result = await assets.CreateFixedAssetsAsync(twoThousandAssets);
```

Services that auto-batch: `CreateFixedAssetsAsync`, `UpdateFixedAssetsAsync`, `PartialUpdateFixedAssetsAsync`, `CreateMultiQuantityAssetsAsync`, `UpdateMultiQuantityAssetsAsync`, `AddAssetQuantityAsync`, `MoveAsync`, `CheckOutAsync`, `CheckInAsync`, and all Address/Phone save methods, plus all CRUD operations on departments, sites, locations, employees, customers, vendors, manufacturers, contracts, funding, purchase orders, and asset types.

---

## Quick Reference — All Methods by Service

| Service | Methods | Count |
|---------|---------|-------|
| AssetController | Create/Update Fixed, Create/Update MQA, AddQuantity, GetByTags(Light), InfoSearch, AdvancedSearch, CheckoutStatus, StreamCSV×3 | 14 |
| AssetTypeController | Create, Update, GetByNumber, InfoSearch, AdvancedSearch, Delete, GetAllSimple | 7 |
| TransactionController | DisposeReasons, Dispose, Move, CheckOut, CheckIn, History, AuditCount(V2), Reconcile(V2), RfScan, StreamCSV×2, HistoryGrid, HistoryNotes, HistoryGridV2 | 16 |
| EmployeeController | CreateNew, UpdateExisting, Save, Delete, SearchExact, AdvancedSearch, GetByNumber(V2), InfoSearch, CheckoutStatus, GetByRfScan | 11 |
| CustomerController | CreateNew, UpdateExisting, Save, Delete, CheckoutStatus, AdvancedSearch, GetByNumber | 7 |
| VendorController | CheckoutStatus, CreateNew, UpdateExisting, AdvancedSearch, GetByNumber | 5 |
| SiteController | Create, Update, SearchExact, GetByName, InfoSearch, AdvancedSearch, Delete | 7 |
| LocationController | Create, Update, SearchExact, AdvancedSearch, GetByCode, InfoSearch, Delete | 7 |
| DepartmentController | Create, Update, InfoSearch, Delete | 4 |
| ManufacturerController | CreateNew, UpdateExisting | 2 |
| ContractController | Create, Update, Delete, AddAssets, RemoveAssets, SearchExact, GetByNumber(V2), InfoSearch | 9 |
| FundingController | Create, Update, Delete, AddAssets, RemoveAssets, AddRestrictedSites, RemoveRestrictedSites, SearchExact, GetByName, InfoSearch | 10 |
| PurchaseOrderController | Create, Update, Search, GetByNumber, Delete, UpdateStatus, Receive | 7 |
| AttachmentController | Upload, BatchUpload, Download, GetMetadata, GetByTagsList | 5 |
| AddressController | SaveCustomer, SaveEmployee, SaveManufacturer, SaveSupplier | 4 |
| PhoneController | SaveCustomer, SaveEmployee, SaveManufacturer, SaveSupplier | 4 |
| SystemInfoController | GetServerType | 1 |
| **Total** | | **120** |
