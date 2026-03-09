# User Service Implementation Plan (Phased Benchmarks)

## Goal
Implement the `UserService` microservice in ASP.NET Core Web API using strict MVC layering with manual mapping and AWS Cognito integration for identity.

## Phase Status
- [x] Phase 0: Foundation and Dependencies (verified by restore/build + package/file changes)
- [x] Phase 1: Domain Model and DTO Contracts (implemented and compile-verified)
- [ ] Phase 2: Persistence Layer (implemented; full runtime verification blocked by unavailable local PostgreSQL)
- [x] Phase 3: Mapping Utilities (implemented and compile-verified)
- [ ] Phase 4: Cognito Integration Service (implemented; requires live Cognito verification)
- [ ] Phase 5: User Business Service (implemented; depends on live Cognito + DB verification)
- [ ] Phase 6: Controller Endpoints (implemented; requires runtime endpoint verification)
- [ ] Phase 7: Program Wiring and Runtime Config (implemented; startup migration requires reachable DB)
- [x] Phase 8: Configuration Files (implemented and file-verified)
- [ ] Phase 9: Migrations and Validation (migration created; `database update` blocked by no DB listener on `localhost:5432`)

## Non-Negotiable Constraints
- Use strict MVC layering: Controller -> Service -> Repository -> DbContext.
- No minimal APIs for user endpoints.
- No AutoMapper.
- No JWT issuance in this service.
- No login flow in this service.
- No BCrypt in this service.
- AWS Cognito is the only identity provider.
- No Swagger/OpenAPI.
- No global exception middleware yet.

## Target Folder Structure
```text
src/UserService/
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
│
├── Data/
│   └── UserDbContext.cs
│
├── Models/
│   ├── Entities/
│   │   └── User.cs
│   └── DTOs/
│       ├── RegisterRequestDto.cs
│       └── UserResponseDto.cs
│
├── Repositories/
│   ├── IUserRepository.cs
│   └── UserRepository.cs
│
├── Services/
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── ICognitoService.cs
│   └── CognitoService.cs
│
├── Controllers/
│   └── UsersController.cs
│
└── Utils/
    └── UserMapper.cs
```

---

## Phase 0: Foundation and Dependencies
### Scope
- Align project references and config prerequisites before feature coding.

### Tasks
- Add required NuGet packages:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore.Design`
  - `AWSSDK.CognitoIdentityProvider`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Microsoft.IdentityModel.Tokens`
- Remove/avoid identity packages not needed (`BCrypt`, token generation logic).
- Ensure folders/files exist per target structure.

### Benchmark / Exit Criteria
- Project restores successfully.
- Package list matches requirements only.
- Folder structure matches target layout.

---

## Phase 1: Domain Model and DTO Contracts
### Scope
- Define data contracts first so repository/service/controller can build on stable types.

### Tasks
1. Create `Models/Entities/User.cs` with:
   - `Id: Guid` (PK)
   - `CognitoSub: string` (unique/indexed)
   - `Name: string`
   - `Email: string` (unique/indexed)
   - `CreatedAt: DateTime` (UTC)
2. Create `Models/DTOs/RegisterRequestDto.cs`:
   - `Name: string`
   - `Email: string`
   - `Password: string` (for Cognito call only, never persisted)
3. Create `Models/DTOs/UserResponseDto.cs`:
   - `Id: Guid`
   - `Name: string`
   - `Email: string`
   - `CreatedAt: DateTime`

### Benchmark / Exit Criteria
- All model/DTO files compile.
- DTOs do not include auth token fields.
- Entity includes `CognitoSub` and no password hash fields.

---

## Phase 2: Persistence Layer
### Scope
- Build EF Core context and repository contract/implementation.

### Tasks
1. Create `Data/UserDbContext.cs`:
   - Inherit `DbContext`
   - `DbSet<User> Users`
   - Configure `User` in `OnModelCreating`
   - Unique indexes on `Email` and `CognitoSub`
2. Create `Repositories/IUserRepository.cs`:
   - `Task AddAsync(User user)`
   - `Task<User?> GetByIdAsync(Guid id)`
   - `Task<User?> GetByCognitoSubAsync(string sub)`
3. Implement `Repositories/UserRepository.cs` using async EF Core methods.

### Benchmark / Exit Criteria
- Repository compiles and uses async EF methods.
- Unique indexes configured in model builder.
- No service/controller logic in repository.

---

## Phase 3: Mapping Utilities
### Scope
- Manual mapping utility to isolate object transformation logic.

### Tasks
Create `Utils/UserMapper.cs` static class with:
- `ToEntity(RegisterRequestDto dto, string cognitoSub) -> User`
  - Generates new `Guid` for `Id`
  - Sets `CreatedAt = DateTime.UtcNow`
- `ToResponseDto(User user) -> UserResponseDto`

### Benchmark / Exit Criteria
- Mapper is static and pure (no DB/service dependencies).
- `Password` is never mapped to persistence model.

---

## Phase 4: Cognito Integration Service
### Scope
- Isolate AWS Cognito registration into dedicated service.

### Tasks
1. Create `Services/ICognitoService.cs`:
   - `Task<string> RegisterAsync(string email, string password, string name)`
