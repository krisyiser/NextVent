using NextVent.Data.Dtos;

namespace NextVent.Services.Interfaces;

/// <summary>Promotion rule CRUD and activation toggle.</summary>
public interface IPromotionService
{
    Task<List<PromotionDto>> GetAllAsync();
    Task<List<PromotionDto>> GetActiveAsync();
    Task SaveAsync(PromotionDto promotion);
    Task DeleteAsync(string id);
    Task<List<CartItemDto>> EvaluateAndApplyPromotionsAsync(List<CartItemDto> cartItems);
}

/// <summary>Parked/paused order CRUD.</summary>
public interface IParkedOrderService
{
Task<List<ParkedOrderDto>> GetAllAsync();
Task SaveAsync(ParkedOrderDto order);
Task DeleteAsync(string id);
}

/// <summary>System user CRUD with RBAC.</summary>
public interface IUserService
{
Task<int> GetCountAsync();
Task<List<UserDto>> GetAllAsync();
Task<UserDto?> GetByNameAsync(string name);
Task<List<UserDto>> GetManagersAsync();
Task SaveAsync(string id, string nombre, string rol, string? passwordHash, string? pinHash);
Task DeleteAsync(string id);
Task<string?> GetPasswordHashAsync(string userId);
Task<string?> GetPinHashAsync(string userId);
}

/// <summary>Key-value settings persistence.</summary>
public interface ISettingsService
{
Task<string?> GetAsync(string key);
Task SetAsync(string key, string value);
Task<Dictionary<string, string>> GetAllAsync();
}

/// <summary>System audit log writer for tamper-evident security tracking.</summary>
public interface IAuditService
{
    Task LogAsync(NextVent.Data.Entities.AuditLogEntity log);
    Task LogAsync(string level, string message, string? meta = null);
    Task<List<NextVent.Data.Entities.AuditLogEntity>> GetRecentLogsAsync(int limit = 100);
}

/// <summary>Product co-occurrence engine for combo recommendations.</summary>
public interface IComboEngine
{
Task UpdateCoOccurrencesAsync(List<string> productIds);
Task<List<string>> GetRecommendationsAsync(string productId, int limit = 3);
}
