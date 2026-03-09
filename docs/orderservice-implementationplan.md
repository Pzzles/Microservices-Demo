# Order Service Implementation Plan (Phased)

## Goal
Implement `OrderService` as a strict MVC ASP.NET Core Web API with Repository, Service, Controller, and manual mapper layers (no AutoMapper), backed by PostgreSQL, validating Cognito JWTs, and calling ProductService via a named `HttpClient` for product validation.

## Non-Negotiable Constraints
- Strict dependency direction: Controller -> Service -> Repository -> DbContext.
- No minimal APIs for order endpoints.
- JWT validation only (no direct Cognito API calls).
- One outbound HTTP dependency: ProductService via `IHttpClientFactory` named client.
- No Swagger/OpenAPI.
- No global exception middleware.
- `Microsoft.IdentityModel.Tokens` must not be explicitly pinned in `.csproj`.
- Required comments must appear verbatim:
  - XML doc on `OrderStatus` enum.
  - XML doc on `Order` entity snapshot pattern.
  - TEMPORARY block comment in `ProductValidationService`.

## Target Structure
```text
src/OrderService/
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
│
├── Data/
│   ├── OrderDbContext.cs
│   └── OrderDbContextFactory.cs
│
├── Models/
│   ├── Entities/
│   │   └── Order.cs
│   ├── Enums/
│   │   └── OrderStatus.cs
│   └── DTOs/
│       ├── CreateOrderRequestDto.cs
│       ├── UpdateOrderStatusDto.cs
│       └── OrderResponseDto.cs
│
├── Repositories/
│   ├── IOrderRepository.cs
│   └── OrderRepository.cs
│
├── Services/
│   ├── IOrderService.cs
│   ├── OrderService.cs
│   ├── IProductValidationService.cs
│   ├── ProductValidationService.cs
│   └── ProductDto.cs
│
├── Controllers/
│   └── OrdersController.cs
│
└── Utils/
    └── OrderMapper.cs
```

## Phase Status
- [x] Phase 0: Foundation and dependencies
- [x] Phase 1: Enum/entity/DTO contracts
- [x] Phase 2: DbContext + factory
- [x] Phase 3: Mapper utilities
- [x] Phase 4: Repository layer
- [x] Phase 5: Product validation integration service
- [x] Phase 6: Order service logic
- [x] Phase 7: Controller endpoints
- [x] Phase 8: Program wiring and configuration
- [x] Phase 9: Build, migration, endpoint verification

---

## Phase 0: Foundation and Dependencies
### Tasks
- Update `src/OrderService/OrderService.csproj` with:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore.Design`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
- Ensure no explicit `Microsoft.IdentityModel.Tokens` pin.
- Create missing folders/files per target structure.

### Exit Criteria
- Project restore/build succeeds.
- Package graph obeys no-pin constraint.

---

## Phase 1: Enum/Entity/DTO Contracts
### Tasks
1. `Models/Enums/OrderStatus.cs`:
   - `Pending=0`, `Confirmed=1`, `Shipped=2`, `Delivered=3`, `Cancelled=4`
   - Add exact XML doc:
   - `TODO: Cancelled status will be expanded to support WSO2-routed refund flow in a future phase.`
2. `Models/Entities/Order.cs`:
   - Fields per spec
   - Add exact XML doc for snapshot pattern (verbatim).
3. DTOs:
   - `CreateOrderRequestDto`: `UserId`, `ProductId`, `Quantity`
   - `UpdateOrderStatusDto`: `Status`
   - `OrderResponseDto`: full response with `Status` as enum name string

### Exit Criteria
- Contracts compile.
- Required doc comments present verbatim.

---

## Phase 2: DbContext + Factory
### Tasks
1. `Data/OrderDbContext.cs`
   - `DbSet<Order> Orders`
   - index on `UserId`
   - `OrderStatus` stored as string via `.HasConversion<string>()`
2. `Data/OrderDbContextFactory.cs`
   - implement `IDesignTimeDbContextFactory<OrderDbContext>`
   - read dev connection string from config/env for EF tooling

### Exit Criteria
- DbContext compiles.
- Enum conversion and index are configured correctly.
- EF tooling can create context.

---

## Phase 3: Mapper Utilities
### Tasks
Create `Utils/OrderMapper.cs`:
- `ToEntity(CreateOrderRequestDto dto, string productName, decimal unitPrice) -> Order`
  - new Guid
  - status `Pending`
  - `TotalPrice = unitPrice * dto.Quantity`
  - `CreatedAt = DateTime.UtcNow`
- `ToResponseDto(Order order) -> OrderResponseDto`
  - `Status = order.Status.ToString()`

### Exit Criteria
- Mapper methods compile and follow exact mapping rules.

---

## Phase 4: Repository Layer
### Tasks
1. `Repositories/IOrderRepository.cs`:
   - `GetByIdAsync`
   - `GetByUserIdAsync`
   - `AddAsync`
   - `UpdateAsync`
2. `Repositories/OrderRepository.cs` async EF implementation.

### Exit Criteria
- Repository methods compile and work asynchronously.

---

## Phase 5: Product Validation Integration Service
### Tasks
1. Add `Services/ProductDto.cs` (internal service contract object).
2. `IProductValidationService.cs`:
   - `GetProductAsync(Guid productId) -> Task<ProductDto?>`
3. `ProductValidationService.cs`:
   - use named `HttpClient` `"ProductService"`
   - call `GET /api/products/{productId}`
   - deserialize with `System.Text.Json`
   - return `null` on non-success or exception
   - include required TEMPORARY WSO2 comment block verbatim above constructor

### Exit Criteria
- Integration service compiles.
- Required comment block present verbatim.

---

## Phase 6: Order Service Logic
### Tasks
1. `IOrderService.cs` methods:
   - `CreateAsync(CreateOrderRequestDto dto) -> Task<(OrderResponseDto? order, string? error)>`
   - `GetByIdAsync(Guid id)`
   - `GetByUserIdAsync(Guid userId)`
   - `UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto)`
2. `OrderService.cs` implementation:
   - validate product via `IProductValidationService`
   - null product -> `(null, "Product not found")`
   - insufficient stock -> `(null, "Insufficient stock")`
   - map/persist/return response

### Exit Criteria
- Create flow matches required sequence and error messages exactly.

---

## Phase 7: Controller Endpoints
### Tasks
Implement `Controllers/OrdersController.cs`:
- `POST /api/orders` (auth) -> `201`, `404` product missing, `400` insufficient stock/bad input
- `GET /api/orders/{id}` (auth) -> `200/404`
- `GET /api/orders/user/{userId}` (auth) -> `200` array (possibly empty)
- `PATCH /api/orders/{id}/status` (auth) -> `200/404`
- `GET /api/orders/health` (anonymous) -> `200 "Healthy"`

### Exit Criteria
- Route/status/auth behavior matches spec exactly.

---

## Phase 8: Program Wiring and Configuration
### Tasks
In `Program.cs`:
- register DbContext using `ConnectionStrings__OrderDb`
- DI:
  - `IOrderRepository -> OrderRepository`
  - `IOrderService -> OrderService`
  - `IProductValidationService -> ProductValidationService`
- register named client:
  - `"ProductService"` with `ServiceUrls__ProductService`
- JWT bearer:
  - authority `https://cognito-idp.{region}.amazonaws.com/{userPoolId}`
  - `ValidateAudience = false`
  - issuer/lifetime/signing validations enabled
