# Setting Up Supervisor and Admin Roles via Configuration

## Overview
Role assignments are now fully configuration-driven and support two approaches:
1. **Direct email configuration** - Add specific user emails to grant roles (quick for development)
2. **Azure AD group membership by name** - Add Azure AD group display names (production-ready, scales better)

The system automatically looks up group IDs from Azure AD by display name, so you don't need to work with GUIDs!

## Configuration Structure

Both `appsettings.json` and `appsettings.Development.json` use the same structure:

```json
"Authorization": {
  "Roles": {
    "SupervisorEmails": [
      "user@company.com"
    ],
    "SupervisorGroupNames": [
      "CallCenterSupervisors"
    ],
    "AdminEmails": [
      "admin@company.com"
    ],
    "AdminGroupNames": [
      "CallCenterAdmins"
    ]
  }
}
```

## Setup Options

### Option 1: Using Emails (Development/Quick Setup)

Edit `appsettings.Development.json`:
```json
"Authorization": {
  "Roles": {
    "SupervisorEmails": [
      "madisond@company.com"
    ],
    "SupervisorGroupNames": [],
    "AdminEmails": [
      "madisond@company.com"
    ],
    "AdminGroupNames": []
  }
}
```

✅ **Best for:** Local development, quick testing

### Option 2: Using Azure AD Groups (Production Ready)

1. **Identify your Azure AD groups** in Azure Portal or Microsoft Entra
   - Look for groups like "CallCenterSupervisors", "TimeKeeperAdmins", etc.
   - Note the **exact display name** (case-sensitive in AD, case-insensitive in our config)

2. **Add group names to appsettings**
   ```json
   "Authorization": {
    "Roles": {
      "SupervisorEmails": [],
      "SupervisorGroupNames": [
        "CallCenterSupervisors",
        "TimeKeeperSupervisors"
      ],
      "AdminEmails": [],
      "AdminGroupNames": [
        "CallCenterAdmins",
        "TimeKeeperAdmins"
      ]
    }
   }
   ```

✅ **Best for:** Production, team management, centralized control

### Option 3: Hybrid (Emails + Groups)

Combine both approaches for maximum flexibility:
```json
"Authorization": {
  "Roles": {
    "SupervisorEmails": [
      "madisond@company.com"
    ],
    "SupervisorGroupNames": [
      "CallCenterSupervisors"
    ],
    "AdminEmails": [
      "admin@company.com"
    ],
    "AdminGroupNames": [
      "CallCenterAdmins"
    ]
  }
}
```

Users will be granted the role if they match **either** the email list **OR** are members of one of the configured Azure AD groups.

## How It Works

1. **On every login**, the `DevelopmentClaimsTransformation` service runs
2. **Email check** - Compares user's email against configured supervisor/admin emails
3. **Group lookup** - For each configured group name:
   - Calls Microsoft Graph API to look up the group by display name
   - Gets the group's ObjectId
   - Caches the result to minimize API calls
4. **Group membership check** - Compares user's group claims against the looked-up group ObjectIds
5. **Role assignment** - Adds "Supervisor" or "Admin" role claims if either condition is met

## Current Configuration

Your `appsettings.Development.json` is currently set up as:
- **SupervisorEmails**: `madisond@company.com` ✅
- **SupervisorGroupNames**: `CallCenterSupervisors` (ready to use if group exists)
- **AdminEmails**: `madisond@company.com` ✅
- **AdminGroupNames**: `CallCenterAdmins` (ready to use if group exists)

You'll have supervisor access immediately when you log in via email!

## Azure AD Setup for Groups (One-Time)

If you want to use Azure AD groups for role assignment:

### Step 1: Enable "groups" Claim in Azure AD
1. Go to **Azure Portal** → **Microsoft Entra ID** → **App Registrations** → Your App
2. Click **Token configuration**
3. Click **Add groups claim**
4. Select:
   - ☑️ **Security groups** (recommended)
   - ☑️ **Distribution groups** (optional)
5. For "Group ID", use the default (**Object ID**)
6. Click **Add**

### Step 2: Ensure Your App Has Permissions
1. Go to **API permissions**
2. Ensure you have **Directory.Read.All** permission (needed for Graph API group lookups)
3. If not present, click **Add a permission** → **Microsoft Graph** → **Application permissions** → search for **Directory.Read.All**

### Step 3: Create or Identify Your Groups

