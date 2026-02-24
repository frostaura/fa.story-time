# TaleWeaver — Database Schema

> PostgreSQL schema for server-side configuration and subscription management.  
> **No story content, child profiles, or PII are stored in this database.**

---

## 1 Design Principles

| Principle | Detail |
|---|---|
| **Zero PII** | The database never stores personal information. `SoftUserId` is an opaque UUID generated on the client. |
| **Soft Delete** | Every entity inherits `IsDeleted` (bool). Hard deletes are prohibited in application code. |
| **Audit Columns** | Every entity inherits `CreatedAt` and `UpdatedAt` (UTC timestamps). |
| **PascalCase** | All table and column names use PascalCase (EF Core convention). |
| **GUIDs** | All primary keys are `Guid` (UUID v4), generated server-side. |

---

## 2 BaseEntity

All entities inherit from `BaseEntity`:

```
BaseEntity
├── Id           : Guid          (PK, default: newid())
├── CreatedAt    : DateTime      (UTC, set on insert)
├── UpdatedAt    : DateTime      (UTC, set on insert and update)
└── IsDeleted    : bool          (default: false)
```

---

## 3 Entity Definitions

### 3.1 Tiers

Defines the subscription tier feature matrix. Tiers are seeded data, not user-editable.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique tier identifier |
| `Name` | `string` | Required, Unique | `Trial`, `Plus`, or `Premium` |
| `Concurrency` | `int` | Required | Max concurrent generation requests |
| `CooldownMinutes` | `int` | Required | Minimum minutes between generations |
| `AllowedLengths` | `string[]` | Required | Array of allowed story lengths (e.g., `["short", "medium"]`) |
| `HasLockScreenArt` | `bool` | Required | Whether tier includes lock-screen poster art |
| `HasLongStories` | `bool` | Required | Whether tier allows long-form stories |
| `HasHighQualityBudget` | `bool` | Required | Whether tier uses premium AI model budgets |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

**Seed data**:

| Name | Concurrency | CooldownMinutes | AllowedLengths | LockScreenArt | LongStories | HighQualityBudget |
|---|---|---|---|---|---|---|
| Trial | 1 | 30 | `["short"]` | ❌ | ❌ | ❌ |
| Plus | 2 | 10 | `["short", "medium"]` | ✅ | ❌ | ❌ |
| Premium | 4 | 5 | `["short", "medium", "long"]` | ✅ | ✅ | ✅ |

---

### 3.2 SubscriptionPlans

Maps tiers to Stripe price IDs and billing details.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique plan identifier |
| `TierId` | `Guid` | FK → Tiers.Id, Required | Associated tier |
| `StripePriceId` | `string` | Required, Unique | Stripe Price object ID (e.g., `price_xxx`) |
| `Name` | `string` | Required | Human-readable plan name (e.g., "Plus Monthly") |
| `MonthlyPriceCents` | `int` | Required | Price in cents (e.g., 999 = $9.99) |
| `TrialDays` | `int` | Required, Default: 0 | Number of free trial days |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

**Relationships**: Many SubscriptionPlans → one Tier.

---

### 3.3 Subscriptions

Tracks active subscription state per anonymous user.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique subscription identifier |
| `SoftUserId` | `string` | Required, Indexed | Anonymous device-scoped user ID |
| `PlanId` | `Guid` | FK → SubscriptionPlans.Id, Required | Active subscription plan |
| `StripeSubscriptionId` | `string` | Required, Unique | Stripe Subscription object ID |
| `StripeCustomerId` | `string` | Required, Indexed | Stripe Customer object ID |
| `Status` | `enum` | Required | One of: `trialing`, `active`, `past_due`, `canceled` |
| `CurrentPeriodStart` | `DateTime` | Required | Start of current billing period (UTC) |
| `CurrentPeriodEnd` | `DateTime` | Required | End of current billing period (UTC) |
| `TrialEnd` | `DateTime?` | Nullable | Trial expiration timestamp (UTC), null if no trial |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

**Relationships**: Many Subscriptions → one SubscriptionPlan.

**Status enum values**:

| Value | Description |
|---|---|
| `trialing` | User is within their free trial period |
| `active` | Subscription is paid and active |
| `past_due` | Payment failed; grace period before cancellation |
| `canceled` | Subscription has been canceled (soft-deleted) |

---

### 3.4 FeatureFlags

Runtime feature toggles for gradual rollouts and kill switches.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique flag identifier |
| `Key` | `string` | Required, Unique | Feature flag key (e.g., `enable_flux_images`) |
| `Value` | `string` | Required | Flag value (typically `"true"` / `"false"`, but supports any string) |
| `Description` | `string` | Required | Human-readable description of what the flag controls |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

