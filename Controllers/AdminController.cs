using Microsoft.AspNetCore.Mvc;
using ActivationCodeApi.Data;
using ActivationCodeApi.Models;
using ActivationCodeApi.DTOs;
using ActivationCodeApi.Services;

namespace ActivationCodeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly LiteDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly AdminSetupService _adminSetupService;
    private readonly TokenService _tokenService;

    public AdminController(
        LiteDbContext context, 
        ILogger<AdminController> logger,
        AdminSetupService adminSetupService,
        TokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _adminSetupService = adminSetupService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        var isValid = await _adminSetupService.ValidateCredentialsAsync(request.Username, request.Password);

        if (!isValid)
        {
            _logger.LogWarning($"Failed login attempt for username: {request.Username}");
            return Unauthorized(new AdminLoginResponse
            {
                Success = false,
                Message = "Invalid username or password"
            });
        }

        var token = _tokenService.GenerateToken(request.Username);
        _logger.LogInformation($"Admin user logged in: {request.Username}");

        return Ok(new AdminLoginResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token
        });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var success = await _adminSetupService.ChangePasswordAsync(username, request.OldPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Invalid old password" });
        }

        _logger.LogInformation($"Password changed successfully for user: {username}");
        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("generate-codes")]
    public async Task<ActionResult<object>> GenerateCodes([FromBody] GenerateCodesRequest request)
    {
        var generatedCodes = new List<string>();
        var prefix = string.IsNullOrWhiteSpace(request.Prefix) ? "CODE" : request.Prefix;

        _logger.LogInformation($"Starting generation of {request.Count} activation codes");

        // Generate codes in batches for better performance
        const int batchSize = 1000;
        var totalBatches = (int)Math.Ceiling(request.Count / (double)batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            var currentBatchSize = Math.Min(batchSize, request.Count - (batch * batchSize));
            var batchCodes = new List<ActivationCode>(currentBatchSize);

            for (int i = 0; i < currentBatchSize; i++)
            {
                var code = $"{prefix}-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
                
                batchCodes.Add(new ActivationCode
                {
                    Code = code,
                    IsUsed = false
                });
                
                generatedCodes.Add(code);
            }

            _context.ActivationCodes.InsertBulk(batchCodes);
            
            _logger.LogInformation($"Generated batch {batch + 1}/{totalBatches} ({currentBatchSize} codes)");
        }

        _logger.LogInformation($"Successfully generated {request.Count} activation codes");

        return Ok(new 
        { 
            message = $"Successfully generated {request.Count} activation codes",
            count = request.Count,
            prefix = prefix,
            codes = request.Count <= 100 ? generatedCodes : null, // Only return codes if count is small
            note = request.Count > 100 ? "Use GET /api/admin/codes to retrieve the generated codes" : null
        });
    }

    [HttpGet("codes")]
    public Task<ActionResult<PagedCodesResponse>> GetAllCodes(
        [FromQuery] bool? isUsed = null,
        [FromQuery] int? skipToken = null,
        [FromQuery] int pageSize = 100)
    {
        // Validate page size
        if (pageSize < 1 || pageSize > 1000)
        {
            return Task.FromResult<ActionResult<PagedCodesResponse>>(BadRequest(new { message = "Page size must be between 1 and 1000" }));
        }

        // Build query
        var query = _context.ActivationCodes.Query();

        if (isUsed.HasValue)
        {
            query = query.Where(c => c.IsUsed == isUsed.Value);
        }

        // Apply skip token (last seen ID)
        if (skipToken.HasValue)
        {
            query = query.Where(c => c.Id > skipToken.Value);
        }

        // Get total count for the filtered query
        var totalCount = query.Count();

        // Get codes ordered by ID
        var codes = query
            .OrderBy(c => c.Id)
            .Limit(pageSize + 1) // Take one extra to check if there are more
            .ToList();

        var hasMore = codes.Count > pageSize;
        if (hasMore)
        {
            codes = codes.Take(pageSize).ToList();
        }

        var response = new PagedCodesResponse
        {
            Codes = codes,
            TotalCount = totalCount,
            PageSize = pageSize,
            NextSkipToken = hasMore && codes.Any() ? codes.Last().Id : null,
            HasMore = hasMore
        };

        return Task.FromResult<ActionResult<PagedCodesResponse>>(Ok(response));
    }

    [HttpGet("stats")]
    public Task<ActionResult<CodeStatsResponse>> GetStats()
    {
        var totalCodes = _context.ActivationCodes.Count();
        var unusedCodes = _context.ActivationCodes.Count(c => !c.IsUsed);
        var usedCodes = _context.ActivationCodes.Count(c => c.IsUsed);
        var now = DateTime.UtcNow;
        var activeCodes = _context.ActivationCodes
            .Count(c => c.IsUsed && c.ExpiresAt.HasValue && c.ExpiresAt.Value > now);

        return Task.FromResult<ActionResult<CodeStatsResponse>>(Ok(new CodeStatsResponse
        {
            TotalCodes = totalCodes,
            UnusedCodes = unusedCodes,
            UsedCodes = usedCodes,
            ActiveCodes = activeCodes
        }));
    }

    [HttpDelete("codes/{code}")]
    public Task<ActionResult> DeleteCode(string code)
    {
        var activationCode = _context.ActivationCodes.FindOne(c => c.Code == code);

        if (activationCode == null)
        {
            return Task.FromResult<ActionResult>(NotFound(new { message = "Code not found" }));
        }

        _context.ActivationCodes.Delete(activationCode.Id);

        _logger.LogInformation($"Deleted activation code: {code}");
        return Task.FromResult<ActionResult>(Ok(new { message = "Code deleted successfully" }));
    }

    [HttpDelete("codes/expired")]
    public Task<ActionResult> DeleteExpiredCodes()
    {
        var now = DateTime.UtcNow;
        var expiredCodes = _context.ActivationCodes
            .Find(c => c.IsUsed && c.ExpiresAt != null && c.ExpiresAt < now)
            .ToList();

        foreach (var code in expiredCodes)
        {
            _context.ActivationCodes.Delete(code.Id);
        }

        _logger.LogInformation($"Deleted {expiredCodes.Count} expired codes");
        return Task.FromResult<ActionResult>(Ok(new { message = $"Deleted {expiredCodes.Count} expired codes" }));
    }

    [HttpPost("codes/batch-delete")]
    public async Task<ActionResult<BatchDeleteResponse>> BatchDeleteCodes([FromBody] BatchDeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pattern))
        {
            return BadRequest(new BatchDeleteResponse
            {
                Success = false,
                Message = "Pattern is required"
            });
        }

        try
        {
            // 验证正则表达式
            var regex = new System.Text.RegularExpressions.Regex(request.Pattern);

            // 获取所有激活码
            var allCodes = _context.ActivationCodes.FindAll().ToList();

            // 使用正则表达式匹配
            var matchedCodes = allCodes
                .Where(c => regex.IsMatch(c.Code))
                .ToList();

            var matchedCodeStrings = matchedCodes.Select(c => c.Code).ToList();

            // 如果是试运行，只返回匹配结果
            if (request.DryRun)
            {
                _logger.LogInformation($"Dry run: Found {matchedCodes.Count} codes matching pattern '{request.Pattern}'");
                
                return Ok(new BatchDeleteResponse
                {
                    Success = true,
                    Message = $"Dry run completed. Found {matchedCodes.Count} matching codes",
                    MatchedCount = matchedCodes.Count,
                    DeletedCount = 0,
                    MatchedCodes = matchedCodeStrings,
                    WasDryRun = true
                });
            }

            // 实际删除
            if (matchedCodes.Any())
            {
                foreach (var code in matchedCodes)
                {
                    _context.ActivationCodes.Delete(code.Id);
                }
                
                _logger.LogInformation($"Batch deleted {matchedCodes.Count} codes matching pattern '{request.Pattern}'");
            }

            return Ok(new BatchDeleteResponse
            {
                Success = true,
                Message = $"Successfully deleted {matchedCodes.Count} codes",
                MatchedCount = matchedCodes.Count,
                DeletedCount = matchedCodes.Count,
                MatchedCodes = matchedCodeStrings,
                WasDryRun = false
            });
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            _logger.LogWarning($"Invalid regex pattern: {request.Pattern}");
            return BadRequest(new BatchDeleteResponse
            {
                Success = false,
                Message = $"Invalid regex pattern: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during batch delete with pattern: {request.Pattern}");
            return StatusCode(500, new BatchDeleteResponse
            {
                Success = false,
                Message = $"Error during batch delete: {ex.Message}"
            });
        }
    }

    [HttpPost("init-database")]
    public Task<ActionResult> InitializeDatabase()
    {
        try
        {
            // LiteDB creates database automatically, just verify connection
            var canConnect = _context.CanConnect();
            if (canConnect)
            {
                _logger.LogInformation("Database initialized successfully");
                return Task.FromResult<ActionResult>(Ok(new { message = "Database initialized successfully" }));
            }
            else
            {
                return Task.FromResult<ActionResult>(StatusCode(500, new { message = "Failed to connect to database" }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            return Task.FromResult<ActionResult>(StatusCode(500, new { message = "Failed to initialize database", error = ex.Message }));
        }
    }
}
