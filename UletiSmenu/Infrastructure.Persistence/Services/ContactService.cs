using Core.Interfaces;
using Core.Models.Entities;
using Core.Services;
using CSharpFunctionalExtensions;
using Infrastructure.Persistence.Database;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactService> _logger;

        public ContactService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<ContactService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Result> SubmitAsync(
            string name,
            string email,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            var createResult = ContactMessage.Create(
                Guid.NewGuid(),
                name,
                email,
                subject,
                message,
                DateTime.UtcNow);

            if (createResult.IsFailure)
                return Result.Failure(createResult.Error);

            var contactMessage = createResult.Value;
            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync(cancellationToken);

            var emailSent = await _emailService.SendContactFormAsync(
                contactMessage.Name,
                contactMessage.Email,
                contactMessage.Subject,
                contactMessage.Message,
                cancellationToken);

            contactMessage.MarkEmailSent(emailSent);
            await _context.SaveChangesAsync(cancellationToken);

            if (!emailSent)
            {
                _logger.LogWarning(
                    "Contact message {MessageId} saved but email failed for {Email}",
                    contactMessage.Id,
                    contactMessage.Email);
            }

            // Message is persisted either way so admin inbox still works when SMTP fails.
            return Result.Success();
        }
    }
}
