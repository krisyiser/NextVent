using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sentry;

namespace Ticketfy.Data.Interceptors;

public class SlowQueryInterceptor : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds > 50)
        {
            // Reportar a la nube silenciosamente
            SentrySdk.CaptureMessage($"Slow Query Detectada: {eventData.Duration.TotalMilliseconds}ms - {command.CommandText}", SentryLevel.Warning);
        }
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        if (eventData.Duration.TotalMilliseconds > 50)
        {
            SentrySdk.CaptureMessage($"Slow Query Detectada (Sync): {eventData.Duration.TotalMilliseconds}ms - {command.CommandText}", SentryLevel.Warning);
        }
        return base.ReaderExecuted(command, eventData, result);
    }
}