---

### 3.5 AppConfig

General application configuration stored in the database for runtime flexibility.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique config entry identifier |
| `Key` | `string` | Required, Unique | Configuration key (e.g., `max_story_length_words`) |
| `Value` | `string` | Required | Configuration value |
| `Category` | `string` | Required | Grouping category (e.g., `generation`, `tts`, `stripe`, `ui`) |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

---

### 3.6 CooldownState

Per-user rate-limiting state for story generation.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | `Guid` | PK | Unique cooldown record identifier |
| `SoftUserId` | `string` | Required, Unique, Indexed | Anonymous device-scoped user ID |
| `LastGenerationAt` | `DateTime` | Required | Timestamp of the user's last generation request (UTC) |
| `TierId` | `Guid` | FK → Tiers.Id, Required | User's current tier (determines cooldown duration) |
| `CreatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `UpdatedAt` | `DateTime` | Auto | Inherited from BaseEntity |
| `IsDeleted` | `bool` | Default: false | Inherited from BaseEntity |

---

## 4 Entity Relationship Diagram

```
┌──────────────┐       ┌───────────────────┐       ┌───────────────┐
│    Tiers      │       │ SubscriptionPlans  │       │ Subscriptions  │
├──────────────┤       ├───────────────────┤       ├───────────────┤
│ Id (PK)      │◄──┐   │ Id (PK)           │◄──┐   │ Id (PK)       │
│ Name         │   └───│ TierId (FK)       │   └───│ PlanId (FK)   │
│ Concurrency  │       │ StripePriceId     │       │ SoftUserId    │
│ Cooldown...  │       │ Name              │       │ StripeSub...  │
│ Allowed...   │       │ MonthlyPrice...   │       │ StripeCust... │
│ HasLock...   │       │ TrialDays         │       │ Status        │
│ HasLong...   │       │ CreatedAt         │       │ CurrentPer... │
│ HasHigh...   │       │ UpdatedAt         │       │ TrialEnd      │
│ CreatedAt    │       │ IsDeleted         │       │ CreatedAt     │
│ UpdatedAt    │       └───────────────────┘       │ UpdatedAt     │
│ IsDeleted    │                                    │ IsDeleted     │
└──────┬───────┘                                    └───────────────┘
       │
       │  ┌────────────────┐
       └──│ CooldownState   │
          ├────────────────┤
          │ Id (PK)        │
          │ SoftUserId     │
          │ LastGeneration │
          │ TierId (FK)    │
          │ CreatedAt      │
          │ UpdatedAt      │
          │ IsDeleted      │
          └────────────────┘

┌──────────────┐       ┌──────────────┐
│ FeatureFlags  │       │  AppConfig    │
├──────────────┤       ├──────────────┤
│ Id (PK)      │       │ Id (PK)      │
│ Key          │       │ Key          │
│ Value        │       │ Value        │
│ Description  │       │ Category     │
│ CreatedAt    │       │ CreatedAt    │
│ UpdatedAt    │       │ UpdatedAt    │
│ IsDeleted    │       │ IsDeleted    │
└──────────────┘       └──────────────┘
```

### Relationship Summary

| Relationship | Type | Description |
|---|---|---|
| Tiers → SubscriptionPlans | One-to-Many | A tier can have multiple plans (e.g., monthly vs. annual) |
| SubscriptionPlans → Subscriptions | One-to-Many | A plan can have many active subscriptions |
| Tiers → CooldownState | One-to-Many | Cooldown duration is determined by the user's tier |

---

## 5 Indexes

| Table | Column(s) | Type | Rationale |
|---|---|---|---|
| `Subscriptions` | `SoftUserId` | Non-unique | Lookup by anonymous user |
| `Subscriptions` | `StripeSubscriptionId` | Unique | Webhook event correlation |
| `Subscriptions` | `StripeCustomerId` | Non-unique | Customer lookup |
| `CooldownState` | `SoftUserId` | Unique | Fast cooldown check per user |
| `FeatureFlags` | `Key` | Unique | Flag lookup by key |
| `AppConfig` | `Key` | Unique | Config lookup by key |
| `Tiers` | `Name` | Unique | Tier lookup by name |
| `SubscriptionPlans` | `StripePriceId` | Unique | Stripe price correlation |

---

## 6 Migration Strategy

- **EF Core Code-First migrations** are the sole mechanism for schema changes.
- No manual DDL is permitted against any environment.
- Migration naming convention: `YYYYMMDD_HHMMSS_DescriptiveName`.
- Every migration must be reviewed by the Architect for schema correctness.
- Seed data (Tiers, default AppConfig) is applied via `HasData()` in `OnModelCreating`.
