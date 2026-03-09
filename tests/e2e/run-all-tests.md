# E2E Runbook — `tests/e2e/run-all-tests.ps1`

This runbook explains how to execute the PowerShell end-to-end test script manually and how to diagnose common failures.

## What the script does (high level)

`run-all-tests.ps1`:

1. Starts 3 .NET services locally (UserService, ProductService, OrderService) using `dotnet run`.
2. Waits for each service to become healthy via HTTP health endpoints.
3. Registers a new user in UserService.
4. Confirms the user in **AWS Cognito** (admin confirm sign-up).
5. Requests a Cognito access token (USER_PASSWORD_AUTH) and uses it as a Bearer token.
6. Runs a set of API checks against:
   - User endpoints (authorized/unauthorized)
   - Product endpoints (list, filter, get by id, create/update/delete with auth)
   - Order endpoints (auth required, missing product, insufficient stock, create/get/list/patch)
7. Stops all started services in a `finally` block (even on failures).

## Prerequisites

### 1) Windows + PowerShell
- Run from **Windows PowerShell** or **PowerShell 7+**.
- You may need to allow local scripts:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

### 2) .NET SDK
- Install the .NET SDK version required by the services in this repo.
- Verify:

```powershell
dotnet --info
```

### 3) PostgreSQL running locally
The script expects PostgreSQL on `localhost:5432` with these DBs (and the user/password shown):

- `user_service_db`
- `product_service_db`
- `order_service_db`

Default connection strings used by the script:

```text
Host=localhost;Port=5432;Database=user_service_db;Username=postgres;Password=yourpassword
Host=localhost;Port=5432;Database=product_service_db;Username=postgres;Password=yourpassword
Host=localhost;Port=5432;Database=order_service_db;Username=postgres;Password=yourpassword
```

Notes:
- Ensure the databases exist.
- Ensure the `postgres` user password matches the script value (or update the script).
- Ensure the services can apply migrations / create schema as needed for your environment.

Quick connectivity check:

```powershell
psql -h localhost -p 5432 -U postgres -d user_service_db
```

### 4) AWS CLI installed
- Install AWS CLI v2.
- Verify:

```powershell
aws --version
```

### 5) AWS CLI configured and authorized for Cognito admin calls
The script uses:

- `aws cognito-idp admin-confirm-sign-up`
- `aws cognito-idp initiate-auth`

You must have AWS credentials configured that can perform these operations for the target user pool.

Verify your AWS identity:

```powershell
aws sts get-caller-identity
```

Region must match the UserService Cognito region from config (see below).

### 6) Cognito config present in `UserService` dev settings
The script reads:

- `src/UserService/appsettings.Development.json`

Required keys (as used by the script):

- `Cognito__ClientId`
- `Cognito__ClientSecret`
- `Cognito__UserPoolId`
- `Cognito__Region`

If any of these are missing or empty, the script will stop immediately with:

> Missing Cognito values in src/UserService/appsettings.Development.json

## Startup expectations / ports

The script starts services on these base URLs:

- UserService: `http://localhost:5204`
- ProductService: `http://localhost:5002`
- OrderService: `http://localhost:5136`

Health endpoints used:

- `GET http://localhost:5204/api/users/health`
- `GET http://localhost:5002/api/products/health`
- `GET http://localhost:5136/api/orders/health`

Timeout behavior:
- Each health check is polled up to **80** times.
- Delay between checks: **500 ms**.
- Overall wait per service: ~**40 seconds** maximum.

If a service never becomes healthy, the script fails with:

> `<ServiceName> did not become healthy at <url>`

## How to run

From the repo root, run:

```powershell
cd C:\Users\user\Desktop\Projects\Large\Assessements_and_Experiments\Experiments\E-Commerce-Microservice-Demo

# Run the E2E script
.\tests\e2e\run-all-tests.ps1
```

Alternatively, from the `tests/e2e` folder:

```powershell
cd .\tests\e2e
.\run-all-tests.ps1
```

The script sets:

- `ASPNETCORE_ENVIRONMENT=Development`

And launches the services with `--no-build`. That means:

- You should **build once** before running if you haven’t built recently:

```powershell
dotnet build
```

## Output and how to interpret results

### PASS lines
Each check prints:

- `PASS: <name> (<statusCode>)` for request assertions
- `PASS: <service> healthy` for health checks

Example:

```text
PASS: UserService healthy
PASS: User Register (201)
PASS: Products GetAll (200)
```

