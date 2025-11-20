using LiteDB;

namespace ActivationCodeApi.Models;

public class ActivationCode
{
    [BsonId(autoId: true)]
    public int Id { get; set; }
    
    public string Code { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ValidationCount { get; set; } = 0;
    public DateTime? LastValidatedAt { get; set; }
}
