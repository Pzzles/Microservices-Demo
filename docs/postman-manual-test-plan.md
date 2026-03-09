# Postman Manual Test Plan (All Endpoints)

This guide gives you a practical, FE-oriented sequence to manually test every API endpoint with expected results.

## 1) Pre-Run Checklist

1. Start Docker/Postgres and confirm port `5432` is mapped.
2. Confirm these databases exist:
   - `user_service_db`
   - `product_service_db`
   - `order_service_db`
3. Confirm AWS CLI is authenticated:
   - `aws sts get-caller-identity`
4. From repo root, start the services exactly as the E2E script does:

```powershell
# Terminal 1
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --no-build --project src/UserService --urls "http://localhost:5204" --ConnectionStrings__UserDb "Host=localhost;Port=5432;Database=user_service_db;Username=postgres;Password=yourpassword"
```

```powershell
# Terminal 2
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --no-build --project src/ProductService --urls "http://localhost:5002" --ConnectionStrings__ProductDb "Host=localhost;Port=5432;Database=product_service_db;Username=postgres;Password=yourpassword"
```

```powershell
# Terminal 3
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --no-build --project src/OrderService --urls "http://localhost:5136" --ConnectionStrings__OrderDb "Host=localhost;Port=5432;Database=order_service_db;Username=postgres;Password=yourpassword" --ServiceUrls__ProductService "http://localhost:5002"
```

5. Verify health:
   - `GET http://localhost:5204/api/users/health` -> `200`, body `"Healthy"`
   - `GET http://localhost:5002/api/products/health` -> `200`, body `"Healthy"`
   - `GET http://localhost:5136/api/orders/health` -> `200`, body `"Healthy"`

## 2) Postman Environment Setup

Create these variables:

- `userBaseUrl` = `http://localhost:5204`
- `productBaseUrl` = `http://localhost:5002`
- `orderBaseUrl` = `http://localhost:5136`
- `email` = `e2e.{{$timestamp}}@example.com`
- `password` = `Aa12345!!Bb`
- `name` = `E2E User`
- `accessToken` = (empty initially)
- `userId` = (empty initially)
- `productId` = (empty initially)
- `orderId` = (empty initially)
- `testUserId` = `{{$guid}}`
- `missingProductId` = `{{$guid}}`
- `missingOrderId` = `{{$guid}}`

## 3) Get Bearer Token (for protected endpoints)

Primary demo path (FE token helper):

1. Register user via API (next section) and confirm the user in Cognito once.
2. Open `http://localhost:5204/token-helper.html`.
3. Enter email/password and click **Get Access Token**.
4. Copy the token and paste into Postman env var `accessToken`.

Alternative API-only path:

- `POST {{userBaseUrl}}/api/users/token` with:

```json
{
  "email": "{{email}}",
  "password": "{{password}}"
}
```

- Expected: `200` with body:

```json
{
  "accessToken": "<jwt>"
}
```

Use header on protected requests:

- `Authorization: Bearer {{accessToken}}`

## 4) FE-Oriented Test Flow

## A. User Service (`/api/users`)

1. **Health**
   - Request: `GET {{userBaseUrl}}/api/users/health`
   - Expected: `200`, body `"Healthy"`

2. **Register**
   - Request: `POST {{userBaseUrl}}/api/users/register`
   - Body:

```json
{
  "name": "{{name}}",
  "email": "{{email}}",
  "password": "{{password}}"
}
```

   - Expected: `201`
   - Expected body fields: `id`, `name`, `email`, `createdAt`
   - Save `id` as `userId`

3. **Get User Without Token**
   - Request: `GET {{userBaseUrl}}/api/users/{{userId}}`
   - Expected: `401`

4. **Get User With Token**
   - Request: `GET {{userBaseUrl}}/api/users/{{userId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Expected: `200`
   - Expected body: same `id`, `email` as registration result

5. **Get Token (Login)**
   - Request: `POST {{userBaseUrl}}/api/users/token`
   - Body:

```json
{
  "email": "{{email}}",
  "password": "{{password}}"
}
```

   - Expected: `200`
   - Expected body field: `accessToken`
   - Save `accessToken` to Postman env var

6. **Register Validation Failure**
   - Request: `POST {{userBaseUrl}}/api/users/register`
   - Body:

```json
{
  "name": "",
  "email": "not-an-email",
  "password": ""
}
```

   - Expected: `400`
   - Expected body: validation problem details

## B. Product Service (`/api/products`)

1. **Get All Products**
   - Request: `GET {{productBaseUrl}}/api/products`
   - Expected: `200`
   - Expected body: array with `>= 1` product
   - Save first product `id` as `productId`

2. **Filter Products by Category**
   - Request: `GET {{productBaseUrl}}/api/products?category=Phones`
   - Expected: `200`
   - Expected body: array (typically non-empty from seeded data)

3. **Get Product by ID**
   - Request: `GET {{productBaseUrl}}/api/products/{{productId}}`
   - Expected: `200`
   - Expected body includes: `id`, `name`, `price`, `stockQuantity`

4. **Create Product Without Token**
   - Request: `POST {{productBaseUrl}}/api/products`
   - Body:

```json
{
  "name": "Unauthorized Product",
  "description": "Should fail",
  "price": 1,
  "category": "Accessories",
  "stockQuantity": 1
}
```

   - Expected: `401`

5. **Create Product With Token**
   - Request: `POST {{productBaseUrl}}/api/products`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "name": "Postman Product",
  "description": "Created from Postman manual test",
  "price": 49.99,
  "category": "Accessories",
  "imageUrl": "https://example.com/images/postman-product.jpg",
  "stockQuantity": 25
}
```

   - Expected: `201`
   - Save returned `id` as `createdProductId`

