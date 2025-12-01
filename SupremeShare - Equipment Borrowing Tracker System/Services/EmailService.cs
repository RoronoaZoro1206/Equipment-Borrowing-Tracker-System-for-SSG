using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string toEmail, string verificationToken, string username);
        Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string username);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _baseUrl;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
            
            // Load SMTP settings from environment variables
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            _fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? _smtpUsername;
            _fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "SupremeShare System";
            _baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "https://localhost:7001";
        }

        public async Task<bool> SendEmailVerificationAsync(string toEmail, string verificationToken, string username)
        {
            try
            {
                var verificationLink = $"{_baseUrl}/verify-email?token={verificationToken}";
                
                var subject = "SupremeShare - Email Verification Required";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333333; background-color: #f4f4f4;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f4f4f4;'>
        <tr>
            <td align='center' style='padding: 40px 20px;'>
                <table role='presentation' width='600' cellspacing='0' cellpadding='0' border='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background-color: #8B0000; padding: 30px 40px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;'>SupremeShare</h1>
                            <p style='margin: 10px 0 0 0; color: #ffcccc; font-size: 14px;'>Equipment Borrowing Tracker System</p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='margin: 0 0 20px 0; color: #333333; font-size: 22px;'>Email Verification Required</h2>
                            <p style='margin: 0 0 15px 0; color: #555555;'>Hello <strong>{username}</strong>,</p>
                            <p style='margin: 0 0 25px 0; color: #555555;'>You have requested to change your email address. Please click the button below to verify your new email address:</p>
                            
                            <!-- Button -->
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                <tr>
                                    <td align='center' style='padding: 20px 0;'>
                                        <a href='{verificationLink}' style='display: inline-block; padding: 16px 40px; background-color: #8B0000; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: bold; text-align: center;'>Verify Email Address</a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='margin: 25px 0 10px 0; color: #555555;'>Or copy and paste this link into your browser:</p>
                            <p style='margin: 0 0 25px 0; padding: 15px; background-color: #f5f5f5; border-radius: 4px; word-break: break-all; font-size: 13px; color: #666666; border: 1px solid #e0e0e0;'>
                                <a href='{verificationLink}' style='color: #8B0000; text-decoration: none;'>{verificationLink}</a>
                            </p>
                            
                            <p style='margin: 0 0 15px 0; color: #555555;'><strong style='color: #d32f2f;'>⏰ This link will expire in 24 hours.</strong></p>
                            <p style='margin: 0; color: #777777; font-size: 14px;'>If you did not request this change, please ignore this email or contact your administrator.</p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f9f9f9; padding: 25px 40px; text-align: center; border-top: 1px solid #eeeeee;'>
                            <p style='margin: 0 0 5px 0; color: #888888; font-size: 12px;'>© 2025 SupremeShare - Cebu Technological University Main Campus</p>
                            <p style='margin: 0; color: #aaaaaa; font-size: 11px;'>This is an automated message. Please do not reply.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string username)
        {
            try
            {
                var subject = "SupremeShare - Password Changed Successfully";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333333; background-color: #f4f4f4;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f4f4f4;'>
        <tr>
            <td align='center' style='padding: 40px 20px;'>
                <table role='presentation' width='600' cellspacing='0' cellpadding='0' border='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background-color: #8B0000; padding: 30px 40px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;'>SupremeShare</h1>
                            <p style='margin: 10px 0 0 0; color: #ffcccc; font-size: 14px;'>Equipment Borrowing Tracker System</p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='margin: 0 0 20px 0; color: #28a745; font-size: 22px;'>✓ Password Changed Successfully</h2>
                            <p style='margin: 0 0 15px 0; color: #555555;'>Hello <strong>{username}</strong>,</p>
                            <p style='margin: 0 0 25px 0; color: #555555;'>Your password has been successfully changed on <strong>{DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</strong>.</p>
                            
                            <!-- Warning Box -->
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                <tr>
                                    <td style='padding: 20px; background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; border-left: 4px solid #ffc107;'>
                                        <p style='margin: 0 0 10px 0; color: #856404; font-weight: bold; font-size: 15px;'>⚠️ Security Notice</p>
                                        <p style='margin: 0; color: #856404; font-size: 14px;'>If you did not make this change, please contact your administrator immediately to secure your account.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f9f9f9; padding: 25px 40px; text-align: center; border-top: 1px solid #eeeeee;'>
                            <p style='margin: 0 0 5px 0; color: #888888; font-size: 12px;'>© 2025 SupremeShare - Cebu Technological University Main Campus</p>
                            <p style='margin: 0; color: #aaaaaa; font-size: 11px;'>This is an automated message. Please do not reply.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password change notification to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email not sent to {Email}", toEmail);
                return false;
            }

            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
