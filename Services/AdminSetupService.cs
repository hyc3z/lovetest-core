using ActivationCodeApi.Data;
using ActivationCodeApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace ActivationCodeApi.Services;

public class AdminSetupService
{
    private readonly LiteDbContext _context;
    private readonly ILogger<AdminSetupService> _logger;

    public AdminSetupService(LiteDbContext context, ILogger<AdminSetupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task InitializeAdminAccountAsync()
    {
        // Check if admin account already exists
        var adminExists = _context.AdminUsers.Count() > 0;
        
        if (adminExists)
        {
            _logger.LogInformation("Admin account already exists. Skipping setup.");
            return Task.CompletedTask;
        }

        // Create default admin account with username: admin, password: admin
        var username = "admin";
        var password = "admin";
        var passwordHash = HashPassword(password);

        var adminUser = new AdminUser
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdminUsers.Insert(adminUser);

        _logger.LogInformation("Admin account created with default credentials (username: admin, password: admin)");
        return Task.CompletedTask;
    }

    public Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
    {
        var admin = _context.AdminUsers.FindOne(a => a.Username == username);

        if (admin == null)
        {
            return Task.FromResult(false);
        }

        var oldPasswordHash = HashPassword(oldPassword);
        if (admin.PasswordHash != oldPasswordHash)
        {
            return Task.FromResult(false);
        }

        admin.PasswordHash = HashPassword(newPassword);
        _context.AdminUsers.Update(admin);

        _logger.LogInformation($"Password changed for user: {username}");
        return Task.FromResult(true);
    }

    public Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var admin = _context.AdminUsers.FindOne(a => a.Username == username);

        if (admin == null)
        {
            return Task.FromResult(false);
        }

        var passwordHash = HashPassword(password);
        return Task.FromResult(admin.PasswordHash == passwordHash);
    }

    public string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
