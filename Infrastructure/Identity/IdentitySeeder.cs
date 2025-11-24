using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.Common.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            // 1. Tạo role
            var userRole = await EnsureRoleAsync(roleManager, "User");
            var adminRole = await EnsureRoleAsync(roleManager, "Admin");

            // 2. Gán permission cho role User
            await EnsureRolePermissionsAsync(roleManager, userRole, new[]
            {
                Permissions.QnA.AskQuestion,
                Permissions.QnA.Answer,
                Permissions.QnA.Comment,
                Permissions.QnA.Vote,
                Permissions.QnA.Search,
                Permissions.QnA.Delete,
            });

            // 3. Gán permission cho role Admin (User + Admin)
            await EnsureRolePermissionsAsync(roleManager, adminRole, new[]
            {
                Permissions.QnA.AskQuestion,
                Permissions.QnA.Answer,
                Permissions.QnA.Comment,
                Permissions.QnA.Vote,
                Permissions.QnA.Search,
                Permissions.QnA.Delete,
                Permissions.Admin.ViewStats,
                Permissions.Admin.UploadPolicyDocs,
                Permissions.Admin.ManagePolicyDataset,
            });

            // 4. (Optional) tạo admin demo – bạn chưa dùng admin thì cứ để đó
            var adminEmail = "admin@example.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, "Admin@123");
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không tạo được admin user: {errors}");
                }

                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        private static async Task<ApplicationRole> EnsureRoleAsync(
            RoleManager<ApplicationRole> roleManager,
            string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new ApplicationRole { Name = roleName };
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Không tạo được role {roleName}: {errors}");
                }
            }

            return role;
        }

        private static async Task EnsureRolePermissionsAsync(
            RoleManager<ApplicationRole> roleManager,
            ApplicationRole role,
            IEnumerable<string> permissions)
        {
            var currentClaims = await roleManager.GetClaimsAsync(role);

            foreach (var permission in permissions)
            {
                if (!currentClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(
                        role,
                        new Claim("permission", permission));
                }
            }
        }
    }
}
