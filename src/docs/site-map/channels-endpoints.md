# Channels API Endpoints

The Channels API provides endpoints for managing notification delivery channels, monitoring their health status, and testing connectivity. This API is essential for administrators who need to configure and maintain notification delivery infrastructure.

## Table of Contents

- [Overview](#overview)
- [Channel Types](#channel-types)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
  - [Get All Channel Statuses](#get-all-channel-statuses)
  - [Get All Channel Configurations](#get-all-channel-configurations)
  - [Get Specific Channel Configuration](#get-specific-channel-configuration)
  - [Update Channel Configuration](#update-channel-configuration)
  - [Enable Channel](#enable-channel)
  - [Disable Channel](#disable-channel)
  - [Test Channel Connectivity](#test-channel-connectivity)
- [Data Models](#data-models)
- [Common Use Cases](#common-use-cases)
- [Troubleshooting](#troubleshooting)

---

## Overview

The Channels API manages four notification delivery channels:
- **SignalR**: Real-time browser notifications
- **Email**: Email delivery via SMTP or Microsoft Graph
- **SMS**: Text messages via Twilio
- **Teams**: Microsoft Teams messages via webhooks

Each channel can be independently configured, enabled/disabled, and tested for connectivity.

**Base URL**: `http://localhost:5201/api/channels`

---

## Channel Types

| Channel | Description | Configuration Requirements | Status |
|---------|-------------|---------------------------|--------|
| **SignalR** | Real-time push notifications to connected browser clients | Read-only, always available | Active |
| **Email** | Email delivery using SMTP or Microsoft Graph | SMTP credentials or Graph API configuration | Configurable |
| **SMS** | Text message delivery via Twilio | Twilio Account SID, Auth Token, Phone Number | Configurable |
| **Teams** | Microsoft Teams channel messages | Webhook URL, Channel Name | Configurable |

---

## Authentication

**Admin Access Required**: All endpoints in this API require administrator privileges. Ensure your authentication token has the appropriate role claims before accessing these endpoints.

```http
Authorization: Bearer <your-jwt-token>
```

---

## Endpoints

### Get All Channel Statuses

Retrieve the health status and configuration state of all notification channels.

**Endpoint**: `GET /api/channels/status`

**Use Case**: Monitor overall system health, view which channels are operational, identify channels with recent errors.

#### Request

No request body required.

```http
GET /api/channels/status HTTP/1.1
Host: localhost:5201
```

#### Response

**Status Code**: `200 OK`

```json
[
  {
    "channel": "Email",
    "enabled": true,
    "configured": true,
    "status": "healthy",
    "lastDeliveryAt": "2025-12-18T14:32:15Z",
    "errorCount24h": 0,
    "config": {
      "provider": "graph",
      "fromAddress": "notifications@example.com",
      "replyToAddress": "support@example.com",
      "enableSsl": true
    }
  },
  {
    "channel": "SMS",
    "enabled": true,
    "configured": true,
    "status": "degraded",
    "lastDeliveryAt": "2025-12-18T12:15:00Z",
    "errorCount24h": 3,
    "config": {
      "fromPhoneNumber": "+1555000****"
    }
  },
  {
    "channel": "SignalR",
    "enabled": true,
    "configured": true,
    "status": "healthy",
    "lastDeliveryAt": "2025-12-18T14:35:00Z",
    "errorCount24h": 0,
    "config": {
      "hubUrl": "/hubs/notifications",
      "autoReconnect": true
    }
  },
  {
    "channel": "Teams",
    "enabled": false,
    "configured": false,
    "status": "unhealthy",
    "lastDeliveryAt": null,
    "errorCount24h": 0,
    "config": null
  }
]
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `channel` | string | Channel name (Email, SMS, SignalR, Teams) |
| `enabled` | boolean | Whether the channel is currently enabled |
| `configured` | boolean | Whether required configuration is present |
| `status` | string | Health status: "healthy", "degraded", or "unhealthy" |
| `lastDeliveryAt` | string (ISO 8601) | Timestamp of last successful delivery |
| `errorCount24h` | integer | Number of errors in the last 24 hours |
| `config` | object | Channel-specific configuration (sensitive data masked) |

#### Health Status Definitions

- **healthy**: Channel is operational with no recent errors
- **degraded**: Channel is operational but experiencing intermittent errors
- **unhealthy**: Channel is not operational or has critical configuration issues

#### Error Responses

**500 Internal Server Error**
```json
{
  "error": "Failed to retrieve channel status"
}
```

---

### Get All Channel Configurations

Retrieve detailed configuration for all channels. Sensitive data (passwords, tokens) is masked in the response.

**Endpoint**: `GET /api/channels/config`

**Use Case**: Review all channel configurations, audit settings, export configuration for documentation.

#### Request

```http
GET /api/channels/config HTTP/1.1
Host: localhost:5201
```

#### Response

**Status Code**: `200 OK`

```json
[
  {
    "channel": "Email",
    "enabled": true,
    "configured": true,
    "config": {
      "provider": "graph",
      "smtpHost": null,
      "smtpPort": 587,
      "smtpUsername": null,
      "smtpPassword": "****",
      "fromAddress": "notifications@example.com",
      "replyToAddress": "support@example.com",
      "enableSsl": true
    },
    "lastTestedAt": "2025-12-18T10:00:00Z",
    "testStatus": "success",
    "testError": null
  },
  {
    "channel": "SMS",
    "enabled": true,
    "configured": true,
    "config": {
      "accountSid": "AC****",
      "authToken": "****",
      "fromPhoneNumber": "+1555000****"
    },
    "lastTestedAt": "2025-12-17T15:30:00Z",
    "testStatus": "success",
    "testError": null
  }
]
```

#### Error Responses

**500 Internal Server Error**
```json
{
  "error": "Failed to retrieve channel configurations"
}
```

---

### Get Specific Channel Configuration

Retrieve configuration for a single channel.

**Endpoint**: `GET /api/channels/{channel}/config`

**URL Parameters**:
- `{channel}`: Channel name (case-insensitive). Valid values: `SignalR`, `Email`, `SMS`, `Teams`

**Use Case**: Review configuration before making changes, verify current settings for a specific channel.

#### Request

```http
GET /api/channels/Email/config HTTP/1.1
Host: localhost:5201
```

#### Response

**Status Code**: `200 OK`

```json
{
  "channel": "Email",
  "enabled": true,
  "configured": true,
  "config": {
    "provider": "smtp",
    "smtpHost": "smtp.gmail.com",
    "smtpPort": 587,
    "smtpUsername": "notifications@example.com",
    "smtpPassword": "****",
    "fromAddress": "notifications@example.com",
    "replyToAddress": "support@example.com",
    "enableSsl": true
  },
  "lastTestedAt": "2025-12-18T10:00:00Z",
  "testStatus": "success",
  "testError": null
}
```

#### Error Responses

**400 Bad Request** - Invalid channel name
```json
{
  "error": "Invalid channel: InvalidName"
}
```

**500 Internal Server Error**
```json
{
  "error": "Failed to retrieve channel configuration"
}
```

---

### Update Channel Configuration

Update configuration for a specific channel. Only provide fields you want to change.

**Endpoint**: `PUT /api/channels/{channel}/config`

**URL Parameters**:
- `{channel}`: Channel name (case-insensitive)

**Use Case**: Configure new SMTP settings, update Twilio credentials, change email from address.

#### Request Body Schema

All fields are optional. Only provide the fields you want to update.

```json
{
  "enabled": true,

  // Email channel fields
  "provider": "smtp",
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "smtpUsername": "notifications@example.com",
  "smtpPassword": "app-specific-password",
  "fromAddress": "notifications@example.com",
  "replyToAddress": "support@example.com",
  "enableSsl": true,

  // SMS channel fields (Twilio)
  "accountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "authToken": "your-auth-token",
  "fromPhoneNumber": "+15555551234",

  // Teams channel fields
  "webhookUrl": "https://outlook.office.com/webhook/...",
  "channelName": "Notifications"
}
```

#### Example: Update Email SMTP Settings

```http
PUT /api/channels/Email/config HTTP/1.1
Host: localhost:5201
Content-Type: application/json

{
  "enabled": true,
  "provider": "smtp",
  "smtpHost": "smtp.gmail.com",
  "smtpPort": 587,
  "smtpUsername": "notifications@example.com",
  "smtpPassword": "app-specific-password",
  "fromAddress": "notifications@example.com",
  "enableSsl": true
}
```

#### Example: Switch Email to Microsoft Graph

```http
PUT /api/channels/Email/config HTTP/1.1
Host: localhost:5201
Content-Type: application/json

{
  "enabled": true,
  "provider": "graph",
  "fromAddress": "notifications@example.com",
  "replyToAddress": "support@example.com"
}
```

#### Example: Configure SMS Channel

```http
PUT /api/channels/SMS/config HTTP/1.1
Host: localhost:5201
Content-Type: application/json

{
  "enabled": true,
  "accountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "authToken": "your-twilio-auth-token",
  "fromPhoneNumber": "+15555551234"
}
```

#### Example: Configure Teams Channel

```http
PUT /api/channels/Teams/config HTTP/1.1
Host: localhost:5201
Content-Type: application/json

{
  "enabled": true,
  "webhookUrl": "https://outlook.office.com/webhook/abc123...",
  "channelName": "Development Notifications"
}
```

#### Response

**Status Code**: `200 OK`

```json
{
  "channel": "Email",
  "enabled": true,
  "configured": true,
  "config": {
    "provider": "smtp",
    "smtpHost": "smtp.gmail.com",
    "smtpPort": 587,
    "smtpUsername": "notifications@example.com",
    "smtpPassword": "****",
    "fromAddress": "notifications@example.com",
    "replyToAddress": "support@example.com",
    "enableSsl": true
  },
  "lastTestedAt": null,
  "testStatus": null,
  "testError": null
}
```

#### Error Responses

**400 Bad Request**
```json
{
  "error": "Invalid channel: InvalidName"
}
```

**500 Internal Server Error**
```json
{
  "error": "Failed to update channel configuration"
}
```

---

### Enable Channel

Enable a notification channel. The channel must be properly configured before enabling.

**Endpoint**: `POST /api/channels/{channel}/enable`

**URL Parameters**:
- `{channel}`: Channel name (case-insensitive)

**Use Case**: Activate a channel after configuration, re-enable a channel after maintenance.

#### Request

```http
POST /api/channels/Email/enable HTTP/1.1
Host: localhost:5201
```

#### Response

**Status Code**: `200 OK`

```json
{
  "message": "Channel Email enabled"
}
```

#### Error Responses

**400 Bad Request**
```json
{
  "error": "Invalid channel: InvalidName"
}
```

**500 Internal Server Error**
```json
{
  "error": "Failed to enable channel"
}
```

---

### Disable Channel

Disable a notification channel. Notifications for this channel will be skipped until re-enabled.

**Endpoint**: `POST /api/channels/{channel}/disable`

**URL Parameters**:
- `{channel}`: Channel name (case-insensitive)

**Use Case**: Temporarily disable a channel for maintenance, stop notifications through a specific channel.

#### Request

```http
POST /api/channels/SMS/disable HTTP/1.1
Host: localhost:5201
```

#### Response

**Status Code**: `200 OK`

```json
{
  "message": "Channel SMS disabled"
}
```

#### Error Responses

**400 Bad Request**
```json
{
  "error": "Invalid channel: InvalidName"
}
```

**500 Internal Server Error**
```json
{
  "error": "Failed to disable channel"
}
```

---

### Test Channel Connectivity

Test a channel's connectivity and configuration without sending actual notifications.

**Endpoint**: `POST /api/channels/{channel}/test`

**URL Parameters**:
- `{channel}`: Channel name (case-insensitive)

**Use Case**: Verify configuration after changes, diagnose connectivity issues, confirm credentials are valid.

#### Request

```http
POST /api/channels/Email/test HTTP/1.1
Host: localhost:5201
```

#### Response - Success

**Status Code**: `200 OK`

```json
{
  "success": true,
  "message": "Channel Email test successful",
  "details": "Successfully connected to SMTP server at smtp.gmail.com:587"
}
```

#### Response - Failure

**Status Code**: `200 OK`

```json
{
  "success": false,
  "message": "Channel Email test failed",
  "details": "SMTP authentication failed: Invalid credentials"
}
```

#### Error Responses

**400 Bad Request**
```json
{
  "error": "Invalid channel: InvalidName"
}
```

**500 Internal Server Error**
```json
{
  "error": "Failed to test channel"
}
```

---

## Data Models

### ChannelConfigurationResponse

Complete channel configuration with masked sensitive data.

```typescript
{
  channel: string;           // Channel name
  enabled: boolean;          // Whether channel is enabled
  configured: boolean;       // Whether required config is present
  config: object | null;     // Channel-specific configuration
  lastTestedAt: string | null;  // ISO 8601 timestamp
  testStatus: string | null;     // "success" or "failure"
  testError: string | null;      // Error message from last test
}
```

### UpdateChannelConfigurationRequest

Request body for updating channel configuration. All fields are optional.

```typescript
{
  enabled?: boolean;

  // Email fields
  provider?: string;         // "smtp" or "graph"
  smtpHost?: string;
  smtpPort?: number;
  smtpUsername?: string;
  smtpPassword?: string;
  fromAddress?: string;
  replyToAddress?: string;
  enableSsl?: boolean;

  // SMS fields
  accountSid?: string;
  authToken?: string;
  fromPhoneNumber?: string;

  // Teams fields
  webhookUrl?: string;
  channelName?: string;
}
```

### TestChannelResult

Result of testing channel connectivity.

```typescript
{
  success: boolean;          // Whether test succeeded
  message: string;           // Human-readable result message
  details: string | null;    // Additional diagnostic information
}
```

### ChannelHealthStatus

Current operational status of a channel.

```typescript
{
  channel: string;           // Channel name
  enabled: boolean;          // Whether channel is enabled
  configured: boolean;       // Whether required config exists
  status: "healthy" | "degraded" | "unhealthy";
  lastDeliveryAt: string | null;  // ISO 8601 timestamp
  errorCount24h: number;          // Errors in last 24 hours
  config: object | null;          // Masked configuration
}
```

---

## Common Use Cases

### Initial Email Setup with SMTP

1. **Update configuration** with SMTP settings:
```bash
curl -X PUT http://localhost:5201/api/channels/Email/config \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "smtp",
    "smtpHost": "smtp.gmail.com",
    "smtpPort": 587,
    "smtpUsername": "notifications@example.com",
    "smtpPassword": "app-password",
    "fromAddress": "notifications@example.com",
    "enableSsl": true
  }'
```

2. **Test the connection**:
```bash
curl -X POST http://localhost:5201/api/channels/Email/test
```

3. **Enable the channel**:
```bash
curl -X POST http://localhost:5201/api/channels/Email/enable
```

4. **Verify status**:
```bash
curl http://localhost:5201/api/channels/status
```

### Switch from SMTP to Microsoft Graph

1. **Update to Graph provider**:
```bash
curl -X PUT http://localhost:5201/api/channels/Email/config \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "graph",
    "fromAddress": "notifications@example.com"
  }'
```

2. **Test and enable**:
```bash
curl -X POST http://localhost:5201/api/channels/Email/test
curl -X POST http://localhost:5201/api/channels/Email/enable
```

### Configure SMS with Twilio

1. **Update SMS configuration**:
```bash
curl -X PUT http://localhost:5201/api/channels/SMS/config \
  -H "Content-Type: application/json" \
  -d '{
    "accountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "authToken": "your-auth-token",
    "fromPhoneNumber": "+15555551234"
  }'
```

2. **Test and enable**:
```bash
curl -X POST http://localhost:5201/api/channels/SMS/test
curl -X POST http://localhost:5201/api/channels/SMS/enable
```

### Temporarily Disable a Channel

```bash
# Disable for maintenance
curl -X POST http://localhost:5201/api/channels/SMS/disable

# Perform maintenance...

# Re-enable when ready
curl -X POST http://localhost:5201/api/channels/SMS/enable
```

### Monitor Channel Health

```bash
# Get all channel statuses
curl http://localhost:5201/api/channels/status | jq '.[] | select(.status != "healthy")'

# Check specific channel
curl http://localhost:5201/api/channels/Email/config
```

---

## Troubleshooting

### Channel shows "configured: false"

**Cause**: Required configuration fields are missing.

**Solution**:
1. Get current config: `GET /api/channels/{channel}/config`
2. Identify missing required fields for the channel type
3. Update with complete configuration: `PUT /api/channels/{channel}/config`

**Required Fields by Channel**:
- **Email (SMTP)**: `smtpHost`, `smtpPort`, `smtpUsername`, `smtpPassword`, `fromAddress`
- **Email (Graph)**: `fromAddress` (plus Azure AD app registration)
- **SMS**: `accountSid`, `authToken`, `fromPhoneNumber`
- **Teams**: `webhookUrl`

### Channel test fails with authentication error

**Cause**: Invalid credentials or expired tokens.

**Solution**:
1. Verify credentials in the external service (Gmail, Twilio, etc.)
2. For Gmail: Ensure you're using an App Password, not your regular password
3. For Twilio: Verify Account SID and Auth Token in Twilio console
4. Update configuration with valid credentials
5. Test again

### Channel shows "degraded" status

**Cause**: Recent delivery failures or intermittent connectivity issues.

**Solution**:
1. Check `errorCount24h` to see frequency of errors
2. Review application logs for detailed error messages
3. Test channel connectivity: `POST /api/channels/{channel}/test`
4. If test passes but degraded persists, monitor for additional errors
5. Consider temporarily disabling and re-enabling the channel

### Email delivery fails with SSL/TLS errors

**Cause**: Incorrect SSL/TLS settings for the SMTP server.

**Solution**:
1. For Office 365/Outlook: Use port 587 with `enableSsl: true`
2. For Gmail: Use port 587 with `enableSsl: true`
3. For other SMTP servers: Consult provider documentation
4. Update configuration with correct port and SSL settings
5. Test the connection

### SMS delivery fails with "Invalid phone number"

**Cause**: Phone number format doesn't match E.164 standard.

**Solution**:
1. Ensure `fromPhoneNumber` is in E.164 format: `+[country code][number]`
2. Example: US number should be `+15555551234`, not `555-555-1234`
3. Verify the number is purchased and active in Twilio
4. Update configuration with correctly formatted number

### Teams webhook returns 404

**Cause**: Webhook URL is invalid or has been regenerated.

**Solution**:
1. Go to Teams channel settings
2. Navigate to Connectors > Incoming Webhook
3. Copy the new webhook URL
4. Update configuration with new URL
5. Test the channel

### Channel enabled but notifications not sending

**Possible Causes**:
1. **Firewall/Network**: Outbound connections blocked
2. **Configuration**: Required fields missing or invalid
3. **Service**: External service (Gmail, Twilio) is down
4. **Preferences**: User preferences have channel disabled

**Diagnostic Steps**:
1. Check channel status: `GET /api/channels/status`
2. Verify `configured: true` and `enabled: true`
3. Test connectivity: `POST /api/channels/{channel}/test`
4. Review application logs for delivery attempts
5. Check user notification preferences
6. Verify external service status (status.twilio.com, etc.)

---

## Best Practices

### Security

1. **Rotate credentials regularly**: Update SMTP passwords, Twilio tokens periodically
2. **Use app-specific passwords**: For Gmail, use App Passwords instead of account password
3. **Limit webhook exposure**: Regenerate Teams webhooks if exposed
4. **Audit configuration changes**: Monitor who updates channel configurations

### Reliability

1. **Test after configuration changes**: Always run test endpoint after updates
2. **Monitor health regularly**: Check `/api/channels/status` periodically
3. **Set up alerting**: Alert when `errorCount24h` exceeds threshold
4. **Have fallback channels**: Configure multiple channels for critical notifications

### Performance

1. **Disable unused channels**: Disable channels you're not using to reduce overhead
2. **Monitor degraded channels**: Investigate and fix degraded channels promptly
3. **Use appropriate providers**: Use Graph API for high-volume email, SMTP for simpler needs

---

## Related Documentation

- [Notification Preferences API](./notification-preferences-endpoints.md) - User notification preferences
- [Health Check API](./health-endpoints.md) - Service health monitoring
- [Architecture Overview](../architecture/notification-service-architecture.md) - System architecture

---

**Last Updated**: 2025-12-18
**API Version**: 1.0
**Service**: NotificationService.Api
**Port**: 5201
