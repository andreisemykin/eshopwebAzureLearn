using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace OrderItemsReserver
{
    public static class FailedOrdersProcessingFunction
    {
        [FunctionName("FailedOrdersProcessingFunction")]
        public static async Task Run([ServiceBusTrigger("itemreserver/$deadletterqueue", Connection = "ServiceBusConnectionString")] string myQueueItem, ILogger log)
        {
            string queueMessage = $"C# ServiceBus queue trigger function processed failed message: {myQueueItem}";
            log.LogInformation(queueMessage);

            using (var client = new SmtpClient())
            {
                var message = new MimeMessage();
                var bodyBuilder = new BodyBuilder();

                // from
                message.From.Add(new MailboxAddress("from_name", "from_email@example.com"));
                // to
                message.To.Add(new MailboxAddress("to_name", "to_email@example.com"));
                // reply to
                message.ReplyTo.Add(new MailboxAddress("reply_name", "reply_email@example.com"));

                message.Subject = "subject";
                bodyBuilder.HtmlBody = queueMessage;
                message.Body = bodyBuilder.ToMessageBody();

                client.Connect("MAIL_SERVER", 465, SecureSocketOptions.SslOnConnect);
                client.Authenticate("USERNAME", "PASSWORD");
                await client.SendAsync(message);
            }
        }
    }
}
