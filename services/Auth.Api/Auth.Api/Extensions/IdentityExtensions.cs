using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityConfig(this IServiceCollection services)
    {
        services
            .AddIdentity<User, IdentityRole>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = true;
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opt.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()                    // be able to add roles
            .AddRoleManager<RoleManager<IdentityRole>>() // be able to make use of RoleManager
            .AddEntityFrameworkStores<AuthDbContext>()    // provide our context to Identity
            .AddSignInManager<SignInManager<User>>()     // sign in users
            .AddUserManager<UserManager<User>>()         // create users
            .AddDefaultTokenProviders();                 // tokens for email confirmation

        return services;
    }
}
