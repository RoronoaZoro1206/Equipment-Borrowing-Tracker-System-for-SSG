# Environment Configuration

This project uses a `.env` file for configuration. Follow these steps to set up your environment:

## Setup Instructions

1. **Copy the example file:**
   ```bash
   cp .env.example .env
   ```

2. **Configure your database settings in `.env`:**
   - `DB_HOST`: Your PostgreSQL server host (default: localhost)
   - `DB_PORT`: PostgreSQL port (default: 5432)
   - `DB_NAME`: Your database name (default: supremeshare_db)
   - `DB_USER`: Your PostgreSQL username
   - `DB_PASSWORD`: Your PostgreSQL password

3. **Configure JWT settings (if using authentication):**
   - `JWT_SECRET`: A secure random string (minimum 32 characters)
   - `JWT_ISSUER`: Token issuer name
   - `JWT_AUDIENCE`: Token audience name
   - `JWT_EXPIRATION_MINUTES`: Token expiration time in minutes

4. **Configure email settings (if using email notifications):**
   - `SMTP_HOST`: Your SMTP server
   - `SMTP_PORT`: SMTP port
   - `SMTP_USER`: Your email address
   - `SMTP_PASSWORD`: Your email password or app password
   - `SMTP_FROM`: Email "from" address

## Important Notes

- **Never commit your `.env` file to version control!** It contains sensitive credentials.
- The `.env.example` file is a template that should be committed.
- The `.env` file is automatically loaded when the application starts.

## Accessing Environment Variables in Code

```csharp
// Get environment variable
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
```

## Example Connection String

Your connection string will be automatically constructed from the database settings:
```
Host=localhost;Port=5432;Database=supremeshare_db;Username=postgres;Password=your_password
```
