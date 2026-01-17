# 🗄️ Database Setup Guide - Human OS v2.0

## 📊 Current Status

**Current Configuration**: Using **InMemory Database** (default)
- ✅ **Works immediately** - No setup required
- ✅ **Perfect for development** - Fast, no configuration needed
- ⚠️ **Data is NOT persistent** - Resets when app restarts

---

## 🔧 Database Options

You have **2 options** for database storage:

### **Option 1: InMemory Database** (Current - Already Connected ✅)

**Status**: ✅ **Already configured and working**

- **No setup required** - Works out of the box
- **Perfect for testing** - Fast, no configuration
- **Data resets on restart** - Not for production

**How it works:**
- Data is stored in memory
- Lost when application stops
- Automatically initialized when app starts
- No migrations needed

**Use this for:**
- ✅ Development and testing
- ✅ Quick demos
- ✅ Learning and experimentation

---

### **Option 2: SQL Server Database** (Recommended for Production)

**Status**: ⏳ **Ready to configure** (optional)

- **Persistent data** - Survives app restarts
- **Production-ready** - Real database
- **Requires setup** - Need SQL Server and connection string

---

## 🚀 How to Switch to SQL Server

### Step 1: Install SQL Server

**Option A: SQL Server LocalDB** (Easiest - Built into Visual Studio)
- Already installed if you have Visual Studio
- Lightweight, local development database

**Option B: SQL Server Express** (Free, Full Features)
- Download: https://www.microsoft.com/sql-server/sql-server-downloads
- Free version of SQL Server

**Option C: SQL Server Full** (Production)
- Enterprise-grade database server

**Option D: Azure SQL** (Cloud)
- Managed SQL database in Azure

### Step 2: Create Database

Using **SQL Server Management Studio (SSMS)** or **Visual Studio**:

```sql
CREATE DATABASE NeuroSyncHumanOS;
```

Or using **Command Line (sqlcmd)**:
```bash
sqlcmd -S localhost -Q "CREATE DATABASE NeuroSyncHumanOS"
```

### Step 3: Update appsettings.json

Add connection string to `NeuroSync.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NeuroSyncHumanOS;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

**Connection String Examples:**

**LocalDB (Visual Studio):**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NeuroSyncHumanOS;Trusted_Connection=true;TrustServerCertificate=true"
```

**SQL Server Express:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=NeuroSyncHumanOS;Trusted_Connection=true;TrustServerCertificate=true"
```

**SQL Server with Username/Password:**
```json
"DefaultConnection": "Server=localhost;Database=NeuroSyncHumanOS;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
```

**Azure SQL:**
```json
"DefaultConnection": "Server=your-server.database.windows.net;Database=NeuroSyncHumanOS;User Id=your-user;Password=your-password;TrustServerCertificate=false"
```

### Step 4: Create Database Migrations

```bash
cd NeuroSync.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This will:
1. Create migration files in `NeuroSync.Api/Migrations/`
2. Create all tables in your database
3. Set up indexes and relationships

### Step 5: Run the Application

```bash
dotnet run
```

The system will now use **SQL Server** instead of InMemory database!

---

## ✅ Quick Start (No Database Setup)

**For immediate use** (current setup):

1. **Run the application:**
   ```bash
   cd NeuroSync.Api
   dotnet run
   ```

2. **Access the dashboard:**
   - `http://localhost:5000/dashboard.html`
   - `https://localhost:5001/dashboard.html`

3. **Use the system** - Data stored in memory (resets on restart)

**No database configuration needed!** ✅

---

## 🔄 Switching Between Databases

### Use InMemory (Current):
- **Remove or comment out** `ConnectionStrings` in `appsettings.json`
- System automatically uses InMemory database

### Use SQL Server:
- **Add** `ConnectionStrings` in `appsettings.json`
- Run migrations: `dotnet ef database update`
- System automatically uses SQL Server

**The code handles both automatically!**

---

## 📋 Database Tables (Auto-created)

When using SQL Server with migrations, these tables will be created:

1. **DailyEmotionalSummaries** - Daily emotional dashboard data
2. **LifeDomains** - 5 life domains (Mental, Relationships, Career, Money, Self)
3. **Decisions** - User decisions and options
4. **DecisionOptions** - Decision option analysis
5. **CollapseRiskAssessments** - Burnout/depression/anxiety risk
6. **IdentityProfiles** - User identity and purpose
7. **LifeEvents** - Life events for narrative memory
8. **EmotionalGrowthMetrics** - Growth metrics

---

## 🎯 Recommendations

### Development/Testing:
✅ **Use InMemory** (current) - Fast, no setup, perfect for testing

### Production:
✅ **Use SQL Server** - Persistent, scalable, production-ready

---

## 🛠️ Troubleshooting

### Error: "Cannot open database"
- **Solution**: Create the database first: `CREATE DATABASE NeuroSyncHumanOS;`

### Error: "Connection string not found"
- **Solution**: This is OK! System will use InMemory database as fallback

### Error: "Migration not found"
- **Solution**: Run migrations: `dotnet ef database update`

### Want to reset database?
- **InMemory**: Just restart the app (data resets automatically)
- **SQL Server**: Delete database and run migrations again

---

## 📊 Current Configuration Summary

✅ **Database**: InMemory (no setup needed)
✅ **Tables**: Created automatically in memory
✅ **Data**: Temporary (resets on restart)
✅ **Ready to use**: Yes, works immediately!

**To make data persistent**: Add SQL Server connection string to `appsettings.json`

---

**Current Status**: ✅ **Already connected and working with InMemory database!**

You can use it immediately, or switch to SQL Server when ready for production.
