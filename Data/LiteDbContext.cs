using LiteDB;
using ActivationCodeApi.Models;

namespace ActivationCodeApi.Data;

public class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _database;

    public LiteDbContext(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        
        // Ensure indexes
        ActivationCodes.EnsureIndex(x => x.Code, true); // Unique index on Code
        AdminUsers.EnsureIndex(x => x.Username, true);  // Unique index on Username
    }

    public ILiteCollection<ActivationCode> ActivationCodes => _database.GetCollection<ActivationCode>("activationCodes");
    
    public ILiteCollection<AdminUser> AdminUsers => _database.GetCollection<AdminUser>("adminUsers");

    public void Dispose()
    {
        _database?.Dispose();
    }

    // Helper method to check if database can connect
    public bool CanConnect()
    {
        try
        {
            // Try to access a collection to verify connection
            _ = ActivationCodes.Count();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