Option A: Using existing groups
- Go to **Microsoft Entra ID** → **Groups**
- Find your groups (e.g., "CallCenterSupervisors")
- Copy the **Display Name** (exact spelling)

Option B: Create new groups
1. Go to **Microsoft Entra ID** → **Groups** → **New Group**
2. Create group "CallCenterSupervisors"
3. Set as **Security** group
4. Add members
5. Create

### Step 4: Update appsettings
```json
"Authorization": {
  "Roles": {
    "SupervisorEmails": [],
    "SupervisorGroupNames": [
      "CallCenterSupervisors"
    ],
    "AdminEmails": [],
    "AdminGroupNames": [
      "CallCenterAdmins"
    ]
  }
}
```

## Finding Your Group Names

### Via Azure Portal
1. Go to **Microsoft Entra ID** → **Groups**
2. Find your group
3. Copy the **Display name** exactly as shown

### Via PowerShell
```powershell
# Connect to Azure AD
Connect-MgGraph -Scopes "Group.Read.All"

# List all groups
Get-MgGroup -All | Where-Object { $_.DisplayName -like "*Supervisor*" } | Select DisplayName, Id
```

### Via Microsoft Graph Explorer
1. Go to https://developer.microsoft.com/en-us/graph/graph-explorer
2. Sign in with your Azure AD account
3. Query: `GET https://graph.microsoft.com/v1.0/groups?$filter=displayName eq 'CallCenterSupervisors'`
4. Look for the `displayName` field

## Troubleshooting

### Not seeing supervisor features?
- [ ] **Email-based**: Check email is in `SupervisorEmails` array (case-insensitive match)
- [ ] **Group-based**: Verify group name is in `SupervisorGroupNames`
- [ ] **Groups claim**: Ensure "groups" claim is configured in Azure AD
- [ ] **Group membership**: Verify user is actually a member of the specified group
- [ ] **Restart**: Restart application after editing appsettings
- [ ] **Logs**: Check application logs for debug messages

### Graph API errors?
- [ ] Verify **Directory.Read.All** permission is assigned to the app
- [ ] Check app has admin consent granted
- [ ] Verify group name spelling matches Azure AD exactly

### How to debug what groups a user has?

Enable debug logging in `appsettings.Development.json`:
```json
"Logging": {
  "LogLevel": {
    "TimeKeeper.Blazor.Services.DevelopmentClaimsTransformation": "Debug"
  }
}
```

Then check the Application Output window for debug messages showing which groups were found and looked up.

## Best Practices

✅ **Development**
- Use email addresses for quick local testing
- Don't commit production group names or emails to source control

✅ **Staging**
- Test with both email and group configuration
- Verify Graph API permissions are set up
- Test with actual Azure AD users and groups

✅ **Production**
- Use Azure AD groups exclusively (not emails)
- Use descriptive group names: `CallCenterSupervisors`, `TimeKeeperAdmins`, etc.
- Manage group membership through Azure AD for centralized control
- Store configuration in Azure Key Vault or secure configuration

🔒 **Security**
- Never commit sensitive configuration to git
- Use appsettings.Development.json locally only
- Use environment variables or Key Vault for production
- Regularly audit group membership in Azure AD

📝 **Naming Convention**
- `[AppName]Supervisors` - e.g., TimeKeeperSupervisors
- `[AppName]Admins` - e.g., TimeKeeperAdmins
- `[DivisionName]Supervisors` - e.g., CallCenterSupervisors
- Use consistent naming across your organization

## Migration Path

If you're currently using email-only and want to move to Azure AD groups:

1. **Set up Azure AD groups** with appropriate members
2. **Add group names to configuration**
3. **Keep emails during transition** - both will work
4. **Verify all users can access** via group membership
5. **Remove emails** from configuration once groups are tested
6. **Document the group structure** for your team

## File Changes Summary

| File | Change |
|------|--------|
| `DevelopmentClaimsTransformation.cs` | Updated to look up groups by display name using Microsoft Graph |
| `RoleOptions` class | Changed from GroupIds to GroupNames for readability |
| `appsettings.json` | Updated with GroupNames configuration |
| `appsettings.Development.json` | Updated with GroupNames configuration |
| `Program.cs` | Added HttpClientFactory for Graph API calls |

## Support

For group display name issues:
- The lookup is case-insensitive
- Whitespace is trimmed automatically
- Group names are cached after first lookup to minimize API calls
- Check logs for "Resolved group" messages to see successful lookups
