using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Dtos;
using NextVent.Data.Entities;
using NextVent.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.Services.Implementations;

public class ShiftNoteService : IShiftNoteService
{
    private readonly AppDbContext _db;

    public ShiftNoteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ShiftNoteDto>> GetActiveNotesAsync()
    {
        try
        {
            var notes = await _db.ShiftNotes
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return notes.Select(n => new ShiftNoteDto(
                n.Id,
                n.CashierName,
                n.CreatedAt,
                n.NoteText,
                n.Category,
                n.IsResolved
            )).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting shift notes");
            return [];
        }
    }

    public async Task<bool> SaveNoteAsync(string cashierName, string noteText, string category = "General")
    {
        if (string.IsNullOrWhiteSpace(noteText)) return false;
        try
        {
            var note = new ShiftNoteEntity
            {
                Id = Guid.NewGuid().ToString(),
                CashierName = string.IsNullOrWhiteSpace(cashierName) ? "CAJERO" : cashierName,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                NoteText = noteText.Trim(),
                Category = category,
                IsResolved = false
            };

            _db.ShiftNotes.Add(note);
            await _db.SaveChangesAsync();
            Log.Information("Saved shift note from {Cashier}: {Text}", cashierName, noteText);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving shift note");
            return false;
        }
    }

    public async Task<bool> UpdateNoteAsync(string id, string noteText, string category = "General")
    {
        try
        {
            var note = await _db.ShiftNotes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null) return false;

            note.NoteText = noteText.Trim();
            note.Category = category;
            await _db.SaveChangesAsync();
            Log.Information("Updated shift note {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating shift note {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteNoteAsync(string id)
    {
        try
        {
            var note = await _db.ShiftNotes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null) return false;

            _db.ShiftNotes.Remove(note);
            await _db.SaveChangesAsync();
            Log.Information("Deleted shift note {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting shift note {Id}", id);
            return false;
        }
    }

    public async Task<bool> ResolveNoteAsync(string id)
    {
        try
        {
            var note = await _db.ShiftNotes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null) return false;

            note.IsResolved = !note.IsResolved;
            await _db.SaveChangesAsync();
            Log.Information("Toggled resolution status for shift note {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving shift note {Id}", id);
            return false;
        }
    }
}
