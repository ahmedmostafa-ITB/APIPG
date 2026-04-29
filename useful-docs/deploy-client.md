# Deploy to Client

Checklist for deploying to a client environment (e.g. TKH test/prod servers).
Based on lessons learned from actual deployment to TKH servers.

## Pre-Deploy Checklist — 5 locations to update

1. **`.env`** → `REACT_APP_API_URL` — use relative path (e.g. `/PGAPITest`) to avoid CORS issues with load balancers
2. **`package.json`** → `"homepage"` matching the IIS application name (e.g. `/PGApplicationFormTest/`)
3. **`src/App.js`** → `<Router basename={...}>` matching homepage without trailing slash (e.g. `/PGApplicationFormTest`)
4. **`Web.config`** (frontend, repo root) → `<action type="Rewrite" url="/<IIS_APP_NAME>/" />` matching the IIS application name
5. **API `Web.config`** → update `VerificationURLAccount`, `ApplicationLink`, `APIBaseURLcallback` to match new IIS app names

### CORS Note
Use relative API URL in `.env` (e.g. `/PGAPITest` not `https://pcss.tkh.edu.eg/PGAPITest`).
Full domain URLs cause CORS errors when accessing via IP or load balancer since the
browser sees different origins (IP vs domain).

## Frontend Build & Deploy

### Build (on your laptop — PowerShell)

```powershell
cd C:\z-src\TKH\app-form-pg\ApplicationFormPG-test

# Delete old build — CRA caches aggressively, old URLs get baked in
Remove-Item -Recurse -Force build

# Set OpenSSL flag (required for Node.js 17+)
$env:NODE_OPTIONS="--openssl-legacy-provider"

# Build — verify output says "hosted at /<CORRECT_APP_NAME>/"
npm run build
```

**IMPORTANT:** After build, verify the output message:
```
The project was built assuming it is hosted at /PGApplicationFormTest/.
```
If it says the wrong path, the `homepage` in package.json wasn't updated or the cache wasn't cleared.

### Deploy to server(s)

```powershell
$servers = @("SERVER1", "SERVER2")

foreach ($server in $servers) {
    Write-Host "Deploying frontend to $server..." -ForegroundColor Cyan

    # Build output
    Copy-Item -Path "C:\z-src\TKH\app-form-pg\ApplicationFormPG-test\build\*" `
        -Destination "\\$server\c$\inetpub\wwwroot\PGApplicationFormTest\" -Recurse -Force

    # IIS URL Rewrite config (required for client-side routing)
    Copy-Item -Path "C:\z-src\TKH\app-form-pg\ApplicationFormPG-test\Web.config" `
        -Destination "\\$server\c$\inetpub\wwwroot\PGApplicationFormTest\Web.config" -Force

    Write-Host "Done: $server" -ForegroundColor Green
}
```

### URL Rewrite prerequisite
The server must have **IIS URL Rewrite Module** installed. Without it:
- The `Web.config` rewrite rules cause HTTP 500.19 error
- Refreshing on sub-routes (e.g. `/PGApplicationFormTest/login`) gives 404

Check if installed:
```cmd
C:\Windows\System32\inetsrv\appcmd.exe list module | findstr -i rewrite
```
If empty, install from: https://www.iis.net/downloads/microsoft/url-rewrite

## API Build & Deploy

### Build (on your laptop)

1. Open `APIPG-test\SelfServiceAPI.sln` in Visual Studio
2. Right-click SelfServiceAPI → Publish → target a temp folder (e.g. `C:\temp\PGAPITest-publish`)

### Prepare Web.config

Copy `Web.config.client-ready` (pre-configured with client URLs) or create from `Web.config.template`.

**Critical settings to verify:**

