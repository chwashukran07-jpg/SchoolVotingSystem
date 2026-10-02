using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using VotingSystem.Models;

[assembly: OwinStartup(typeof(VotingSystem.Startup))]

namespace VotingSystem
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // One DbContext / UserManager / SignInManager per request.
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Logged-in users are remembered with a cookie; anonymous users
            // hitting an [Authorize] page are sent to the login form.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });
        }
    }
}
