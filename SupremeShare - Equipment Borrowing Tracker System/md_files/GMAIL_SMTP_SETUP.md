# Gmail SMTP Setup Guide for SupremeShare

This guide explains how to configure Gmail SMTP for sending verification emails in the SupremeShare system.

## Prerequisites

- A Gmail account
- 2-Factor Authentication enabled on your Google account

## Step 1: Enable 2-Factor Authentication

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Under "Signing in to Google", click on **2-Step Verification**
3. Follow the prompts to enable 2-Step Verification
4. Complete the setup with your phone number or authenticator app

## Step 2: Generate an App Password

> **Important:** You cannot use your regular Gmail password for SMTP. You must generate an App Password.

1. Go to [Google App Passwords](https://myaccount.google.com/apppasswords)
2. You may need to sign in again
3. Under "Select app", choose **Mail**
4. Under "Select device", choose **Other (Custom name)**
5. Enter a name like "SupremeShare" and click **Generate**
6. **Copy the 16-character password** that appears (e.g., `abcd efgh ijkl mnop`)
7. Click **Done**

> ⚠️ **Save this password securely!** You won't be able to see it again.

## Step 3: Configure the .env File

Open your `.env` file in the project root and update the SMTP settings:

```env
# ===========================================
# SMTP Email Configuration (Gmail)
# ===========================================
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-actual-email@gmail.com
SMTP_PASSWORD=abcdefghijklmnop
SMTP_FROM_EMAIL=your-actual-email@gmail.com
SMTP_FROM_NAME=SupremeShare System

# ===========================================
# Application URL (for email verification links)
# ===========================================
APP_BASE_URL=https://localhost:7001
```

### Configuration Details:

| Setting | Value | Description |
|---------|-------|-------------|
| `SMTP_HOST` | `smtp.gmail.com` | Gmail's SMTP server |
| `SMTP_PORT` | `587` | TLS port (recommended) |
| `SMTP_USERNAME` | Your Gmail address | The email you'll send from |
| `SMTP_PASSWORD` | App Password | The 16-character password from Step 2 |
| `SMTP_FROM_EMAIL` | Your Gmail address | Sender email address |
| `SMTP_FROM_NAME` | Any name | Display name for sent emails |
| `APP_BASE_URL` | Your app URL | Used for verification links |

## Step 4: Run the Database Migration

Before using the email verification feature, run the migration to create the required table:

```sql
-- Run this in your PostgreSQL database
-- File: Schema/migrations/004_email_verification_tokens.sql

CREATE TABLE IF NOT EXISTS EMAIL_VERIFICATION_TOKEN (
    EVT_ID SERIAL PRIMARY KEY,
    USER_ID INTEGER NOT NULL REFERENCES "USER"(USER_ID) ON DELETE CASCADE,
    EVT_TOKEN VARCHAR(100) NOT NULL UNIQUE,
    EVT_NEW_EMAIL VARCHAR(100) NOT NULL,
    EVT_EXPIRES_AT TIMESTAMP WITH TIME ZONE NOT NULL,
    EVT_DATE_CREATED TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    EVT_IS_USED BOOLEAN DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_evt_user_id ON EMAIL_VERIFICATION_TOKEN(USER_ID);
CREATE INDEX IF NOT EXISTS idx_evt_token ON EMAIL_VERIFICATION_TOKEN(EVT_TOKEN);
```

## Step 5: Test the Configuration

1. Start the application
2. Log in as Admin or Support Admin
3. Go to **Account** page
4. Click **Edit** on Account Information
5. Go to the **Email** tab
6. Enter a new email address and click **Send Verification Email**
7. Check the inbox of the new email for the verification link

## Troubleshooting

### "Failed to send verification email"

1. **Check App Password**: Make sure you're using the App Password, not your regular Gmail password
2. **Check 2FA**: Ensure 2-Factor Authentication is enabled
3. **Check Credentials**: Verify the email and password in `.env` are correct
4. **Check Firewall**: Ensure port 587 is not blocked

### "Less secure app access" Error

Google no longer supports "Less secure apps". You **must** use App Passwords with 2FA enabled.

### Email Not Received

1. Check the spam/junk folder
2. Verify the recipient email address is correct
3. Check application logs for SMTP errors

### Connection Timeout

1. Check your internet connection
2. Verify `SMTP_HOST` is `smtp.gmail.com`
3. Verify `SMTP_PORT` is `587`
4. Check if your network blocks outgoing SMTP

## Security Best Practices

1. **Never commit `.env` to version control** - Add it to `.gitignore`
2. **Use environment variables in production** - Don't hardcode credentials
3. **Rotate App Passwords periodically** - Generate new ones every few months
4. **Monitor sent emails** - Check your Gmail "Sent" folder for unusual activity

## Alternative SMTP Providers

If Gmail doesn't work for your use case, consider:

| Provider | SMTP Host | Port | Notes |
|----------|-----------|------|-------|
| Outlook/Hotmail | `smtp.office365.com` | 587 | Requires App Password |
| Yahoo | `smtp.mail.yahoo.com` | 587 | Requires App Password |
| SendGrid | `smtp.sendgrid.net` | 587 | API key as password |
| Mailgun | `smtp.mailgun.org` | 587 | API key as password |

## Production Deployment

For production environments, consider:

1. Using a dedicated email service (SendGrid, Mailgun, AWS SES)
2. Setting up SPF, DKIM, and DMARC records for better deliverability
3. Using environment variables instead of `.env` files
4. Implementing rate limiting for email sending
5. Setting up email logging and monitoring