| Setting | Value |
|---|---|
| Connection strings | Client DB server, catalog, credentials |
| Smtp / UserName / Password / FromEmail | Client SMTP (e.g. smtp.office365.com / Apply@tkh.edu.eg) |
| VerificationURLAccount | `https://<DOMAIN>/<API_APP_NAME>/EmailVerification.aspx` |
| ApplicationLink | `https://<DOMAIN>/<FRONTEND_APP_NAME>` |
| APIBaseURLcallback | `https://<DOMAIN>/<API_APP_NAME>` |
| ErrorLogs | Writable folder path (create manually if doesn't exist) |
| System.Net.Http binding redirect | `newVersion="4.1.1.3"` (NOT 4.2.0.0) |

### Deploy to server(s)

```powershell
$servers = @("SERVER1", "SERVER2")

foreach ($server in $servers) {
    Write-Host "Deploying API to $server..." -ForegroundColor Cyan

    # Published output
    Copy-Item -Path "C:\temp\PGAPITest-publish\*" `
        -Destination "\\$server\c$\inetpub\wwwroot\PGAPITest\" -Recurse -Force

    # Client Web.config (with correct URLs, credentials, binding redirect)
    Copy-Item -Path "C:\z-src\TKH\app-form-pg\APIPG-test\Web.config.client-ready" `
        -Destination "\\$server\c$\inetpub\wwwroot\PGAPITest\Web.config" -Force

    # System.Net.Http.dll — NOT included by Visual Studio publish
    # Without this: "Could not load file or assembly 'System.Net.Http'" error on startup
    Copy-Item -Path "C:\z-src\TKH\app-form-pg\APIPG\packages\System.Net.Http.4.3.4\lib\net46\System.Net.Http.dll" `
        -Destination "\\$server\c$\inetpub\wwwroot\PGAPITest\bin\System.Net.Http.dll" -Force

    Write-Host "Done: $server" -ForegroundColor Green
}
```

**IMPORTANT — System.Net.Http.dll:**
- Visual Studio publish does NOT include this DLL
- Without it the API fails immediately with `FileNotFoundException`
- The DLL is in `packages\System.Net.Http.4.3.4\lib\net46\` (local-dev repo, not test worktree)
- The binding redirect in Web.config must say `newVersion="4.1.1.3"` to match this DLL
- If it says `4.2.0.0` you get "manifest definition does not match" error

## IIS Setup (first time only, on each server)

```powershell
Import-Module WebAdministration

# App Pools (separate for isolation)
New-WebAppPool -Name "PGAPITest" -Force
Set-ItemProperty "IIS:\AppPools\PGAPITest" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\PGAPITest" -Name "managedPipelineMode" -Value "Integrated"

New-WebAppPool -Name "PGApplicationFormTest" -Force
Set-ItemProperty "IIS:\AppPools\PGApplicationFormTest" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\PGApplicationFormTest" -Name "managedPipelineMode" -Value "Integrated"

# Applications under Default Web Site
New-WebApplication -Site "Default Web Site" -Name "PGAPITest" `
    -PhysicalPath "C:\inetpub\wwwroot\PGAPITest" -ApplicationPool "PGAPITest"
New-WebApplication -Site "Default Web Site" -Name "PGApplicationFormTest" `
    -PhysicalPath "C:\inetpub\wwwroot\PGApplicationFormTest" -ApplicationPool "PGApplicationFormTest"
```

### Restart app pools after deployment

```powershell
$servers = @("SERVER1", "SERVER2")

foreach ($server in $servers) {
    Invoke-Command -ComputerName $server -ScriptBlock {
        Restart-WebAppPool -Name "PGAPITest"
        Restart-WebAppPool -Name "PGApplicationFormTest"
    }
}
```

## Post-Deploy Verification

Run these checks in order — each step depends on the previous:

1. **API responds:**
   `https://<DOMAIN>/PGAPITest/api/PersonalInfo/GetCountries`
   → Should return JSON array of countries

2. **Frontend loads:**
   `https://<DOMAIN>/PGApplicationFormTest`
   → Should show login page (not blank page, not "not available")

3. **Client-side routing works:**
   `https://<DOMAIN>/PGApplicationFormTest/login` (direct URL, not navigation)
   → Should show login page (not 404). If 404: URL Rewrite module not installed

4. **No CORS errors:**
   Open browser dev tools → Console. If CORS errors appear, check that `.env` uses
   relative API URL (`/PGAPITest`) not full domain URL

5. **Signup and email verification:**
   Create test account → verify email arrives → click verification link → redirects to login
   - If email fails: check SMTP credentials and server network access to smtp.office365.com
   - If verification link goes to wrong URL: check `VerificationURLAccount` in Web.config
   - If post-verification redirect goes to wrong page: check `ApplicationLink` in Web.config

6. **Full application flow:**
   Login → fill all sections → save → logout → login again → verify data restored → submit
   - Check all 13 sections restore correctly
   - Check attachments upload and appear in ApplicationAttachment table
   - Check ApplicationUserDefined has 30 rows (4 Arabic/Disabilities + 26 PG fields)

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Blank page | Build used wrong `homepage` path | Delete build/, verify package.json, rebuild |
| 404 on sub-routes (/login) | URL Rewrite module not installed | Install IIS URL Rewrite Module |
| HTTP 500.19 | URL Rewrite module not registered | Reinstall URL Rewrite, run `iisreset` |
| "Could not load System.Net.Http" | DLL missing from bin/ | Copy from packages folder |
| "manifest definition does not match" | Binding redirect version wrong | Change newVersion to 4.1.1.3 |
| CORS errors | .env has full domain URL | Use relative path `/PGAPITest` |
| "Application not available" | API unreachable or `GetIsApplicationActive` failing | Check API URL, app pool running |
| Email verification fails | SMTP blocked or wrong credentials | Check SMTP config, try from server directly |
| Post-verification wrong redirect | `ApplicationLink` in Web.config wrong | Update to match frontend IIS app name |
