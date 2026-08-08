using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextVent.Core.Enums;
using NextVent.Core.Models;
using NextVent.Data;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using Serilog;

namespace NextVent.Services.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _dbContext;

    public AttendanceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasActiveClockInAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid)) return false;

        return await _dbContext.Attendances
            .AnyAsync(a => a.UserId == guid && a.CheckOutTime == null && a.Status == AttendanceStatus.Active);
    }

    public async Task<AttendanceEntity?> GetActiveAttendanceAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid)) return null;

        return await _dbContext.Attendances
            .FirstOrDefaultAsync(a => a.UserId == guid && a.CheckOutTime == null && a.Status == AttendanceStatus.Active);
    }

    public async Task<AttendanceResultModel> ClockInAsync(string userId, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
        {
            return new AttendanceResultModel { IsSuccess = false, ErrorMessage = "ID de usuario inválido o formato incorrecto." };
        }

        bool alreadyClockedIn = await HasActiveClockInAsync(userId);
        if (alreadyClockedIn)
        {
            return new AttendanceResultModel { IsSuccess = false, ErrorMessage = "Ya cuentas con un registro de entrada activo." };
        }

        var entry = new AttendanceEntity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = guid,
            CheckInTime = DateTime.UtcNow,
            Status = AttendanceStatus.Active,
            TerminalName = Environment.MachineName,
            Notes = notes
        };

        _dbContext.Attendances.Add(entry);
        await _dbContext.SaveChangesAsync();

        return new AttendanceResultModel { IsSuccess = true, AttendanceId = entry.Id };
    }

    public async Task<AttendanceResultModel> ClockOutAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
        {
            return new AttendanceResultModel { IsSuccess = false, ErrorMessage = "ID de usuario inválido." };
        }

        // PREVENT CLOCK-OUT IF A CASH REGISTER SHIFT IS STILL OPEN
        bool hasOpenShift = await _dbContext.Shifts
            .AnyAsync(s => s.IsOpen == 1 && s.EndTime == null);

        if (hasOpenShift)
        {
            return new AttendanceResultModel
            {
                IsSuccess = false,
                ErrorMessage = "No puedes registrar salida sin antes realizar el Corte de Caja y cerrar tu turno activo."
            };
        }

        var activeAttendance = await _dbContext.Attendances
            .FirstOrDefaultAsync(a => a.UserId == guid && a.CheckOutTime == null);

        if (activeAttendance == null)
        {
            return new AttendanceResultModel { IsSuccess = false, ErrorMessage = "No tienes un registro de entrada activo." };
        }

        activeAttendance.CheckOutTime = DateTime.UtcNow;
        activeAttendance.Status = AttendanceStatus.Completed;
        _dbContext.Attendances.Update(activeAttendance);
        await _dbContext.SaveChangesAsync();

        return new AttendanceResultModel { IsSuccess = true, AttendanceId = activeAttendance.Id };
    }

    public async Task AutoCloseForgottenAttendancesAsync()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var forgotten = await _dbContext.Attendances
                .Where(a => a.CheckOutTime == null && a.CheckInTime < cutoff && a.Status == AttendanceStatus.Active)
                .ToListAsync();

            foreach (var att in forgotten)
            {
                att.CheckOutTime = att.CheckInTime.AddHours(8); // cap at standard 8h
                att.Status = AttendanceStatus.IncompleteAnomaly;
                att.Notes += " [Auto-cerrado por el sistema: Anomaly > 24h]";
                _dbContext.Attendances.Update(att);
            }

            if (forgotten.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
                Log.Information("Auto-closed {Count} unclosed attendances older than 24 hours.", forgotten.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AutoCloseForgottenAttendancesAsync");
        }
    }
}
