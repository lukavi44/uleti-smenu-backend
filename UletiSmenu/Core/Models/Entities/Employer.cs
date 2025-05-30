﻿using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;
using System.Net;

namespace Core.Models.Entities
{
    public class Employer : User
    {
        public string Name { get; private set; }
        public PIB PIB { get; private set; }
        public MB MB { get; private set; }
        public Guid? SubscriptionId { get; private set; }
        public DateTime? SubscriptionStart { get; private set; }
        public DateTime? SubscriptionStop { get; private set; }
        public Address Address { get; private set; }
        public ICollection<JobPost> Posts { get; private set; } = new List<JobPost>();

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
    }
}
