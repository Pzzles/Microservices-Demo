# Product Service Implementation Plan (Phased)

## Goal
Implement `ProductService` as a strict MVC ASP.NET Core Web API with Repository, Service, Controller, and manual mapper layers (no AutoMapper), backed by PostgreSQL, and protected endpoints validated via AWS Cognito JWT (no outbound calls to Cognito).

## Non-Negotiable Constraints
- Strict dependency direction: Controller -> Service -> Repository -> DbContext.
- No minimal APIs for product endpoints.
- No outbound HTTP calls from ProductService.
- No direct Cognito API calls; JWT validation only.
- No Swagger/OpenAPI.
- No global exception middleware.
- `Microsoft.IdentityModel.Tokens` must not be explicitly pinned in `.csproj`.

## Target Structure
```text
src/ProductService/
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
│
├── Data/
│   ├── ProductDbContext.cs
│   └── ProductDbContextFactory.cs
│
├── Models/
│   ├── Entities/
│   │   └── Product.cs
│   └── DTOs/
│       ├── CreateProductRequestDto.cs
│       ├── UpdateProductRequestDto.cs
│       └── ProductResponseDto.cs
│
├── Repositories/
│   ├── IProductRepository.cs
│   └── ProductRepository.cs
│
├── Services/
│   ├── IProductService.cs
│   └── ProductService.cs
│
├── Controllers/
│   └── ProductsController.cs
│
└── Utils/
    └── ProductMapper.cs
```

## Phase Status
- [x] Phase 0: Foundation and dependencies
- [x] Phase 1: Entity and DTO contracts
- [x] Phase 2: DbContext + design-time factory + seed data
- [x] Phase 3: Mapper utilities
- [x] Phase 4: Repository layer
- [x] Phase 5: Service layer
- [x] Phase 6: Controller endpoints
- [x] Phase 7: Program wiring and auth
- [x] Phase 8: Config files and runtime keys
- [x] Phase 9: Build, migration, endpoint verification

---

## Phase 0: Foundation and Dependencies
### Tasks
- Add required packages to `src/ProductService/ProductService.csproj`:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore.Design`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
- Ensure `Microsoft.IdentityModel.Tokens` is **not** explicitly pinned.
- Ensure folder/file layout matches target structure.

### Exit Criteria
- Restore/build succeeds.
- Package graph follows constraint (no explicit tokens pin).

---

## Phase 1: Entity and DTO Contracts
### Tasks
1. `Models/Entities/Product.cs`
   - `Id` (Guid), `Name`, `Description`, `Price` (decimal), `Category`, `ImageUrl` (nullable), `StockQuantity` (int), `CreatedAt` (UTC DateTime)
2. `Models/DTOs/CreateProductRequestDto.cs`
   - required create fields
3. `Models/DTOs/UpdateProductRequestDto.cs`
   - same fields, all nullable/optional for partial updates
4. `Models/DTOs/ProductResponseDto.cs`
   - mirrors full entity

### Exit Criteria
- Contracts compile.
- Update DTO supports partial update semantics.

---

## Phase 2: DbContext + Design-Time Factory + Seed Data
### Tasks
1. `Data/ProductDbContext.cs`
   - `DbSet<Product> Products`
   - index on `Category`
   - seed exactly 10 deterministic products with fixed GUIDs
   - categories: Phones, Laptops, Accessories, Apparel
2. `Data/ProductDbContextFactory.cs`
   - `IDesignTimeDbContextFactory<ProductDbContext>`
   - reads connection from `appsettings.Development.json` for EF tooling

### Exit Criteria
- DbContext compiles.
- Seed set is deterministic with fixed GUIDs.
- EF tooling can create context.

---

## Phase 3: Mapper Utilities
### Tasks
Create `Utils/ProductMapper.cs` with:
- `ToEntity(CreateProductRequestDto dto) -> Product`
- `ApplyUpdate(UpdateProductRequestDto dto, Product entity) -> void` (update only non-null fields)
- `ToResponseDto(Product product) -> ProductResponseDto`

### Exit Criteria
- Mapping is static/manual.
- Partial update logic only mutates provided fields.

---

## Phase 4: Repository Layer
### Tasks
1. `Repositories/IProductRepository.cs`
   - `GetAllAsync(string? category)`
   - `GetByIdAsync(Guid id)`
   - `AddAsync(Product product)`
   - `UpdateAsync(Product product)`
   - `DeleteAsync(Guid id)`
2. `Repositories/ProductRepository.cs`
   - async EF implementation
   - category filter only when query param provided
   - delete returns `false` when missing

### Exit Criteria
- Repository methods compile and behave as specified.

---

## Phase 5: Service Layer
### Tasks
1. `Services/IProductService.cs`
   - CRUD/read signatures returning DTOs
2. `Services/ProductService.cs`
   - mapping + repository delegation only

### Exit Criteria
- Service compiles and returns mapped DTOs.
- No extra business logic added.

---

## Phase 6: Controller Endpoints
### Tasks
Implement `Controllers/ProductsController.cs` (MVC):
- `GET /api/products` (anonymous, optional `category`)
- `GET /api/products/{id}` (anonymous, `200/404`)
- `POST /api/products` (authorized, `201 Created`)
- `PUT /api/products/{id}` (authorized, `200/404`)
- `DELETE /api/products/{id}` (authorized, `204/404`)
- `GET /api/products/health` (anonymous, `"Healthy"`)

### Exit Criteria
- Endpoint routes/status codes match spec.
- Auth attributes applied correctly.

---

## Phase 7: Program Wiring and Auth
### Tasks
In `Program.cs`:
- Add controllers
- Register `ProductDbContext` with `ConnectionStrings__ProductDb`
- Register scoped DI:
  - `IProductRepository -> ProductRepository`
  - `IProductService -> ProductService`
- Configure JWT Bearer:
  - authority: `https://cognito-idp.{region}.amazonaws.com/{userPoolId}`
  - `ValidateAudience = false`
  - issuer/lifetime/signing key validations enabled
