using System.Threading.Tasks;

namespace Ticketfy.Services.Interfaces;

public interface IBackupService
{
    Task<bool> CreateZCutBackupAsync(string shiftReference);
}
