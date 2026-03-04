using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DatabaseSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        // 1. Ensure Migrations are applied
        await _context.Database.MigrateAsync();

        // 2. Backfill NormalizedFullName for existing records (one-time after AddNormalizedFullName migration)
        await BackfillNormalizedFullNameAsync();

        // 4. Seed Roles (Priest = الاب الكاهن المسئول — only one user allowed)
        string[] roles = { "Admin", "Manager", "Servant", "Priest", "Secretary" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        // 5. Seed Admin User
        var adminEmail = "admin@youthdatamanager.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = adminEmail,
                FullName = "System Administrator",
                NormalizedFullName = ArabicNormalizer.Normalize("System Administrator"),
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    private async Task BackfillNormalizedFullNameAsync()
    {
        var studentsToUpdate = await _context.Students
            .Where(s => (s.NormalizedFullName == null || s.NormalizedFullName == "") && s.FullName != null && s.FullName != "")
            .ToListAsync();
        foreach (var s in studentsToUpdate)
        {
            s.NormalizedFullName = ArabicNormalizer.Normalize(s.FullName);
        }

        var usersToUpdate = await _context.Users
            .Where(u => (u.NormalizedFullName == null || u.NormalizedFullName == "") && u.FullName != null && u.FullName != "")
            .ToListAsync();
        foreach (var u in usersToUpdate)
        {
            u.NormalizedFullName = ArabicNormalizer.Normalize(u.FullName);
        }

        if (studentsToUpdate.Count > 0 || usersToUpdate.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }
}
