using Microsoft.AspNetCore.Identity;
using WebApit4s.Identity;
namespace WebApit4s.Services

{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            string[] roles = new[] { "Admin", "Parent", "Employee", "Guest" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
            }

            string adminEmail = "admin@time4wellbeing.com";
            string password = "Admin@123";
            string adminRole = "Admin";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserType = UserType.Admin,
                    IsApprovedByAdmin = true
                };

                var createResult = await userManager.CreateAsync(adminUser, password);

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
            else
            {
                // ✅ Fix existing admin if created without role or UserType
                if (!await userManager.IsInRoleAsync(existingAdmin, adminRole))
                {
                    await userManager.AddToRoleAsync(existingAdmin, adminRole);
                }

                if (existingAdmin.UserType != UserType.Admin || !existingAdmin.IsApprovedByAdmin)
                {
                    existingAdmin.UserType = UserType.Admin;
                    existingAdmin.IsApprovedByAdmin = true;
                    await userManager.UpdateAsync(existingAdmin);
                }
            }
        }
    }

}
