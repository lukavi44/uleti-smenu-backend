using Core.Models;
using Core.Models.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<RestaurantLocation> RestaurantLocations { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Favourite> Favourites { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ConversationReadState> ConversationReadStates { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<MatchReview> MatchReviews { get; set; }
        public DbSet<PaymentEvent> PaymentEvents { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Entity<User>()
            .HasDiscriminator<string>("UserRole")
            .HasValue<Employee>("Employee")
            .HasValue<Employer>("Employer");
        }
    }
}
