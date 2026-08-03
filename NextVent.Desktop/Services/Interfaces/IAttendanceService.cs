using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Data.Entities;

namespace NextVent.Services.Interfaces;

public interface IAttendanceService
{
    Task<bool> HasActiveClockInAsync(string userId);
    Task<AttendanceEntity?> GetActiveAttendanceAsync(string userId);
    Task<AttendanceResultModel> ClockInAsync(string userId, string notes = "");
    Task<AttendanceResultModel> ClockOutAsync(string userId);
    Task AutoCloseForgottenAttendancesAsync();
}
