using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Entities;

namespace Ticketfy.Services.Interfaces;

public interface IAttendanceService
{
    Task<bool> HasActiveClockInAsync(string userId);
    Task<AttendanceEntity?> GetActiveAttendanceAsync(string userId);
    Task<AttendanceResultModel> ClockInAsync(string userId, string notes = "");
    Task<AttendanceResultModel> ClockOutAsync(string userId);
    Task AutoCloseForgottenAttendancesAsync();
}
