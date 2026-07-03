using Core.Models.Enums;
using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Employer : User
    {
        public string Name { get; private set; }
        public string PublicSlug { get; private set; } = string.Empty;
        public PIB PIB { get; private set; }
        public MB MB { get; private set; }
        public Guid? SubscriptionId { get; private set; }
        public DateTime? SubscriptionStart { get; private set; }
        public DateTime? SubscriptionStop { get; private set; }
        public BillingStatus BillingStatus { get; private set; } = BillingStatus.Incomplete;
        public string? StripeCustomerId { get; private set; }
        public string? StripeSubscriptionId { get; private set; }
        public string? StripePriceId { get; private set; }
        public DateTime? CurrentPeriodEndUtc { get; private set; }
        public DateTime? TrialEndsAtUtc { get; private set; }
        public DateTime? GracePeriodEndsAtUtc { get; private set; }
        public int PostCredits { get; private set; }
        public decimal WalletBalance { get; private set; }
        public string BillingProvider { get; private set; } = "None";
        public bool IsVerifiedEmployer { get; private set; }
        public DateTime? VerifiedAtUtc { get; private set; }
        public Guid? VerifiedByUserId { get; private set; }
        public Address Address { get; private set; }
        public ICollection<JobPost> Posts { get; private set; } = new List<JobPost>();
        public ICollection<RestaurantLocation> Locations { get; private set; } = new List<RestaurantLocation>();

        public Employer() : base() {}

        private Employer(Guid id, string name, string email, string username, string phoneNumber, string? profilePhoto,
                         PIB pib, MB mb, Guid? subscriptionId, DateTime? subscriptionStart, DateTime? subscriptionStop, Address address)
            : base(id, email, username, phoneNumber, profilePhoto) {
            Name = name;
            PIB = pib;
            MB = mb;
            SubscriptionId = subscriptionId;
            SubscriptionStart = subscriptionStart;
            SubscriptionStop = subscriptionStop;
            Address = address;
        }

        public static Result<Employer> Create(Guid id, string name, string email, string username, string phoneNumber, string? profilePhoto,
                                              PIB pib, MB mb, Guid? subscriptionId, DateTime? subscriptionStart, DateTime? subscriptionStop, Address address)
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

            var employer = new Employer(id, name, email, username, phoneNumber, profilePhoto, pib, mb, subscriptionId, subscriptionStart, subscriptionStop, address);
            return Result.Success(employer);
        }

        public static Result<Employer> CreateMinimal(Guid id, string email, string username)
        {
            var userResult = User.Create(id, email, username, string.Empty, string.Empty);
            if (userResult.IsFailure)
                return Result.Failure<Employer>(userResult.Error);

            return Result.Success(new Employer(
                id,
                string.Empty,
                email,
                username,
                string.Empty,
                string.Empty,
                PIB.Empty(),
                MB.Empty(),
                null,
                null,
                null,
                Address.Empty()));
        }

        public bool HasCompletedRequiredProfile() =>
            !string.IsNullOrWhiteSpace(Name) &&
            !string.IsNullOrWhiteSpace(PhoneNumber) &&
            !string.IsNullOrWhiteSpace(PIB?.Value) &&
            !string.IsNullOrWhiteSpace(MB?.Value) &&
            !string.IsNullOrWhiteSpace(Address?.Street?.Name) &&
            !string.IsNullOrWhiteSpace(Address?.Street?.Number) &&
            !string.IsNullOrWhiteSpace(Address?.City?.Name) &&
            !string.IsNullOrWhiteSpace(Address?.City?.PostalCode?.Value) &&
            !string.IsNullOrWhiteSpace(Address?.City?.Country?.Name) &&
            !string.IsNullOrWhiteSpace(Address?.City?.Region?.Name);

        public Result SetPublicSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Result.Failure("Slug cannot be empty.");

            PublicSlug = slug.Trim().ToLowerInvariant();
            return Result.Success();
        }

        public Result AssignSubscription(Guid subscriptionId, DateTime subscriptionStart, DateTime subscriptionStop)
        {
            if (subscriptionId == Guid.Empty)
                return Result.Failure("Subscription ID cannot be empty.");

            if (subscriptionStop <= subscriptionStart)
                return Result.Failure("Subscription end must be after start.");

            SubscriptionId = subscriptionId;
            SubscriptionStart = subscriptionStart;
            SubscriptionStop = subscriptionStop;
            return Result.Success();
        }

        public Result AssignTrial(Guid subscriptionId, DateTime startUtc, DateTime endUtc)
        {
            var result = AssignSubscription(subscriptionId, startUtc, endUtc);
            if (result.IsFailure)
                return result;

            BillingStatus = BillingStatus.Trialing;
            TrialEndsAtUtc = endUtc;
            CurrentPeriodEndUtc = endUtc;
            GracePeriodEndsAtUtc = null;
            return Result.Success();
        }

        public Result ActivatePaidPlan(
            Guid planId,
            DateTime periodStartUtc,
            DateTime periodEndUtc,
            string billingProvider,
            string? stripeCustomerId,
            string? stripeSubscriptionId,
            string? stripePriceId)
        {
            var assignResult = AssignSubscription(planId, periodStartUtc, periodEndUtc);
            if (assignResult.IsFailure)
                return assignResult;

            BillingStatus = BillingStatus.Active;
            BillingProvider = billingProvider;
            StripeCustomerId = stripeCustomerId;
            StripeSubscriptionId = stripeSubscriptionId;
            StripePriceId = stripePriceId;
            CurrentPeriodEndUtc = periodEndUtc;
            TrialEndsAtUtc = null;
            GracePeriodEndsAtUtc = null;
            return Result.Success();
        }

        public void UpdateStripeCustomerId(string customerId)
        {
            StripeCustomerId = customerId;
            BillingProvider = "Stripe";
        }

        public void SyncStripeSubscription(
            BillingStatus status,
            DateTime? currentPeriodEndUtc,
            string? stripeSubscriptionId,
            string? stripePriceId,
            int gracePeriodDays)
        {
            BillingProvider = "Stripe";
            BillingStatus = status;
            StripeSubscriptionId = stripeSubscriptionId;
            StripePriceId = stripePriceId;
            CurrentPeriodEndUtc = currentPeriodEndUtc;

            if (currentPeriodEndUtc.HasValue)
                SubscriptionStop = currentPeriodEndUtc.Value;

            if (status == BillingStatus.PastDue)
            {
                GracePeriodEndsAtUtc ??= DateTime.UtcNow.AddDays(gracePeriodDays);
            }
            else if (status is BillingStatus.Active or BillingStatus.Trialing)
            {
                GracePeriodEndsAtUtc = null;
            }
            else if (status is BillingStatus.Expired or BillingStatus.Incomplete)
            {
                GracePeriodEndsAtUtc = null;
            }
        }

        public void MarkExpired()
        {
            BillingStatus = BillingStatus.Expired;
            GracePeriodEndsAtUtc = null;
        }

        public void ClearSubscription()
        {
            SubscriptionId = null;
            SubscriptionStart = null;
            SubscriptionStop = null;
            BillingStatus = BillingStatus.Incomplete;
            TrialEndsAtUtc = null;
            CurrentPeriodEndUtc = null;
            GracePeriodEndsAtUtc = null;
            StripeSubscriptionId = null;
            StripePriceId = null;
        }

        public void AddPostCredits(int credits)
        {
            if (credits > 0)
                PostCredits += credits;
        }

        public void GrantRegistrationBonus(int credits)
        {
            if (credits > 0)
                PostCredits += credits;

            if (!SubscriptionId.HasValue)
                BillingStatus = BillingStatus.Incomplete;
        }

        public Result ConsumePostCredit(int creditsRequired = 1)
        {
            if (PostCredits < creditsRequired)
                return Result.Failure("Insufficient free posting credits.");

            PostCredits -= creditsRequired;
            return Result.Success();
        }

        public Result CreditWallet(decimal amount)
        {
            if (amount <= 0)
                return Result.Failure("Credit amount must be positive.");

            WalletBalance += amount;
            return Result.Success();
        }

        public Result DebitWallet(decimal amount)
        {
            if (amount <= 0)
                return Result.Failure("Debit amount must be positive.");

            if (WalletBalance < amount)
                return Result.Failure("Insufficient wallet balance.");

            WalletBalance -= amount;
            return Result.Success();
        }

        public bool HasActiveSubscription(DateTime utcNow)
        {
            if (!SubscriptionId.HasValue || !SubscriptionStart.HasValue || !SubscriptionStop.HasValue)
                return false;

            return SubscriptionStart.Value <= utcNow && SubscriptionStop.Value >= utcNow;
        }

        public bool IsWithinGracePeriod(DateTime utcNow) =>
            GracePeriodEndsAtUtc.HasValue && utcNow <= GracePeriodEndsAtUtc.Value;

        public bool CanPostDuringPastDue(DateTime utcNow) =>
            BillingStatus == BillingStatus.PastDue &&
            IsWithinGracePeriod(utcNow) &&
            !string.IsNullOrWhiteSpace(StripeSubscriptionId);

        public Result UpdateProfile(
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure("Name cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure("Phone number cannot be empty.");

            var pibResult = PIB.Create(pib.Trim());
            if (pibResult.IsFailure)
                return Result.Failure(pibResult.Error);

            var mbResult = MB.Create(mb.Trim());
            if (mbResult.IsFailure)
                return Result.Failure(mbResult.Error);

            if (string.IsNullOrWhiteSpace(streetName))
                return Result.Failure("Street name cannot be empty.");

            if (string.IsNullOrWhiteSpace(streetNumber))
                return Result.Failure("Street number cannot be empty.");

            if (string.IsNullOrWhiteSpace(city))
                return Result.Failure("City cannot be empty.");

            if (string.IsNullOrWhiteSpace(postalCode))
                return Result.Failure("Postal code cannot be empty.");

            if (string.IsNullOrWhiteSpace(country))
                return Result.Failure("Country cannot be empty.");

            if (string.IsNullOrWhiteSpace(region))
                return Result.Failure("Region cannot be empty.");

            var streetResult = Street.Create(streetName.Trim(), streetNumber.Trim());
            if (streetResult.IsFailure)
                return Result.Failure(streetResult.Error);

            var postalCodeResult = PostalCode.Create(postalCode.Trim());
            if (postalCodeResult.IsFailure)
                return Result.Failure(postalCodeResult.Error);

            var countryResult = Country.Create(country.Trim());
            if (countryResult.IsFailure)
                return Result.Failure(countryResult.Error);

            var regionResult = Region.Create(region.Trim());
            if (regionResult.IsFailure)
                return Result.Failure(regionResult.Error);

            var cityResult = City.Create(
                city.Trim(),
                postalCodeResult.Value,
                countryResult.Value,
                regionResult.Value);
            if (cityResult.IsFailure)
                return Result.Failure(cityResult.Error);

            var addressResult = Address.Create(streetResult.Value, cityResult.Value);
            if (addressResult.IsFailure)
                return Result.Failure(addressResult.Error);

            Name = name.Trim();
            PhoneNumber = phoneNumber.Trim();
            PIB = pibResult.Value;
            MB = mbResult.Value;
            Address = addressResult.Value;

            return Result.Success();
        }

        public Result SetVerification(bool isVerified, Guid? verifiedByUserId, DateTime utcNow)
        {
            if (isVerified)
            {
                if (verifiedByUserId == null || verifiedByUserId == Guid.Empty)
                    return Result.Failure("Verifier is required when marking employer as verified.");

                IsVerifiedEmployer = true;
                VerifiedAtUtc = utcNow;
                VerifiedByUserId = verifiedByUserId;
                return Result.Success();
            }

            IsVerifiedEmployer = false;
            VerifiedAtUtc = null;
            VerifiedByUserId = null;
            return Result.Success();
        }
    }
}
