using pearl_leasing.Data;
using System.Net;
using System.Net.Mail;

namespace pearl_leasing.Class
{
    public class SendEmailOtp
    {
        public void SendOTP(string targetEmail, string otpCode)
        {
            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential("liwanagken853@gmail.com", "cbfvirrxkvbjnkdk");
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                                                mailMessage.From = new MailAddress("liwanagken853@gmail.com", "Support Team");
                        mailMessage.Subject = "Your one-time password";
                        mailMessage.To.Add(targetEmail);

                                                mailMessage.IsBodyHtml = true;

                                                mailMessage.Body = $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333333; max-width: 600px; margin: 0 auto; padding: 20px; line-height: 1.6;"">
                    <p style=""font-size: 16px;"">Hello,</p>
                    
                    <p style=""font-size: 15px; margin-bottom: 25px;"">
                        To verify your identity and continue signing in to the Pearl Place, 
                        please enter the following one-time password (OTP):
                    </p>
                    
                    <p style=""font-size: 16px; margin-bottom: 10px;"">Your one-time password (OTP) is:</p>
                    
                    <div style=""font-size: 36px; font-weight: bold; letter-spacing: 4px; color: #1a1a1a; margin-bottom: 30px; margin-top: 5px;"">
                        {otpCode}
                    </div>
                    
                    <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
                        The code is valid for 5 minutes and may only be used once.
                    </p>
                    
                    <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
                        For security purposes, do not share this code with anyone.
                    </p>
                    
                    <p style=""font-size: 14px; color: #555555; margin-bottom: 35px;"">
                        If you did not request this verification, please contact your system administrator.
                    </p>
                    
                    <p style=""font-size: 15px; margin-bottom: 5px;"">Thank you,</p>
                    <p style=""font-size: 15px; font-weight: 500; margin-top: 0;"">Support Team</p>
                </div>";

                        smtpClient.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email dispatch failed: {ex.Message}");
            }
        }
    }
}
