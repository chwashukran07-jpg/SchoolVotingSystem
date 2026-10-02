using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace VotingSystem.Models
{
    // Creates the database on first run and fills in the roles, the admin
    // account and a few sample students. If you change a model class, the
    // database is dropped and re-created (all data is lost), which is
    // convenient during development.
    public class VotingDbInitializer : DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    {
        public const string AdminUserName = "admin";
        public const string AdminPassword = "Admin123";

        protected override void Seed(ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            foreach (var role in new[] { "Admin", "Voter" })
            {
                if (!roleManager.RoleExists(role))
                    roleManager.Create(new IdentityRole(role));
            }

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            ApplicationUserManager.Configure(userManager);

            if (userManager.FindByName(AdminUserName) == null)
            {
                var admin = new ApplicationUser { UserName = AdminUserName };
                userManager.Create(admin, AdminPassword);
                userManager.AddToRole(admin.Id, "Admin");
            }

            // Sample voter list so the system can be demonstrated straight away.
            // Replace these with your real class list from Admin > Voter List.
            context.Students.Add(new Student { StudentNumber = "2024-0001", FullName = "Student One", Course = "BS Computer Science" });
            context.Students.Add(new Student { StudentNumber = "2024-0002", FullName = "Student Two", Course = "BS Computer Science" });
            context.Students.Add(new Student { StudentNumber = "2024-0003", FullName = "Student Three", Course = "BS Information Technology" });
            context.Students.Add(new Student { StudentNumber = "2024-0004", FullName = "Student Four", Course = "BS Information Technology" });
            context.Students.Add(new Student { StudentNumber = "2024-0005", FullName = "Student Five", Course = "BS Software Engineering" });

            base.Seed(context);
        }
    }
}
