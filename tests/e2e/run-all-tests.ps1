$ErrorActionPreference = "Stop"

function Assert-StatusCode {
    param(
        [Parameter(Mandatory = $true)] [string]$Name,
        [Parameter(Mandatory = $true)] [int]$Actual,
        [Parameter(Mandatory = $true)] [int]$Expected
    )

    if ($Actual -ne $Expected) {
        throw "$Name failed. Expected status $Expected, got $Actual."
    }

    Write-Output ("PASS: {0} ({1})" -f $Name, $Actual)
}

function Invoke-ExpectStatus {
    param(
        [Parameter(Mandatory = $true)] [string]$Name,
        [Parameter(Mandatory = $true)] [string]$Method,
        [Parameter(Mandatory = $true)] [string]$Uri,
        [Parameter(Mandatory = $true)] [int]$ExpectedStatus,
        [hashtable]$Headers,
        [string]$ContentType,
        [string]$Body
    )

    $params = @{
        Method = $Method
        Uri = $Uri
        UseBasicParsing = $true
        TimeoutSec = 30
    }

    if ($Headers) {
        $params["Headers"] = $Headers
    }

    if (-not [string]::IsNullOrWhiteSpace($ContentType)) {
        $params["ContentType"] = $ContentType
    }

    if (-not [string]::IsNullOrWhiteSpace($Body)) {
        $params["Body"] = $Body
    }

    try {
        $resp = Invoke-WebRequest @params
        Assert-StatusCode -Name $Name -Actual ([int]$resp.StatusCode) -Expected $ExpectedStatus
        return $resp
    }
    catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode.value__
            Assert-StatusCode -Name $Name -Actual $status -Expected $ExpectedStatus
            return $null
        }

        throw
    }
}

function Wait-Healthy {
    param(
        [Parameter(Mandatory = $true)] [string]$Name,
        [Parameter(Mandatory = $true)] [string]$Url
    )

    for ($i = 0; $i -lt 80; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            if ([int]$resp.StatusCode -eq 200) {
                Write-Output ("PASS: {0} healthy" -f $Name)
                return
            }
        }
        catch {}

        Start-Sleep -Milliseconds 500
    }

    throw "$Name did not become healthy at $Url"
}

