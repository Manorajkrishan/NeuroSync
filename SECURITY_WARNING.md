# ⚠️ SECURITY WARNING - Important!

## Your Credentials Are in appsettings.json

Your YouTube/Google OAuth credentials are now stored in `appsettings.json`:
- Client ID: `YOUR_CLIENT_ID_HERE` (replace with your actual Client ID)
- Client Secret: `YOUR_CLIENT_SECRET_HERE` (replace with your actual Client Secret)

## ⚠️ Security Risks

If you commit this file to a public Git repository:
- ❌ Anyone can see your credentials
- ❌ They can use your API quota
- ❌ They can access your Google Cloud project
- ❌ You may face unexpected charges

## ✅ What to Do

### 1. Add to .gitignore (IMPORTANT!)

Make sure `appsettings.json` is in your `.gitignore` file:

```gitignore
# User-specific files
appsettings.json
appsettings.*.json
!appsettings.example.json
```

### 2. Check if Already Committed

If you've already committed `appsettings.json` to Git:

```bash
# Remove from Git tracking (but keep local file)
git rm --cached NeuroSync.Api/appsettings.json

# Commit the removal
git commit -m "Remove appsettings.json from version control"

# Add to .gitignore
echo "appsettings.json" >> .gitignore
git add .gitignore
git commit -m "Add appsettings.json to .gitignore"
```

### 3. Use Alternative Storage (Recommended)

#### For Development: User Secrets
```powershell
dotnet user-secrets init --project NeuroSync.Api
dotnet user-secrets set "IoT:YouTubeMusic:ClientId" "YOUR_CLIENT_ID_HERE"
dotnet user-secrets set "IoT:YouTubeMusic:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
```

#### For Production: Environment Variables
```bash
export IoT__YouTubeMusic__ClientId="YOUR_CLIENT_ID_HERE"
export IoT__YouTubeMusic__ClientSecret="YOUR_CLIENT_SECRET_HERE"
```

#### For Production: Azure Key Vault / AWS Secrets Manager
Store credentials in secure cloud key management services.

### 4. Create Example File

Create `appsettings.example.json` with placeholder values:

```json
{
  "IoT": {
    "YouTubeMusic": {
      "ClientId": "YOUR_CLIENT_ID_HERE",
      "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
      "AccessToken": "",
      "RefreshToken": ""
    }
  }
}
```

Commit this file (it's safe - no real credentials).

## Current Status

✅ Credentials added to `appsettings.json`  
⚠️ **Make sure it's in .gitignore before committing!**

## Quick Checklist

- [ ] Check if `appsettings.json` is in `.gitignore`
- [ ] If already committed, remove from Git history
- [ ] Consider using User Secrets for development
- [ ] Create `appsettings.example.json` for reference
- [ ] Never commit real credentials to Git

## If Credentials Are Compromised

If your credentials are exposed:

1. **Revoke the credentials immediately:**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Navigate to: APIs & Services → Credentials
   - Delete or regenerate the OAuth client

2. **Create new credentials:**
   - Create a new OAuth 2.0 Client ID
   - Update your configuration
   - Update all deployed instances

3. **Review API usage:**
   - Check for unexpected API calls
   - Review billing/quotas
   - Monitor for suspicious activity

**Your credentials are configured, but please secure them before committing to Git!**