6. **Update Product With Token**
   - Request: `PUT {{productBaseUrl}}/api/products/{{createdProductId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "price": 39.99,
  "stockQuantity": 30
}
```

   - Expected: `200`
   - Expected body fields changed: `price=39.99`, `stockQuantity=30`

7. **Delete Product With Token**
   - Request: `DELETE {{productBaseUrl}}/api/products/{{createdProductId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Expected: `204`

8. **Delete Missing Product**
   - Request: `DELETE {{productBaseUrl}}/api/products/{{createdProductId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Expected: `404`

9. **Create Product Validation Failure**
   - Request: `POST {{productBaseUrl}}/api/products`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "name": "",
  "description": "",
  "price": -1,
  "category": "",
  "stockQuantity": -5
}
```

   - Expected: `400`
   - Expected body: validation problem details

## C. Order Service (`/api/orders`)

Use `productId` from Product GetAll (seeded product) and `testUserId` (random guid env var).

1. **Create Order Without Token**
   - Request: `POST {{orderBaseUrl}}/api/orders`
   - Body:

```json
{
  "userId": "{{testUserId}}",
  "productId": "{{productId}}",
  "quantity": 1
}
```

   - Expected: `401`

2. **Create Order Missing Product**
   - Request: `POST {{orderBaseUrl}}/api/orders`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "userId": "{{testUserId}}",
  "productId": "{{missingProductId}}",
  "quantity": 1
}
```

   - Expected: `404`
   - Expected body: `{ "message": "Product not found" }`

3. **Create Order Insufficient Stock**
   - Request: `POST {{orderBaseUrl}}/api/orders`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body: set quantity larger than current stock

```json
{
  "userId": "{{testUserId}}",
  "productId": "{{productId}}",
  "quantity": 999999
}
```

   - Expected: `400`
   - Expected body: `{ "message": "Insufficient stock" }`

4. **Create Valid Order**
   - Request: `POST {{orderBaseUrl}}/api/orders`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "userId": "{{testUserId}}",
  "productId": "{{productId}}",
  "quantity": 1
}
```

   - Expected: `201`
   - Expected body fields: `id`, `userId`, `productId`, `productName`, `quantity`, `unitPrice`, `totalPrice`, `status`, `createdAt`
   - Save returned `id` as `orderId`
   - Expected initial `status`: `"Pending"`

5. **Get Order by ID**
   - Request: `GET {{orderBaseUrl}}/api/orders/{{orderId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Expected: `200`
   - Expected body `id` equals `orderId`

6. **Get Orders by User**
   - Request: `GET {{orderBaseUrl}}/api/orders/user/{{testUserId}}`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Expected: `200`
   - Expected body: array with at least one order

7. **Patch Order Status**
   - Request: `PATCH {{orderBaseUrl}}/api/orders/{{orderId}}/status`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "status": 1
}
```

   - Expected: `200`
   - Expected body field: `status = "Confirmed"`

8. **Patch Missing Order**
   - Request: `PATCH {{orderBaseUrl}}/api/orders/{{missingOrderId}}/status`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "status": 2
}
```

   - Expected: `404`

9. **Order Validation Failure**
   - Request: `POST {{orderBaseUrl}}/api/orders`
   - Header: `Authorization: Bearer {{accessToken}}`
   - Body:

```json
{
  "userId": "{{testUserId}}",
  "productId": "{{productId}}",
  "quantity": 0
}
```

   - Expected: `400`
   - Expected body: validation problem details

## 5) Status Code Summary by Endpoint

- `GET /api/users/health` -> `200`
- `POST /api/users/register` -> `201`, validation `400`
- `POST /api/users/token` -> `200/400`
- `GET /api/users/{id}` -> `200/401/404`

- `GET /api/products/health` -> `200`
- `GET /api/products` -> `200`
- `GET /api/products/{id}` -> `200/404`
- `POST /api/products` -> `201/401/400`
- `PUT /api/products/{id}` -> `200/401/404/400`
- `DELETE /api/products/{id}` -> `204/401/404`

- `GET /api/orders/health` -> `200`
- `POST /api/orders` -> `201/401/404/400`
- `GET /api/orders/{id}` -> `200/401/404`
- `GET /api/orders/user/{userId}` -> `200/401`
- `PATCH /api/orders/{id}/status` -> `200/401/404/400`

## 6) Quick FE Sanity Scenarios

1. Public catalog load:
   - `GET /api/products`
   - `GET /api/products?category=Phones`
2. Authenticated admin actions:
   - Create, update, delete product with Bearer token.
3. Checkout flow:
   - Create order, fetch order details, fetch order list by user, update status.
4. Error UX checks:
   - Unauthorized (`401`)
   - Missing resources (`404`)
   - Validation (`400`)
