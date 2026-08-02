using Auth.Domain.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAsync(UserManager<User> userManager, IConfiguration configuration)
    {
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            // Do not seed if configuration is missing (e.g., in production without env vars)
            return;
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                // Assign all basic and admin roles
                await userManager.AddToRoleAsync(adminUser, Roles.Student);
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }
    }
}
