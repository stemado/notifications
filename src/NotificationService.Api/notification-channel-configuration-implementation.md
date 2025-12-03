I'll create a plan to implement the Configure feature for notification channels. Let me first explore the existing codebase to understand the current architecture. 

Searched codebase for "notification channel configuration settings email SMS SignalR Teams", 29 results

Searched for files matching `**/NotificationService*/**/*.cs`, 20 matches

Searched for regex `ChannelHealth|ChannelStatus|ChannelConfig`, 20 results

Let me explore more of the existing channel configuration and the UI components: 

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/NotificationServices/src/NotificationService.Api/Controllers/ChannelsController.cs)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/NotificationServices/src/NotificationService.Infrastructure/Services/Channels/ChannelHealthService.cs)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/app/orchestration/notifications)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/app/orchestration/notifications/components)

Searched for files matching `**/orchestration/notifications/channels/**`, no matches

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/app/orchestration/notifications/components/ChannelStatusWidget.tsx)

Now let me check the existing notification types and API structure: 

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/types/notifications.ts)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/api/notifications)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/Cal.ImportPulse/app/operations/app)

Read [](file:///d%3A/Projects/PlanSourceAutomation-V2/NotificationServices/README.md)

Now I have a comprehensive understanding of the system. Let me create a detailed implementation plan for the Channel Configuration feature.

---

# 📋 Channel Configuration Feature Implementation Plan

## Overview

This plan outlines the implementation of the **Configure** feature for notification channels in the Cal.ImportPulse orchestration dashboard. The feature allows administrators to configure Email (SMTP), SMS (Twilio), and Microsoft Teams channels directly from the UI.

---

## Current State Analysis

### What Exists:
1. **Backend (`NotificationService.Api`)**:
   - ChannelsController.cs - Only has `GET /api/channels/status` endpoint
   - ChannelHealthService.cs - Reads configuration from environment variables
   - Channels are hardcoded to check environment variables (SMTP_HOST, TWILIO_ACCOUNT_SID, TEAMS_WEBHOOK_URL)

2. **Frontend (Cal.ImportPulse)**:
   - ChannelStatusWidget.tsx - Links to `/orchestration/notifications/channels` (route doesn't exist)
   - NotificationSettings.tsx - User preference settings (per-user, not admin config)
   - Types defined in notifications.ts including `ChannelStatus`

3. **Configuration Storage**:
   - Currently uses environment variables only
   - No database storage for channel configurations
   - No API endpoints for updating configurations

---

## Implementation Plan

### Phase 1: Backend - Database & Models

#### 1.1 Create Channel Configuration Entity
**File**: `NotificationService.Domain/Models/ChannelConfiguration.cs`

```csharp
public class ChannelConfiguration
{
    public Guid Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool Enabled { get; set; }
    public string ConfigurationJson { get; set; }  // Encrypted JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

#### 1.2 Channel-Specific Configuration DTOs
**Files**: `NotificationService.Domain/DTOs/ChannelConfigurations/`

- `EmailChannelConfig.cs` - SMTP settings
- `SmsChannelConfig.cs` - Twilio settings  
- `TeamsChannelConfig.cs` - Webhook settings
- `SignalRChannelConfig.cs` - Hub settings (read-only)

#### 1.3 Database Migration
**File**: `database/migrations/00X_create_channel_configurations.sql`

```sql
CREATE TABLE channel_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel VARCHAR(50) NOT NULL UNIQUE,
    enabled BOOLEAN NOT NULL DEFAULT false,
    configuration_json TEXT NOT NULL,  -- Encrypted
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(255)
);
```

---

### Phase 2: Backend - Repository & Service Layer

#### 2.1 Channel Configuration Repository
**File**: `NotificationService.Infrastructure/Repositories/ChannelConfigurationRepository.cs`

- `GetByChannelAsync(NotificationChannel channel)`
- `GetAllAsync()`
- `SaveAsync(ChannelConfiguration config)`
- `ValidateAndTestConnectionAsync(NotificationChannel, config)` - Test SMTP/Twilio/Teams

#### 2.2 Configuration Encryption Service
**File**: `NotificationService.Infrastructure/Services/ConfigurationEncryptionService.cs`

- Encrypt sensitive data (passwords, API keys) at rest
- Use Data Protection API or Azure Key Vault

#### 2.3 Update ChannelHealthService
**File**: ChannelHealthService.cs

- Read from database instead of environment variables
- Fall back to environment variables if database config not found
- Add configuration validation

---

### Phase 3: Backend - API Endpoints

#### 3.1 Expand ChannelsController
**File**: ChannelsController.cs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/channels/status` | Get all channel statuses (existing) |
| GET | `/api/channels/{channel}/config` | Get configuration (masked secrets) |
| PUT | `/api/channels/{channel}/config` | Update channel configuration |
| POST | `/api/channels/{channel}/test` | Test channel connectivity |
| POST | `/api/channels/{channel}/enable` | Enable channel |
| POST | `/api/channels/{channel}/disable` | Disable channel |

#### 3.2 Request/Response DTOs
**Files**: `NotificationService.Domain/DTOs/`

```typescript
// Example structures
UpdateEmailConfigRequest {
    smtpHost: string;
    smtpPort: number;
    smtpUsername: string;
    smtpPassword?: string;  // Only sent if changing
    fromEmail: string;
    fromName: string;
    enableSsl: boolean;
}

ChannelConfigResponse {
    channel: string;
    enabled: boolean;
    configured: boolean;
    config: {
        // Masked config (passwords = "********")
    };
    lastTestedAt?: string;
    lastTestResult?: 'success' | 'failed';
    lastTestError?: string;
}

TestChannelResponse {
    success: boolean;
    message: string;
    details?: object;
}
```

---

### Phase 4: Frontend - Types & API Client

#### 4.1 Add Types
**File**: notifications.ts

```typescript
// Add channel configuration types
export interface EmailChannelConfig {
    smtpHost: string;
    smtpPort: number;
    smtpUsername: string;
    fromEmail: string;
    fromName: string;
    enableSsl: boolean;
}

export interface SmsChannelConfig {
    provider: 'twilio';
    accountSid: string;
    fromPhoneNumber: string;
}

export interface TeamsChannelConfig {
    webhookUrl: string;
}

export interface ChannelConfigurationResponse {
    channel: NotificationChannel;
    enabled: boolean;
    configured: boolean;
    config?: EmailChannelConfig | SmsChannelConfig | TeamsChannelConfig;
    lastTestedAt?: string;
    testStatus?: 'success' | 'failed' | 'pending';
    testError?: string;
}
```

#### 4.2 API Client Functions
**File**: `Cal.ImportPulse/app/operations/lib/notifications-api.ts`

```typescript
export const channelConfigApi = {
    getConfig: (channel: NotificationChannel) => Promise<ChannelConfigurationResponse>,
    updateConfig: (channel: NotificationChannel, config: object) => Promise<void>,
    testConnection: (channel: NotificationChannel) => Promise<TestResult>,
    enableChannel: (channel: NotificationChannel) => Promise<void>,
    disableChannel: (channel: NotificationChannel) => Promise<void>,
};
```

---

### Phase 5: Frontend - Channel Configuration Pages

#### 5.1 Channel List Page
**File**: `Cal.ImportPulse/app/operations/app/orchestration/notifications/channels/page.tsx`

Features:
- Grid/list of all channels with status indicators
- Quick enable/disable toggles
- Configuration status (Configured/Not Configured)
- "Configure" button for each channel
- Last test result indicator

#### 5.2 Channel Configuration Components
**Files**: `Cal.ImportPulse/app/operations/app/orchestration/notifications/channels/components/`

| Component | Purpose |
|-----------|---------|
| `ChannelList.tsx` | Main list of channels |
| `EmailConfigForm.tsx` | SMTP configuration form |
| `SmsConfigForm.tsx` | Twilio configuration form |
| `TeamsConfigForm.tsx` | Teams webhook configuration |
| `SignalRConfigView.tsx` | Read-only SignalR status |
| `TestConnectionButton.tsx` | Test & show results |
| `ChannelConfigModal.tsx` | Modal wrapper for config forms |

#### 5.3 Individual Channel Config Pages (Alternative to Modal)
**Files**: 
- `.../channels/email/page.tsx`
- `.../channels/sms/page.tsx`
- `.../channels/teams/page.tsx`

---

### Phase 6: Frontend - Configuration Forms

#### 6.1 Email Configuration Form
```tsx
// Fields
- SMTP Host (text input)
- SMTP Port (number input, default 587)
- Username (text input)
- Password (password input, optional if updating)
- From Email (email input)
- From Name (text input)
- Enable SSL (toggle)
- [Test Connection] button
- [Save] [Cancel] buttons
```

#### 6.2 SMS (Twilio) Configuration Form
```tsx
// Fields
- Account SID (text input)
- Auth Token (password input)
- From Phone Number (tel input with validation)
- [Test Connection] button (sends test SMS)
- [Save] [Cancel] buttons
```

#### 6.3 Teams Configuration Form
```tsx
// Fields
- Webhook URL (url input with validation)
- [Test Connection] button (sends test card)
- [Save] [Cancel] buttons
```

---

### Phase 7: MCP Server Integration

#### 7.1 Add New MCP Tools
Extend the notification MCP server with:

```typescript
// New tools
- channel_config_get: Get channel configuration
- channel_config_update: Update channel configuration  
- channel_test: Test channel connectivity
- channel_enable: Enable a channel
- channel_disable: Disable a channel
```

---

## Implementation Order

| Order | Task | Effort | Priority |
|-------|------|--------|----------|
| 1 | Database migration & entity | 2h | High |
| 2 | Configuration repository | 2h | High |
| 3 | Encryption service | 2h | High |
| 4 | Update ChannelHealthService | 2h | High |
| 5 | API endpoints | 3h | High |
| 6 | Frontend types & API client | 1h | High |
| 7 | Channels list page | 2h | High |
| 8 | Email config form | 2h | High |
| 9 | SMS config form | 1h | Medium |
| 10 | Teams config form | 1h | Medium |
| 11 | Test connection functionality | 2h | High |
| 12 | MCP tools | 2h | Low |

**Total Estimated Effort**: ~22 hours

---

## Security Considerations

1. **Sensitive Data Encryption**: All passwords, API keys, and tokens must be encrypted at rest
2. **Masked Responses**: API responses must mask sensitive values (show `********`)
3. **Authorization**: Only admin users should access channel configuration
4. **Audit Logging**: Log all configuration changes with user and timestamp
5. **Rate Limiting**: Limit test connection attempts to prevent abuse

---

## UI/UX Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│  Channel Configuration                              [← Back] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ ⚡ SignalR                              ● Healthy    │    │
│  │ Real-time web notifications                          │    │
│  │ Status: Always Active (Built-in)         [View]     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 📧 Email                    [Toggle: ON]  ○ Unhealthy│    │
│  │ SMTP: Not Configured                                 │    │
│  │ Last Test: Never                    [Configure]     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 📱 SMS                      [Toggle: OFF] ○ Unhealthy│    │
│  │ Twilio: Not Configured                               │    │
│  │ Last Test: Never                    [Configure]     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 👥 Teams                    [Toggle: OFF] ○ Unhealthy│    │
│  │ Webhook: Not Configured                              │    │
│  │ Last Test: Never                    [Configure]     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Next Steps

1. **Review & Approve Plan** - Get stakeholder sign-off
2. **Create Feature Branch** - `feature/channel-configuration`
3. **Start Phase 1** - Database schema and entities
4. **Iterate** - Build incrementally with testing at each phase

Would you like me to start implementing any specific phase of this plan?