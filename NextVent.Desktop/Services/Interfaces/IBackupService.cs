using System.Threading.Tasks;

namespace NextVent.Services.Interfaces;

public interface IBackupService
{
    Task<bool> CreateZCutBackupAsync(string shiftReference);
}