2. Implement `Services/CognitoService.cs`:
   - Use `Amazon.CognitoIdentityProvider`
   - Read:
     - `Cognito__UserPoolId`
     - `Cognito__ClientId`
     - `Cognito__Region`
   - Call Cognito SignUp
   - Return Cognito `sub` on success

### Benchmark / Exit Criteria
- Service compiles with AWS SDK.
- Configuration values come from config/env overrides.
- Return value is Cognito sub string.

---

## Phase 5: User Business Service
### Scope
- Orchestrate Cognito registration + local persistence.

### Tasks
1. Create `Services/IUserService.cs`:
   - `Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto)`
   - `Task<UserResponseDto?> GetByIdAsync(Guid id)`
2. Implement `Services/UserService.cs`:
   - Register in Cognito first via `ICognitoService.RegisterAsync`
   - Map dto -> entity via `UserMapper.ToEntity`
   - Persist using repository
   - Return `UserMapper.ToResponseDto`

### Benchmark / Exit Criteria
- Service has no direct controller/HTTP concerns.
- Flow order is Cognito first, DB second.
- Return types match interface exactly.

---

## Phase 6: Controller Endpoints
### Scope
- Expose REST endpoints with meaningful status codes.

### Tasks
Implement `Controllers/UsersController.cs` with:
1. `POST /api/users/register`
   - Anonymous access
   - Calls `UserService.RegisterAsync`
   - Returns `201 Created` with `UserResponseDto`
2. `GET /api/users/{id}`
   - Requires valid Cognito JWT
   - Calls `UserService.GetByIdAsync`
   - Returns `200` or `404`
3. `GET /api/users/health`
   - Anonymous access
   - Returns `200 OK` + `"Healthy"`

Error handling in controller:
- `400` for bad input
- `500` for unexpected failures

### Benchmark / Exit Criteria
- Uses MVC controller attributes and action methods.
- No minimal API mappings for user endpoints.
- Endpoint responses match required status behavior.

---

## Phase 7: Program Wiring and Runtime Config
### Scope
- Register dependencies, auth, DB migration, and middleware pipeline.

### Tasks
In `Program.cs`:
- Register `UserDbContext` with Npgsql using `ConnectionStrings__UserDb`
- Register scoped services:
  - `IUserRepository -> UserRepository`
  - `IUserService -> UserService`
  - `ICognitoService -> CognitoService`
- Configure JWT Bearer auth for Cognito:
  - `authority = https://cognito-idp.{region}.amazonaws.com/{userPoolId}`
  - `audience = Cognito__ClientId`
- Add `app.UseAuthentication()` and `app.UseAuthorization()`
- Apply migrations on startup with `dbContext.Database.Migrate()`
- Keep Program clean: no Swagger, no global exception middleware

### Benchmark / Exit Criteria
- Auth is configured for validation only (no token generation).
- App startup applies migrations automatically.
- Middleware order is correct.

---

## Phase 8: Configuration Files
### Scope
- Ensure all runtime settings exist and can be overridden by environment variables.

### Tasks
1. `appsettings.json` must contain placeholder keys:
   - `ConnectionStrings__UserDb`
   - `Cognito__UserPoolId`
   - `Cognito__ClientId`
   - `Cognito__Region`
2. `appsettings.Development.json` should include working local connection string:
   - Host: `localhost`
   - Port: `5432`
   - Database: `user_service_db`
   - Username: `postgres`
   - Password: `YOUR_PASSWORD_HERE`

### Benchmark / Exit Criteria
- All required keys present.
- Local dev connection string is set for PostgreSQL.

---

## Phase 9: Migrations and Validation
### Scope
- Finalize schema and verify runtime readiness.

### Tasks
- Run migration commands:
```bash
dotnet ef migrations add InitialCreate --project src/UserService
dotnet ef database update --project src/UserService
```
- Validate:
  - Register endpoint creates Cognito user + DB record.
  - Get by id respects auth requirement.
  - Health endpoint remains anonymous.

### Benchmark / Exit Criteria
- Initial migration created and DB updated.
- Endpoints behave as expected for 201/200/404/400/500 paths.

---

## Runtime Environment Variables Summary
| Variable | Purpose |
|---|---|
| `ConnectionStrings__UserDb` | PostgreSQL connection string for `UserDbContext`. |
| `Cognito__UserPoolId` | AWS Cognito User Pool ID used for signup and JWT validation authority. |
| `Cognito__ClientId` | Cognito App Client ID used for signup and JWT audience validation. |
| `Cognito__Region` | AWS region (for Cognito client and JWT authority URL). |

---

## Definition of Done (Project-Level)
- All required files/layers implemented in strict MVC dependency direction.
- Cognito is the only identity provider path.
- No login/token issuance/BCrypt logic remains.
- EF Core migrations are applied successfully.
- Controller returns required HTTP status codes.
- Configuration works with environment variable overrides.

## Latest Verification Notes
- `dotnet build E-Commerce-Microservices-Demo.sln -c Debug`: succeeded with 0 errors.
- `dotnet ef migrations add InitialCreate --project src/UserService`: succeeded.
- `dotnet ef database update --project src/UserService`: failed because PostgreSQL was not reachable at `127.0.0.1:5432` (`SocketException 10061`).
