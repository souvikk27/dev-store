# Authentication Implementation Summary

## Overview

Complete authentication system with JWT tokens, refresh tokens, API keys, and logout functionality.

## Features Implemented

### 1. Login Endpoint with Grant Types

**Endpoint:** `POST /api/v1/auth/login`

**Grant Types (via header):**

- `grant_type: refresh_token` (default) - Returns JWT access token + refresh token
- `grant_type: api_key` - Returns long-lived API key

**Request:**

```json
{
  "emailOrUsername": "user@example.com",
  "password": "password123",
  "deviceInfo": "Chrome on Windows"
}
```

**Response for `refresh_token`:**

```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "base64-encoded-token",
  "accessTokenExpiresAt": "2026-01-25T12:00:00Z",
  "refreshTokenExpiresAt": "2026-02-01T11:00:00Z",
  "user": {
    "id": "guid",
    "userName": "testuser",
    "email": "user@example.com",
    "firstName": "Test",
    "lastName": "User",
    "emailConfirmed": false
  }
}
```

**Response for `api_key`:**

```json
{
  "apiKey": "isk_randombase64string",
  "expiresAt": "2027-01-25T11:00:00Z",
  "user": {
    "id": "guid",
    "userName": "testuser",
    "email": "user@example.com",
    "firstName": "Test",
    "lastName": "User",
    "emailConfirmed": false
  }
}
```

### 2. Logout Endpoint

**Endpoint:** `POST /api/v1/auth/logout`

**Authentication:** Requires Bearer token

**Options:**

- Logout current device (provide refreshToken)
- Logout all devices (set logoutAllDevices: true)

**Request (Current Device):**

```json
{
  "refreshToken": "your-refresh-token"
}
```

**Request (All Devices):**

```json
{
  "logoutAllDevices": true
}
```

**Response:**

```json
{
  "message": "Logged out successfully"
}
```

### 3. Refresh Token Endpoint

**Endpoint:** `POST /api/v1/auth/refresh-token`

**Request:**

```json
{
  "accessToken": "expired-access-token",
  "refreshToken": "valid-refresh-token"
}
```

**Response:**

```json
{
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "accessTokenExpiresAt": "2026-01-25T12:00:00Z",
  "refreshTokenExpiresAt": "2026-02-01T11:00:00Z"
}
```

## Security Features

### Login Security

- Password verification with hashing
- Failed login attempt tracking
- Account lockout after 5 failed attempts (30 minutes)
- Account status validation (active/inactive)
- Device and IP tracking

### Token Security

- JWT tokens with configurable expiration
- Refresh token rotation (old token marked as used)
- Token revocation support
- JWT ID matching for refresh tokens
- Secure token storage in database

### Session Management

- User session tracking per device
- Session termination on logout
- Active session monitoring
- Device info and IP address logging

## Configuration

### appsettings.json

```json
{
  "JwtSettings": {
    "Authority": "https://localhost:5001",
    "Audience": "intellidevstore-api",
    "Issuer": "intellidevstore",
    "SecretKey": "your-secret-key-min-32-characters-long",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ApiKeyExpirationDays": 365
  }
}
```

## Files Created/Modified

### Services

- `IJwtTokenService.cs` - JWT token operations interface
- `JwtTokenService.cs` - JWT token generation and validation
- `IApiKeyService.cs` - API key operations interface
- `ApiKeyService.cs` - API key generation and management

### Commands & Handlers

- `LoginCommand.cs` - Login with grant type support
- `LogoutCommand.cs` - Logout with device options
- `RefreshTokenCommand.cs` - Token refresh logic

### Contracts (DTOs)

- `LoginRequest.cs` - Login credentials
- `LoginResponse.cs` - JWT token response
- `ApiKeyLoginResponse.cs` - API key response
- `LogoutRequest.cs` - Logout options
- `RefreshTokenRequest.cs` - Token refresh request
- `RefreshTokenResponse.cs` - New tokens response

### Endpoints

- `UserEndpoints.cs` - Updated with login, logout, and refresh endpoints

## Usage Examples

### 1. Register User

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "userName": "testuser",
  "email": "test@example.com",
  "password": "Test@123456",
  "firstName": "Test",
  "lastName": "User"
}
```

### 2. Login with JWT (Default)

```http
POST /api/v1/auth/login
Content-Type: application/json
grant_type: refresh_token

{
  "emailOrUsername": "test@example.com",
  "password": "Test@123456"
}
```

### 3. Login with API Key

```http
POST /api/v1/auth/login
Content-Type: application/json
grant_type: api_key

{
  "emailOrUsername": "test@example.com",
  "password": "Test@123456"
}
```

### 4. Refresh Access Token

```http
POST /api/v1/auth/refresh-token
Content-Type: application/json

{
  "accessToken": "expired-token",
  "refreshToken": "valid-refresh-token"
}
```

### 5. Logout Current Device

```http
POST /api/v1/auth/logout
Content-Type: application/json
Authorization: Bearer your-access-token

{
  "refreshToken": "your-refresh-token"
}
```

### 6. Logout All Devices

```http
POST /api/v1/auth/logout
Content-Type: application/json
Authorization: Bearer your-access-token

{
  "logoutAllDevices": true
}
```

## Database Tables Used

- `Users` - User accounts
- `PlatformRefreshTokens` - Refresh tokens and API keys
- `UserSessions` - Active user sessions

## Token Expiration Defaults

- Access Token: 60 minutes
- Refresh Token: 7 days
- API Key: 365 days

## Next Steps

1. Test all endpoints using the `.http` file
2. Update JWT secret key in production
3. Consider adding email verification
4. Implement password reset functionality
5. Add role-based authorization
6. Consider adding MFA support
