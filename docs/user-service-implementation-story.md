# User Service Implementation Story

## Overview
This document captures the full implementation journey for the `UserService` microservice in the e-commerce demo, including architectural decisions, design pivots, technical challenges, and final validation outcomes.

## Initial State
At the start:
- The solution contained three services (`UserService`, `ProductService`, `OrderService`).
- `UserService` was mostly scaffolded and TODO-driven.
- The runtime behavior was still default template endpoints (`/weatherforecast`).
- Identity-related artifacts suggested a self-managed auth direction, but no complete implementation existed.

## Architecture Target
The target for `UserService` was strict MVC layering:
- Controller
- Service (interface + implementation)
- Repository (interface + implementation)
- Data/DbContext
- Manual mapping via `Utils/UserMapper` (no AutoMapper)

The persistence layer was EF Core with PostgreSQL.

## Identity Design Pivot
### Original Direction
The early service shape implied possible in-service identity handling (e.g., token service, BCrypt references, login concepts).

### Alternative Considered
An external IdP path involving WSO2 was considered conceptually.

### Final Decision (Implemented)
Identity was externalized to AWS Cognito as the sole provider:
- No local password hash management
- No login endpoint in `UserService`
- No JWT issuance from `UserService`
- Cognito handles user auth/token creation
- `UserService` validates incoming Cognito JWTs and stores domain user profile data (`Id`, `CognitoSub`, `Name`, `Email`, `CreatedAt`)

This reduced security surface area and aligned better with microservice responsibility boundaries.

## Implementation Delivered
### 1. Domain and DTOs
- Implemented `User` entity with required fields and UTC `CreatedAt`.
- Implemented:
  - `RegisterRequestDto`
  - `UserResponseDto`
- Removed login-oriented DTO path from final service scope.

### 2. Persistence
- Added `UserDbContext` with:
  - `DbSet<User>`
  - unique indexes on `Email` and `CognitoSub`
- Implemented repository contract and async EF Core repository methods:
  - `AddAsync`
  - `GetByIdAsync`
  - `GetByCognitoSubAsync`

### 3. Mapping
- Added static `UserMapper` with:
  - DTO -> Entity (`Guid.NewGuid`, `DateTime.UtcNow`)
  - Entity -> Response DTO

### 4. Cognito Integration
- Added `ICognitoService` and `CognitoService`.
- Implemented Cognito `SignUp` integration.
- Added support for app clients requiring a secret:
  - implemented `SecretHash` computation (HMAC-SHA256 + Base64)
  - included `SecretHash` in Cognito calls.

### 5. Business Service
- Implemented `IUserService` + `UserService`.
- Registration flow:
  1. Register user in Cognito
  2. Map request to local entity with `CognitoSub`
  3. Persist via repository
  4. Return mapped response DTO

### 6. Controller Endpoints
- Implemented MVC controller endpoints:
  - `POST /api/users/register` (anonymous)
  - `GET /api/users/{id}` (authorized)
  - `GET /api/users/health` (anonymous)
- Added meaningful status code behavior:
  - `201`, `200`, `404`, `400`, `500`

### 7. Program and Runtime Wiring
- Registered DbContext, repository, services in DI.
- Configured JWT bearer authentication against Cognito authority/JWKS.
- Enabled startup migration application (`Database.Migrate()`).
- Kept startup clean per requirement:
  - no Swagger
  - no global exception middleware

## Challenges and Resolutions
### Challenge 1: NuGet/network instability during restore/build
**Issue:** intermittent package restore failures.  
**Resolution:** rerun builds/restores in enabled network context and continue from deterministic compile state.

### Challenge 2: EF design-time migration context failures
**Issue:** `dotnet ef` could not create the DbContext consistently.  
**Resolution:** implemented a design-time `UserDbContextFactory` and improved config loading behavior for tooling scenarios.

### Challenge 3: PostgreSQL connectivity and authentication
**Issue:** first connection refused, then `28P01` invalid password.  
**Resolution:** corrected connection string source and verified credentials/environment alignment with local Postgres runtime.

### Challenge 4: Config key shape mismatch
**Issue:** runtime expected flat keys (`Cognito__...`) while dev config initially used nested JSON object.  
**Resolution:** normalized configuration keys to the expected flat naming.

### Challenge 5: Cognito client secret requirement
**Issue:** Cognito app client required secret hash on auth/signup flows.  
**Resolution:** added `Cognito__ClientSecret`, implemented and wired `ComputeSecretHash`.

### Challenge 6: Cognito auth flow and user state blockers
**Issue:** `USER_PASSWORD_AUTH` initially disabled and user confirmation state caused auth failures.  
**Resolution:** enabled required Cognito flow and confirmed user state for test path.

### Challenge 7: `401 invalid_token` despite token issuance
**Issue:** token was minted, but API rejected it.  
**Root causes:**
1. Audience validation mismatch for Cognito access tokens in ASP.NET expectations.
2. Hidden dependency mismatch from explicitly pinned `Microsoft.IdentityModel.Tokens`, causing runtime token parsing method mismatch.
**Resolution:**
- set `ValidateAudience = false` for Cognito access-token compatibility.
- removed explicit `Microsoft.IdentityModel.Tokens` package pin to let `JwtBearer` dependency graph stay internally consistent.

## Final Verification Outcome
Verified outcomes:
- Project builds cleanly.
- Migrations apply / DB is up to date.
- `POST /api/users/register` works end-to-end (Cognito + DB persistence).
- Protected `GET /api/users/{id}` now succeeds with Cognito **access token** bearer auth.

Representative successful protected call result:
- Status: `200`
- Body includes correct `id`, `name`, `email`, and `createdAt`.

## Final State Summary
`UserService` is now fully functional for the scoped requirements:
- strict MVC architecture
- Cognito-based external identity
- PostgreSQL-backed user persistence
- working protected endpoint validation with Cognito JWT access tokens
