using Ticketfy.Data.Dtos;

namespace Ticketfy.Services.Interfaces;

/// <summary>
/// Cash register shift lifecycle: open, close, and query history.
/// </summary>
public interface IShiftService
{
    Task<ShiftDto?> GetActiveAsync();
    Task<ShiftDto> OpenAsync(double openingBalance);
    Task<ShiftDto> CloseAsync(string shiftId, double actualBalance);
    Task<List<ShiftDto>> GetAllAsync();
}
