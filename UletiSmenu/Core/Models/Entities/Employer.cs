using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Employer : User
    {
        public string Name { get; private set; }
        public PIB PIB { get; private set; }
        public MB MB { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid? SubscriptionId { get; private set; }
        public DateTime? SubscriptionStart { get; private set; }
        public DateTime? SubscriptionStop { get; private set; }

        public Employer() : base() {}

        private Employer(Guid id, string name, string email, string username, string phoneNumber, string? profilePhoto,
                         PIB pib, MB mb, Guid companyId, Guid? subscriptionId, DateTime? subscriptionStart, DateTime? subscriptionStop)
            : base(id, email, username, phoneNumber, profilePhoto) {
            Name = name;
            PIB = pib;
            MB = mb;
            CompanyId = companyId;
            SubscriptionId = subscriptionId;
            SubscriptionStart = subscriptionStart;
            SubscriptionStop = subscriptionStop;
        }

        public static Result<Employer> Create(Guid id, string name, string email, string username, string phoneNumber, string? profilePhoto,
                                              PIB pib, MB mb, Guid companyId, Guid? subscriptionId, DateTime? subscriptionStart, DateTime? subscriptionStop)
        {

            var userResult = User.Create(id, email, username, phoneNumber, profilePhoto);
            if (userResult.IsFailure)
                return Result.Failure<Employer>(userResult.Error);

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Employer>("Name cannot be empty.");

            if (string.IsNullOrWhiteSpace(pib.Value))
                return Result.Failure<Employer>("PIB cannot be empty.");

            if (string.IsNullOrWhiteSpace(mb.Value))
                return Result.Failure<Employer>("MB cannot be empty.");

            var employer = new Employer(id, name, email, username, phoneNumber, profilePhoto, pib, mb, companyId, subscriptionId, subscriptionStart, subscriptionStop);
            return Result.Success(employer);
        }
    }
}
