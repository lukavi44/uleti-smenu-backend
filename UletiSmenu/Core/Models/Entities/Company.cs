using Core.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Entities
{
    public class Company
    {
        public Guid Id { get; }
        public Guid SubscriptionId { get; }
        public string Name { get; }
        public Address Address { get; }
        
        private Company()
        {
        }

        private Company(Guid id, Guid subscriptionId, string name, Address address)
        {
            Id = id;
            SubscriptionId = subscriptionId;
            Name = name;
            Address = address;
        }
    }
}
