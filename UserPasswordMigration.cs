using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Models;

public static class UserPasswordMigration
{
    public static async Task MigratePlainTextPasswordsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var users = await context.Users
            .Where(u => u.Active)
            .ToListAsync();

        foreach (var user in users)
        {
            // ΠΡΟΣΟΧΗ:
            // Τρέχει μόνο αν το PasswordHash περιέχει ακόμη plain text.
            // Καλό είναι να το κάνεις μία φορά και μετά να το αφαιρέσεις.
            if (!string.IsNullOrWhiteSpace(user.PasswordHash) &&
                !user.PasswordHash.StartsWith("AQAAAA"))
            {
                user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
            }
        }

        await context.SaveChangesAsync();
    }
}