using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace VotingSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<Election> Elections { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<BlockedVoteAttempt> BlockedVoteAttempts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Vote is reachable from Election both directly and via Candidate.
            // SQL Server rejects two cascade-delete paths, so turn one off.
            modelBuilder.Entity<Vote>()
                .HasRequired(v => v.Election)
                .WithMany(e => e.Votes)
                .HasForeignKey(v => v.ElectionId)
                .WillCascadeOnDelete(false);
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