- startup migrate (`Database.Migrate()`)
- no swagger/global middleware

Config files:
- `appsettings.json` placeholders:
  - `ConnectionStrings__OrderDb`
  - `Cognito__UserPoolId`
  - `Cognito__Region`
  - `ServiceUrls__ProductService`
- `appsettings.Development.json`:
  - local DB connection
  - Cognito user pool/region
  - `ServiceUrls__ProductService=http://localhost:5002`
  - `ServiceUrls__ProductService__Note` exact value:
    - `TEMPORARY: direct URL for local dev. Replace with WSO2 gateway URL in production config.`

### Exit Criteria
- App starts with correct runtime wiring.
- Config keys present as required.

---

## Phase 9: Build, Migration, Endpoint Verification
### Mandatory Order
1. Build
2. Migration add
3. Migration apply
4. Endpoint tests

### Commands
```bash
dotnet build src/OrderService/OrderService.csproj -c Debug
dotnet ef migrations add InitialCreate --project src/OrderService
dotnet ef database update --project src/OrderService
```

### Verification Matrix
- `GET /api/orders/health` -> `200`
- `POST /api/orders`:
  - `404` when product missing
  - `400` when stock insufficient
  - `201` when valid
- `GET /api/orders/{id}` -> `200/404`
- `GET /api/orders/user/{userId}` -> `200` array
- `PATCH /api/orders/{id}/status` -> `200/404`
- write endpoints reject missing auth (`401`)

### Exit Criteria
- Build clean.
- Migration applied.
- Endpoint matrix passes.

---

## Runtime Environment Variables
| Variable | Purpose |
|---|---|
| `ConnectionStrings__OrderDb` | PostgreSQL connection string for OrderService DB. |
| `Cognito__UserPoolId` | Cognito user pool id for JWT issuer validation. |
| `Cognito__Region` | AWS region for Cognito authority URL. |
| `ServiceUrls__ProductService` | Base URL for ProductService API calls. |

## Notes
- Mark phases complete only after executable verification.
- If blocked by environment, document blocker and keep phase open.

## Latest Verification Notes
- `dotnet build src/OrderService/OrderService.csproj -c Debug`: succeeded (0 warnings, 0 errors).
- `dotnet ef migrations add InitialCreate --project src/OrderService`: succeeded.
- `dotnet ef database update --project src/OrderService --connection "Host=localhost;Port=5432;Database=order_service_db;Username=postgres;Password=yourpassword"`: succeeded and applied `InitialCreate`.
- Endpoint matrix passed:
  - `GET /api/orders/health` -> `200`
  - `POST /api/orders` without JWT -> `401`
  - `POST /api/orders` with missing product -> `404`
  - `POST /api/orders` with insufficient stock -> `400`
  - `POST /api/orders` valid -> `201`
  - `GET /api/orders/{id}` -> `200`
  - `GET /api/orders/user/{userId}` -> `200` array
  - `PATCH /api/orders/{id}/status` -> `200`
  - `PATCH /api/orders/{id}/status` with missing id -> `404`
