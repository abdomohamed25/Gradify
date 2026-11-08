# Module 02: Settings & Quick Actions (backend)

Adds two protected endpoint groups. All new files; nothing existing is modified except one small addition to `Program.cs` (4 lines).

## Endpoints (all require `Authorization: Bearer <token>` from `/api/auth/login`)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/settings` | Load the user's settings (created with defaults on first call) |
| PUT | `/api/settings` | Save `{ "theme": "Light"\|"Dark", "notificationsEnabled": bool, "language": "en"\|"ar" }` |
| POST | `/api/settings/reset` | Back to defaults (Light / true / en) |
| GET | `/api/quick-actions` | Quick Actions the current user's role may see |

Messages match the SRS: `Settings updated successfully.` / `Failed to save settings.`

## Setup (each developer, once)

1. Copy the files in this folder into the project (same folder names).
2. In `Program.cs` add these lines right after `builder.Services.AddScoped<IEmailService, EmailService>();`
   (no new `using` is needed: the new files use the same `AuthDemo.*` namespaces, which `Program.cs` already imports):

```csharp
// Module 02: Settings + Quick Actions
builder.Services.AddDbContext<SettingsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Settings")));
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IProjectRoleResolver, DefaultProjectRoleResolver>();
builder.Services.AddScoped<IQuickActionsService, QuickActionsService>();
```

3. Create the `User_Settings` table:

```bash
dotnet ef migrations add AddUserSettings --context SettingsDbContext --output-dir Migrations/Settings
dotnet ef database update --context SettingsDbContext
```

4. There are now two DbContexts, so EF commands for the existing one need `--context ApplicationDbContext`:

```bash
dotnet ef migrations add <Name> --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

## Design notes

- `SettingsDbContext` is separate with its own history table (`__EFMigrationsHistory_Settings`), so it does not interfere with `ApplicationDbContext` migrations.
- `User_Settings.UserId` is text taken from the token's `sub` claim. There is no foreign key to `Users`.
- Quick Actions visibility per role is defined in one place: `QuickActionCatalog` in `Services/QuickActionsService.cs`.
  - Team Leader: all 5 actions. Team Member: all except `create_project`. Supervisor: `schedule_meeting` only.
  - The SRS does not define this table, so it is an assumption and easy to change.
- Team Leader / Team Member are per-project (Module 03), which does not exist yet. `DefaultProjectRoleResolver` is a temporary stand-in: a role containing "supervisor" is Supervisor, otherwise the optional setting below, otherwise Team Member.
- Optional `appsettings.json` key (for demos, makes students see all 5 actions):

```json
"QuickActions": { "DefaultStudentRole": "TeamLeader" }
```

- `QuickActionCatalog.IsAllowed(role, "create_task")` can be used by the Projects/Tasks/Files/Meetings endpoints to enforce permissions (SRS 5.5). It is not wired into them yet.
- `POST /api/settings/reset` is not in the SRS, but the UI has a Reset Settings button.

## Tested locally

Settings read/save/validation/reset, Quick Actions for Student and Supervisor, and 401 without a token.
