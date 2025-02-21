using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Entities
{
    public class Application
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public Guid JobPostId { get; }
        public string Status { get; }
        public int NumberOfApplicants { get; }
        public DateTime DateTime { get; }

        public Application() { }

        private Application(Guid id, Guid userId, Guid jobPostId, string status, int numberOfApplicants, DateTime dateTime)
        {
            Id = id;
            UserId = userId;
            JobPostId = jobPostId;
            Status = status;
            NumberOfApplicants = numberOfApplicants;
            DateTime = dateTime;
        }
    }
}