function Get-DotEnvMap {
    param(
        [Parameter(Mandatory = $true)] [string]$Path
    )

    if (-not (Test-Path $Path)) {
        throw ".env file not found: $Path"
    }

    $map = @{}
    foreach ($line in Get-Content $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        if ($trimmed.StartsWith("export ")) {
            $trimmed = $trimmed.Substring(7).Trim()
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Length -ne 2) {
            continue
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"')
        if (-not [string]::IsNullOrWhiteSpace($key)) {
            $map[$key] = $value
        }
    }

    return $map
}

$repo = "c:\Users\user\Desktop\Projects\Large\Assessements_and_Experiments\Experiments\E-Commerce-Microservice-Demo"
Set-Location $repo

# Service URLs for this test run
$userBase = "http://localhost:5204"
$productBase = "http://localhost:5002"
$orderBase = "http://localhost:5136"

# DB connections
$userDb = "Host=localhost;Port=5432;Database=user_service_db;Username=postgres;Password=yourpassword"
$productDb = "Host=localhost;Port=5432;Database=product_service_db;Username=postgres;Password=yourpassword"
$orderDb = "Host=localhost;Port=5432;Database=order_service_db;Username=postgres;Password=yourpassword"

# Cognito settings are sourced from UserService .env
$userEnv = Get-DotEnvMap -Path "src/UserService/.env"
$clientId = $userEnv["Cognito__ClientId"]
$clientSecret = $userEnv["Cognito__ClientSecret"]
$userPoolId = $userEnv["Cognito__UserPoolId"]
$region = $userEnv["Cognito__Region"]

if ([string]::IsNullOrWhiteSpace($clientId) -or [string]::IsNullOrWhiteSpace($clientSecret) -or [string]::IsNullOrWhiteSpace($userPoolId) -or [string]::IsNullOrWhiteSpace($region)) {
    throw "Missing Cognito values in src/UserService/.env"
}

$env:ASPNETCORE_ENVIRONMENT = "Development"

# Start services
$userProc = Start-Process dotnet -ArgumentList "run --no-build --project src/UserService --urls $userBase --ConnectionStrings__UserDb $userDb" -WorkingDirectory $repo -PassThru
$productProc = Start-Process dotnet -ArgumentList "run --no-build --project src/ProductService --urls $productBase --ConnectionStrings__ProductDb $productDb" -WorkingDirectory $repo -PassThru
$orderProc = Start-Process dotnet -ArgumentList "run --no-build --project src/OrderService --urls $orderBase --ConnectionStrings__OrderDb $orderDb --ServiceUrls__ProductService $productBase" -WorkingDirectory $repo -PassThru

try {
    # Health checks
    Wait-Healthy -Name "UserService" -Url "$userBase/api/users/health"
    Wait-Healthy -Name "ProductService" -Url "$productBase/api/products/health"
    Wait-Healthy -Name "OrderService" -Url "$orderBase/api/orders/health"

    # Registration
    $suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $email = "e2e.$suffix@example.com"
    $password = "Aa12345!!Bb"
    $registerBody = @{
        name = "E2E User"
        email = $email
        password = $password
    } | ConvertTo-Json

    $registerResp = Invoke-ExpectStatus -Name "User Register" -Method "POST" -Uri "$userBase/api/users/register" -ExpectedStatus 201 -ContentType "application/json" -Body $registerBody
    $registeredUser = $registerResp.Content | ConvertFrom-Json

    # Confirm user (required before auth in many Cognito setups)
    aws cognito-idp admin-confirm-sign-up --user-pool-id $userPoolId --username $email --region $region | Out-Null

    # Get Cognito access token
    $username = $email.ToLowerInvariant()
    $msg = $username + $clientId
    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($clientSecret))
    $secretHash = [Convert]::ToBase64String($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($msg)))
    $authParams = "USERNAME=$username,PASSWORD=$password,SECRET_HASH=$secretHash"
    $authJson = aws cognito-idp initiate-auth --auth-flow USER_PASSWORD_AUTH --client-id $clientId --auth-parameters $authParams --region $region --output json
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to get Cognito token with USER_PASSWORD_AUTH."
    }

    $auth = $authJson | ConvertFrom-Json
    $accessToken = $auth.AuthenticationResult.AccessToken
    if ([string]::IsNullOrWhiteSpace($accessToken)) {
        throw "No AccessToken in Cognito auth response."
    }

    $authHeaders = @{ Authorization = "Bearer $accessToken" }

    # User endpoint tests
    Invoke-ExpectStatus -Name "User Get Unauthorized" -Method "GET" -Uri "$userBase/api/users/$($registeredUser.id)" -ExpectedStatus 401 | Out-Null
    Invoke-ExpectStatus -Name "User Get Authorized" -Method "GET" -Uri "$userBase/api/users/$($registeredUser.id)" -ExpectedStatus 200 -Headers $authHeaders | Out-Null

    # Product endpoint tests
    $allProductsResp = Invoke-ExpectStatus -Name "Products GetAll" -Method "GET" -Uri "$productBase/api/products" -ExpectedStatus 200
    $allProducts = $allProductsResp.Content | ConvertFrom-Json
    if ($allProducts.Count -lt 1) {
        throw "Products GetAll returned no products."
    }

    Invoke-ExpectStatus -Name "Products Filter Phones" -Method "GET" -Uri "$productBase/api/products?category=Phones" -ExpectedStatus 200 | Out-Null
    Invoke-ExpectStatus -Name "Products GetById" -Method "GET" -Uri "$productBase/api/products/$($allProducts[0].id)" -ExpectedStatus 200 | Out-Null
    Invoke-ExpectStatus -Name "Products Create Unauthorized" -Method "POST" -Uri "$productBase/api/products" -ExpectedStatus 401 -ContentType "application/json" -Body (@{
        name = "Unauthorized Product"
        description = "Should fail"
        price = 1
        category = "Accessories"
        stockQuantity = 1
    } | ConvertTo-Json) | Out-Null

    $createProductResp = Invoke-ExpectStatus -Name "Products Create Authorized" -Method "POST" -Uri "$productBase/api/products" -ExpectedStatus 201 -Headers $authHeaders -ContentType "application/json" -Body (@{
        name = "E2E Product"
        description = "Created by E2E test"
        price = 49.99
        category = "Accessories"
        imageUrl = "https://example.com/images/e2e-product.jpg"
        stockQuantity = 25
    } | ConvertTo-Json)
    $createdProduct = $createProductResp.Content | ConvertFrom-Json

    $updateProductResp = Invoke-ExpectStatus -Name "Products Update Authorized" -Method "PUT" -Uri "$productBase/api/products/$($createdProduct.id)" -ExpectedStatus 200 -Headers $authHeaders -ContentType "application/json" -Body (@{
        price = 39.99
        stockQuantity = 30
    } | ConvertTo-Json)
    $updatedProduct = $updateProductResp.Content | ConvertFrom-Json
    if ([decimal]$updatedProduct.price -ne [decimal]39.99) {
        throw "Products Update Authorized failed: expected price 39.99"
    }

    Invoke-ExpectStatus -Name "Products Delete Authorized" -Method "DELETE" -Uri "$productBase/api/products/$($createdProduct.id)" -ExpectedStatus 204 -Headers $authHeaders | Out-Null
    Invoke-ExpectStatus -Name "Products Delete Missing" -Method "DELETE" -Uri "$productBase/api/products/$($createdProduct.id)" -ExpectedStatus 404 -Headers $authHeaders | Out-Null

    # Order endpoint tests
    $seedProduct = $allProducts[0]
    $missingProductId = [Guid]::NewGuid()
    $testUserId = [Guid]::NewGuid()

    Invoke-ExpectStatus -Name "Orders Create Unauthorized" -Method "POST" -Uri "$orderBase/api/orders" -ExpectedStatus 401 -ContentType "application/json" -Body (@{
        userId = $testUserId
        productId = $seedProduct.id
        quantity = 1
    } | ConvertTo-Json) | Out-Null

    Invoke-ExpectStatus -Name "Orders Create Missing Product" -Method "POST" -Uri "$orderBase/api/orders" -ExpectedStatus 404 -Headers $authHeaders -ContentType "application/json" -Body (@{
        userId = $testUserId
        productId = $missingProductId
        quantity = 1
    } | ConvertTo-Json) | Out-Null

    Invoke-ExpectStatus -Name "Orders Create Insufficient Stock" -Method "POST" -Uri "$orderBase/api/orders" -ExpectedStatus 400 -Headers $authHeaders -ContentType "application/json" -Body (@{
        userId = $testUserId
        productId = $seedProduct.id
        quantity = ([int]$seedProduct.stockQuantity + 1)
    } | ConvertTo-Json) | Out-Null

    $createOrderResp = Invoke-ExpectStatus -Name "Orders Create Valid" -Method "POST" -Uri "$orderBase/api/orders" -ExpectedStatus 201 -Headers $authHeaders -ContentType "application/json" -Body (@{
        userId = $testUserId
        productId = $seedProduct.id
        quantity = 1
    } | ConvertTo-Json)
    $createdOrder = $createOrderResp.Content | ConvertFrom-Json

    Invoke-ExpectStatus -Name "Orders GetById" -Method "GET" -Uri "$orderBase/api/orders/$($createdOrder.id)" -ExpectedStatus 200 -Headers $authHeaders | Out-Null

    $ordersByUserResp = Invoke-ExpectStatus -Name "Orders GetByUserId" -Method "GET" -Uri "$orderBase/api/orders/user/$testUserId" -ExpectedStatus 200 -Headers $authHeaders
    $ordersByUser = $ordersByUserResp.Content | ConvertFrom-Json
    if ($ordersByUser.Count -lt 1) {
        throw "Orders GetByUserId returned empty array after creation."
    }

    Invoke-ExpectStatus -Name "Orders Patch Status" -Method "PATCH" -Uri "$orderBase/api/orders/$($createdOrder.id)/status" -ExpectedStatus 200 -Headers $authHeaders -ContentType "application/json" -Body (@{
        status = 1
    } | ConvertTo-Json) | Out-Null

    Invoke-ExpectStatus -Name "Orders Patch Missing" -Method "PATCH" -Uri "$orderBase/api/orders/$([Guid]::NewGuid())/status" -ExpectedStatus 404 -Headers $authHeaders -ContentType "application/json" -Body (@{
        status = 2
    } | ConvertTo-Json) | Out-Null

    Write-Output "ALL TESTS PASSED"
}
finally {
    if ($orderProc -and -not $orderProc.HasExited) { Stop-Process -Id $orderProc.Id -Force }
    if ($productProc -and -not $productProc.HasExited) { Stop-Process -Id $productProc.Id -Force }
    if ($userProc -and -not $userProc.HasExited) { Stop-Process -Id $userProc.Id -Force }
}
