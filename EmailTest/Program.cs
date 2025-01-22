using Library;

string connectionString = Environment.GetEnvironmentVariable("EmailConnectionString") ?? "";

EmailService.SendEmailNotification("This is a test of the emergency broadcasting system!", connectionString);