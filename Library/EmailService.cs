using Azure;
using Azure.Communication.Email;

namespace Library;

public static class EmailService
{
    public static void SendEmailNotification(string message, string connectionString)
    {
        Console.WriteLine($"Sending email with message: {message}");

        var emailClient = new EmailClient(connectionString);
        var emailContent = new EmailContent(message)
        {
            PlainText = message
        };
        var emailAddresses = new List<EmailAddress> { new("saintgimp@hotmail.com", "Eric Lee") };
        var emailRecipients = new EmailRecipients(emailAddresses);
        var emailMessage = new EmailMessage("DoNotReply@a036e2cc-5e5b-4ece-bf78-dcbd50ba6554.azurecomm.net", emailRecipients, emailContent);
        emailClient.Send(WaitUntil.Started, emailMessage, CancellationToken.None);
    }
}
