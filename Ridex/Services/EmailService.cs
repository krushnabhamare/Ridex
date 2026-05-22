using System.Net;
using System.Net.Mail;

namespace Ridex.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // =============================
        // Send OTP / Email
        // =============================

        public async Task SendOtpEmailAsync(
            string toEmail,
            string riderName,
            string otp)
        {
            try
            {
                using var smtp = new SmtpClient(
                    _config["Smtp:Host"])
                {
                    Port = int.Parse(
                        _config["Smtp:Port"]!),

                    Credentials = new NetworkCredential(
                        _config["Smtp:Username"],
                        _config["Smtp:Password"]),

                    EnableSsl = true
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(
                        _config["Smtp:Username"]!,
                        "Ridex"),

                    Subject = "Your Ridex Ride OTP",

                    Body = $@"
                    Hello {riderName},
                    Your Ridex OTP is: {otp}
                    Please share this OTP with the driver only after boarding the ride.
                    Regards,
                    Ridex Team
                    ",
                    IsBodyHtml = false
                };

                message.To.Add(toEmail);

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Email sending failed: {ex.Message}");

                throw;
            }
        }
    }
}