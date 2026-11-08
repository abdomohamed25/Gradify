# Gradify Auth API (.NET + SQL Server)

Backend for the Gradify login / multi-step register / email verification flow.

## Endpoints

| Screen | Endpoint |
|---|---|
| Sign In | `POST /api/auth/login` |
| Academic step dropdowns (University/Faculty/Department cascading, Graduation Year) | `GET /api/lookups/academic` |
| Create Account (steps 1-3 submitted together) | `POST /api/auth/register` |
| Profile picture (step 1, optional) | `POST /api/upload/profile-picture` |
| Verify your email | `POST /api/auth/verify-email` |
| Resend Code | `POST /api/auth/resend-code` |
| Forgot password? | `POST /api/auth/forgot-password` |
| Reset password page | `POST /api/auth/reset-password` |
| Loading the signed-in user (dashboard, refresh, etc.) | `GET /api/auth/me` (requires `Authorization: Bearer <token>`) |

> The "Create/Join Team" and "Go to Dashboard" screen isn't built yet — that's a
> separate feature once you've decided how teams/projects should work. Ask if you
> want that added.

## 1. Install NuGet packages
```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

## 2. Copy files into your project
Drop these into your existing .NET project, keeping the folder structure:
- `Models/User.cs`
- `Models/AcademicLookup.cs` (University / Faculty / Department)
- `Data/ApplicationDbContext.cs`
- `DTOs/AuthDtos.cs`
- `Services/TokenService.cs`
- `Services/EmailService.cs`
- `Controllers/AuthController.cs`
- `Controllers/UploadController.cs`
- `Controllers/LookupsController.cs`
- `wwwroot/uploads/profile-pictures/` (empty folder, stores uploaded photos)
- Merge `Program.cs` with your existing one
- Merge `appsettings.json` settings into your existing file

Update the namespace (`AuthDemo`) in every file if your project name is different.

## 3. Update the connection string
In `appsettings.json`, point `DefaultConnection` at your SQL Server instance, e.g.
```
Server=.\SQLEXPRESS;Database=GradifyDb;Trusted_Connection=True;TrustServerCertificate=True;
```

## 4. Replace the JWT key
Generate a real random secret (32+ characters) for `Jwt:Key`. Don't commit a real
key to source control — use `dotnet user-secrets` or environment variables instead.

## 5. Configure email sending (optional for local dev)
Fill in `Smtp:Host/Username/Password` with a real provider (Gmail app password,
SendGrid, Mailtrap, etc). **If you leave `Smtp:Host` empty**, `EmailService` just
logs the verification code to the console instead of sending an email — handy for
testing the register → verify flow locally without setting up SMTP.

## 6. Create the database
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```
This also seeds all University/Faculty/Department rows the Academic step needs:
`EELU` → **Computers and Information Technology** (Computer Science, Software
Engineering, Information Systems, Information Technology), **Business
Administration** (Accounting, Marketing, Management Information Systems), and
**Engineering** (Computer Engineering, Electronics & Communications).

## 7. Run and test
```
dotnet run
```

### Flow, matching the screens
0. **Academic dropdowns** — `GET /api/lookups/academic` before showing step 2. Returns:
   ```json
   {
     "universities": [
       {
         "id": 1,
         "name": "Egyptian E-Learning University (EELU)",
         "faculties": [
           {
             "id": 1,
             "name": "Computers and Information Technology",
             "departments": [
               { "id": 1, "name": "Computer Science" },
               { "id": 2, "name": "Software Engineering" },
               { "id": 3, "name": "Information Systems" },
               { "id": 4, "name": "Information Technology" }
             ]
           },
           { "id": 2, "name": "Business Administration", "departments": [
               { "id": 5, "name": "Accounting" },
               { "id": 6, "name": "Marketing" },
               { "id": 7, "name": "Management Information Systems" }
           ]},
           { "id": 3, "name": "Engineering", "departments": [
               { "id": 8, "name": "Computer Engineering" },
               { "id": 9, "name": "Electronics & Communications" }
           ]}
         ]
       }
     ],
     "graduationYears": [2026, 2027, 2028, 2029, 2030, 2031]
   }
   ```
   The frontend filters `departments` by the selected faculty's `id` client-side.

1. **Register** — `POST /api/auth/register` with all 3 steps' data in one payload:
   ```json
   {
     "firstName": "Jane",
     "lastName": "Doe",
     "username": "janedoe",
     "email": "jane@university.edu",
     "phoneNumber": "01012345678",
     "profilePictureUrl": null,
     "role": "Student",
     "university": "Egyptian E-Learning University (EELU)",
     "faculty": "Computers and Information Technology",
     "department": "Software Engineering",
     "academicId": "202012345",
     "graduationYear": 2027,
     "password": "SecurePass123",
     "confirmPassword": "SecurePass123",
     "agreedToTerms": true
   }
   ```
   Creates an unverified account and emails (or logs, in dev) a 6-digit code.
   `agreedToTerms` must be `true` or the request is rejected with a 400.
   University/Faculty/Department are checked against the lookup tables (see
   below) if provided - an unknown or mismatched combination is also a 400.

2. **Verify** — `POST /api/auth/verify-email` with `{ "email": "...", "code": "482913" }`.
   Marks the account verified and returns a JWT, matching "Welcome aboard!".

3. **Resend** — `POST /api/auth/resend-code` with `{ "email": "..." }` if the code expired.

4. **Login** — `POST /api/auth/login`:
   ```json
   { "emailOrUsername": "janedoe", "password": "SecurePass123", "rememberMe": true }
   ```
   Blocks sign-in with a 403 until the account is verified. `rememberMe` extends
   the token from 2 hours to 30 days.

5. **Forgot password** — `POST /api/auth/forgot-password` with `{ "email": "..." }`.
   Always returns the same generic message (whether or not the email is registered,
   to prevent account enumeration). If an account exists, emails (or logs, in dev)
   a reset link pointing at `Frontend:ResetPasswordUrl` with an email + token query string.

6. **Reset password** — the page at that link calls
   `POST /api/auth/reset-password`:
   ```json
   {
     "email": "jane@university.edu",
     "token": "token-from-the-link",
     "newPassword": "NewSecurePass456",
     "confirmNewPassword": "NewSecurePass456"
   }
   ```
   The reset token expires after 30 minutes (`PasswordReset:TokenExpiryMinutes`)
   and is single-use — only its SHA-256 hash is ever stored.

7. **Current user** — `GET /api/auth/me` with `Authorization: Bearer <token>`.
   Returns the same `UserDto` shape as login/verify, for the dashboard to render
   without decoding the JWT client-side.

## Security limits (already built in)
- Verification code: max 5 wrong attempts (`EmailVerification:MaxFailedAttempts`), then a new code is required.
- Resend code: 60-second cooldown (`EmailVerification:ResendCooldownSeconds`).
- Enums are accepted/returned as strings (`JsonStringEnumConverter` in Program.cs).
- Frontend integration details: see `API_CONTRACT.md`.

## Notes
- Passwords are hashed with BCrypt.
- Email **and** username are unique (enforced at DB + code level).
- The verification code expires after 10 minutes (`EmailVerification:CodeExpiryMinutes`).
- To protect other endpoints later, add `[Authorize]` above the controller/action.
- CORS allows `localhost:5173` (Vite), `localhost:3000`, and the three Vercel URLs (gradify-two / gradify-five / gradify-pi-red) — edit the list in Program.cs if the frontend URL changes.
