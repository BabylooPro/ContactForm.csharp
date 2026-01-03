using API.Models;

namespace API.Services
{
    public interface IEmailTemplateService
    {
        EmailTemplate GetTemplate(PredefinedTemplate template);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly Dictionary<PredefinedTemplate, EmailTemplate> _templates;

        public EmailTemplateService()
        {
            _templates = new Dictionary<PredefinedTemplate, EmailTemplate>
            {
                {
                    PredefinedTemplate.Default,
                    new EmailTemplate
                    {
                        Name = "Default",
                        Subject = "Message from {Username}",
                        Body = """
                        New contact form submission:
                        From: {Email}
                        Name: {Username}
                        Message: {Message}
                        """,
                        IsHtml = false
                    }
                },
                {
                    PredefinedTemplate.Modern,
                    new EmailTemplate
                    {
                        Name = "Modern",
                        Subject = "✉️ New Message from {Username}",
                        Body = """
                        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background: #f8f9fa; border-radius: 10px;">
                            <h1 style="color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;">New Message</h1>
                            <div style="background: white; padding: 20px; border-radius: 5px; box-shadow: 0 2px 5px rgba(0,0,0,0.1);">
                                <h2 style="color: #3498db;">From: {Username}</h2>
                                <p style="color: #7f8c8d;"><strong>Email:</strong> {Email}</p>
                                <div style="margin: 20px 0; padding: 15px; background: #f1f3f4; border-radius: 5px;">
                                    <p style="margin: 0; color: #2c3e50;">{Message}</p>
                                </div>
                            </div>
                        </div>
                        """,
                        IsHtml = true
                    }
                },
                {
                    PredefinedTemplate.Minimal,
                    new EmailTemplate
                    {
                        Name = "Minimal",
                        Subject = "Contact: {Username}",
                        Body = """
                        <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto; padding: 20px;">
                            <p><strong>{Username}</strong> sent a message:</p>
                            <blockquote style="border-left: 2px solid #ddd; margin: 0; padding: 10px 20px;">
                                {Message}
                            </blockquote>
                            <p style="color: #666; font-size: 0.9em; margin-top: 20px;">Reply to: {Email}</p>
                        </div>
                        """,
                        IsHtml = true
                    }
                },
                {
                    PredefinedTemplate.Professional,
                    new EmailTemplate
                    {
                        Name = "Professional",
                        Subject = "New Contact Form Submission - {Username}",
                        Body = """
                        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                            <div style="text-align: center; padding: 20px 0;">
                                <h1 style="color: #333; margin: 0;">Contact Form Submission</h1>
                                <p style="color: #666;">Received on {DateTime.Now}</p>
                            </div>
                            <table style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td style="padding: 10px; border-bottom: 1px solid #eee; width: 120px;"><strong>Name:</strong></td>
                                    <td style="padding: 10px; border-bottom: 1px solid #eee;">{Username}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px; border-bottom: 1px solid #eee;"><strong>Email:</strong></td>
                                    <td style="padding: 10px; border-bottom: 1px solid #eee;">{Email}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px;"><strong>Message:</strong></td>
                                    <td style="padding: 10px;">{Message}</td>
                                </tr>
                            </table>
                        </div>
                        """,
                        IsHtml = true
                    }
                },
                {
                    PredefinedTemplate.Alert,
                    new EmailTemplate
                    {
                        Name = "Alert",
                        Subject = "Important message from {Username}",
                        Body = """
                        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                            <div style="background: #e74c3c; color: white; padding: 15px; border-radius: 5px; text-align: center;">
                                <h1 style="margin: 0;">Priority Message</h1>
                            </div>
                            <div style="margin-top: 20px; padding: 20px; border: 2px solid #e74c3c; border-radius: 5px;">
                                <h2 style="color: #e74c3c; margin-top: 0;">From: {Username}</h2>
                                <p><strong>Email:</strong> {Email}</p>
                                <div style="background: #fdf0ed; padding: 15px; border-radius: 5px; margin-top: 15px;">
                                    {Message}
                                </div>
                            </div>
                            <div style="margin-top: 15px; text-align: center; color: #666;">
                                <p>This is a priority message that requires your attention</p>
                            </div>
                        </div>
                        """,
                        IsHtml = true
                    }
                }
            };
        }

        public EmailTemplate GetTemplate(PredefinedTemplate template)
        {
            if (_templates.TryGetValue(template, out var emailTemplate))
            {
                return emailTemplate;
            }

            return _templates[PredefinedTemplate.Default];
        }
    }
} 