- `UseAuthentication`, `UseAuthorization`
- apply `Database.Migrate()` on startup
- no swagger/global exception middleware

### Exit Criteria
- App starts with correct pipeline and auth configuration.

---

## Phase 8: Configuration Files and Runtime Keys
### Tasks
1. `appsettings.json` placeholders:
   - `ConnectionStrings__ProductDb`
   - `Cognito__UserPoolId`
   - `Cognito__Region`
2. `appsettings.Development.json`:
   - `ConnectionStrings__ProductDb=Host=localhost;Port=5432;Database=product_service_db;Username=postgres;Password=YOUR_PASSWORD_HERE`
   - `Cognito__UserPoolId=eu-north-1_VtygYI1E3`
   - `Cognito__Region=eu-north-1`

### Exit Criteria
- Keys exist and are environment-overridable.

---

## Phase 9: Build, Migration, Endpoint Verification
### Mandatory Order
1. Build
2. Migration generation
3. Migration apply
4. Endpoint tests

### Commands
```bash
dotnet build src/ProductService/ProductService.csproj -c Debug
dotnet ef migrations add InitialCreate --project src/ProductService
dotnet ef database update --project src/ProductService
```

### Verification Matrix
- `GET /api/products` returns seeded data
- `GET /api/products?category=Phones` filters correctly
- `GET /api/products/{id}` returns `200` for existing, `404` missing
- `POST /api/products` with valid Cognito token returns `201`
- `PUT /api/products/{id}` with valid token updates and returns `200`
- `DELETE /api/products/{id}` with valid token returns `204` then `404` on re-delete
- `GET /api/products/health` returns `200 Healthy`
- unauthorized write operations return `401`

### Exit Criteria
- Build clean.
- Migrations successful.
- Endpoint matrix passes.

---

## Runtime Environment Variables
| Variable | Purpose |
|---|---|
| `ConnectionStrings__ProductDb` | PostgreSQL connection string for ProductService database. |
| `Cognito__UserPoolId` | Cognito user pool id for JWT issuer validation. |
| `Cognito__Region` | AWS region used to build Cognito authority URL. |

## Notes
- Keep implementation incremental; only mark a phase complete after executable verification.
- If a phase is blocked by environment (DB/Cognito setup), document the blocker and do not mark complete.

## Latest Verification Notes
- `dotnet build src/ProductService/ProductService.csproj -c Debug`: succeeded (0 warnings, 0 errors).
- `dotnet ef migrations add InitialCreate --project src/ProductService`: succeeded.
- `dotnet ef database update --project src/ProductService --connection "Host=localhost;Port=5432;Database=product_service_db;Username=postgres;Password=yourpassword"`: succeeded and applied `InitialCreate`.
- Endpoint matrix passed:
  - `GET /api/products/health` -> `200`
  - `GET /api/products` -> 10 seeded items
  - `GET /api/products?category=Phones` -> 3 filtered items
  - `GET /api/products/{id}` -> `200` for existing id
  - `POST /api/products` without auth -> `401`
  - `POST /api/products` with Cognito access token -> `201`
  - `PUT /api/products/{id}` with token -> `200`
  - `DELETE /api/products/{id}` with token -> `204`
  - second delete on same id -> `404`