### FAIL behavior
- Any status mismatch throws and stops the run.
- Failures look like:

```text
<CheckName> failed. Expected status <Expected>, got <Actual>.
```

### Final success line
If everything passes:

```text
ALL TESTS PASSED
```

### Cleanup behavior
Regardless of pass/fail, the script attempts to stop all 3 `dotnet run` processes in the `finally` block.

## Troubleshooting common failures

### 1) Health checks failing / timing out
**Symptoms**
- Error: `UserService did not become healthy at http://localhost:5204/api/users/health` (or similar)

**Common causes & fixes**
1. **Port already in use**
   - Another process is using 5204/5002/5136.
   - Check:

   ```powershell
   netstat -ano | findstr :5204
   netstat -ano | findstr :5002
   netstat -ano | findstr :5136
   ```

   - Stop the conflicting PID or change ports in the script.

2. **Service failed to start (crashed on boot)**
   - The script starts services in the background. Check service console logs:
     - Re-run the service manually in a separate terminal to see output:

   ```powershell
   dotnet run --project src/UserService --urls http://localhost:5204
   ```

3. **Database connection failure during startup**
   - If a service cannot connect to PostgreSQL, it may fail to start and never become healthy.
   - See “DB connection issues” below.

4. **Cold start longer than ~40 seconds**
   - Increase the wait loop or reduce workload during startup.

---

### 2) DB connection issues (PostgreSQL)
**Symptoms**
- Health check never becomes 200
- Service logs show connection/auth errors
- Errors like: authentication failed, database does not exist, connection refused

**Checklist**
1. Confirm PostgreSQL is running and listening on 5432:

```powershell
netstat -ano | findstr :5432
```

2. Confirm credentials match what the script passes:

- Username: `postgres`
- Password: `yourpassword`

3. Confirm DBs exist:

```powershell
psql -h localhost -p 5432 -U postgres -c "\l"
```

4. If you use a different password/port/user:
- Update the connection strings in `tests/e2e/run-all-tests.ps1`.

---

### 3) AWS CLI / Cognito failures
**Symptoms**
- Script fails around user confirmation or auth token retrieval
- Error: `Failed to get Cognito token with USER_PASSWORD_AUTH.`
- AWS CLI prints errors (AccessDenied, NotAuthorized, InvalidParameter, etc.)

**Common causes & fixes**
1. **AWS CLI not logged in / wrong profile**
   - Verify:

   ```powershell
   aws sts get-caller-identity
   ```

   - If you use profiles, set one before running:

   ```powershell
   $env:AWS_PROFILE = "your-profile"
   ```

2. **Region mismatch**
   - Script uses `Cognito__Region` from `appsettings.Development.json`.
   - Ensure AWS CLI can access the user pool in that region.

3. **Insufficient IAM permissions**
   - The identity must be allowed to call:
     - `cognito-idp:AdminConfirmSignUp`
     - `cognito-idp:InitiateAuth`

4. **Cognito app client auth flow not enabled**
   - The client must allow `USER_PASSWORD_AUTH` (or equivalent) for `initiate-auth`.

5. **Client secret / secret hash issues**
   - The script computes `SECRET_HASH = Base64(HMAC_SHA256(ClientSecret, username + clientId))`.
   - Ensure the app client actually has a secret, and values are correct.

---

### 4) Status code assertion failures (API behavior differences)
**Symptoms**
- Error: `<Name> failed. Expected status X, got Y.`

**What to do**
1. Re-run the failing request manually (Postman/curl) and inspect response body.
2. Check for:
   - Changed routes
   - Authorization requirements
   - Validation rules
   - Seed data differences (products list empty)

In particular:
- The script expects `GET /api/products` to return at least **1 product**.
- If your ProductService doesn’t seed data in dev, you may need to seed it or adjust the test.

---

### 5) Script exits immediately: missing Cognito config
**Symptoms**
- Error: `Missing Cognito values in src/UserService/appsettings.Development.json`

**Fix**
- Populate these fields in `src/UserService/appsettings.Development.json`:
  - `Cognito__ClientId`
  - `Cognito__ClientSecret`
  - `Cognito__UserPoolId`
  - `Cognito__Region`

## Notes / customization

- **Timeouts**: request timeout is 30s (`Invoke-WebRequest -TimeoutSec 30`).
- **Service startup**: uses `dotnet run --no-build`. If you’re changing code frequently, build first.
- **Connection strings and ports** are hard-coded near the top of the script and can be edited for your environment.
