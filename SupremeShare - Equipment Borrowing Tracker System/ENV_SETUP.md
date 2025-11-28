# Database Connection Setup Guide

This guide explains how to set up the PostgreSQL database connection for the Equipment Borrowing Tracker System.

---

## Step 1: Install Required NuGet Packages

Run these commands in your project folder:

```bash
dotnet add package Npgsql
dotnet add package DotNetEnv
dotnet add package BCrypt.Net-Next
```

---

## Step 2: Create PostgreSQL Database

### Option A: Using pgAdmin

1. Open pgAdmin
2. Right-click on "Databases" → "Create" → "Database"
3. Enter name: `supremeshare_db`
4. Click "Save"

### Option B: Using Command Line

```sql
CREATE DATABASE supremeshare_db;
```

---

## Step 3: Run Schema Scripts

Execute the SQL files in the `Schema` folder in this order:

1. `01_types_and_schema.sql` - Creates enum types and audit schema
2. `02_create_tables.sql` - Creates all tables
3. `03_indexes.sql` - Creates indexes for performance
4. `04_roles_and_privileges.sql` - Sets up database roles
5. `05_functions.sql` - Creates stored functions
6. `06_procedures.sql` - Creates stored procedures
7. `07_views.sql` - Creates views

### Using pgAdmin:
1. Open Query Tool
2. Open each file and execute (F5)

### Using Command Line:
```bash
psql -U postgres -d supremeshare_db -f Schema/01_types_and_schema.sql
psql -U postgres -d supremeshare_db -f Schema/02_create_tables.sql
# ... continue for all files
```

---

## Step 4: Configure Environment Variables

### Edit the `.env` file in your project root:

```env
# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=supremeshare_db
DB_USER=postgres
DB_PASSWORD=your_actual_password_here

# Or use full connection string
CONNECTION_STRING=Host=localhost;Port=5432;Database=supremeshare_db;Username=postgres;Password=your_actual_password_here
```

### Important:
- Replace `your_actual_password_here` with your PostgreSQL password
- Never commit the `.env` file to git (it's in .gitignore)
- Use `.env.example` as a template

---

## Step 5: Verify Connection

Run the application:

```bash
dotnet run
```

You should see in the console:
```
SupremeShare - Equipment Borrowing Tracker
Database: supremeshare_db
Environment: Development
```

---

## Connection String Format

The PostgreSQL connection string format:

```
Host=<server>;Port=<port>;Database=<database>;Username=<user>;Password=<password>
```

### Examples:

**Local Development:**
```
Host=localhost;Port=5432;Database=supremeshare_db;Username=postgres;Password=mypassword
```

**Remote Server:**
```
Host=192.168.1.100;Port=5432;Database=supremeshare_db;Username=app_user;Password=securepassword
```

**With SSL (Production):**
```
Host=db.example.com;Port=5432;Database=supremeshare_db;Username=app_user;Password=securepassword;SSL Mode=Require;Trust Server Certificate=true
```

---

## Troubleshooting

### Error: "Connection refused"
- Check if PostgreSQL is running
- Verify the port number (default: 5432)
- Check firewall settings

### Error: "Password authentication failed"
- Verify username and password in `.env`
- Check PostgreSQL user permissions

### Error: "Database does not exist"
- Create the database first (Step 2)
- Check database name spelling

### Error: "Relation does not exist"
- Run the schema scripts (Step 3)
- Check if tables were created in pgAdmin

---

## Security Notes

1. **Never commit `.env` to version control**
   - Add `.env` to `.gitignore`
   - Use `.env.example` for templates

2. **Use strong passwords**
   - Minimum 12 characters
   - Mix of letters, numbers, symbols

3. **Production settings**
   - Use a dedicated database user (not postgres)
   - Enable SSL for remote connections
   - Restrict database user permissions

---

## File Structure

```
Project Root/
├── .env                    ← Your actual config (DO NOT COMMIT)
├── .env.example            ← Template for others
├── appsettings.json        ← App settings
├── appsettings.Development.json
├── Data/
│   └── DatabaseHelper.cs   ← Database connection class
└── Schema/
    ├── 01_types_and_schema.sql
    ├── 02_create_tables.sql
    ├── 03_indexes.sql
    ├── 04_roles_and_privileges.sql
    ├── 05_functions.sql
    ├── 06_procedures.sql
    └── 07_views.sql
```
