using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace Ticketfy.Data;

public static class DbResilienceHelper
{
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<DbUpdateException>()
        .Or<SqliteException>(ex => ex.SqliteErrorCode == 5) // Error 5: SQLITE_BUSY
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100)); // 200ms, 400ms, 800ms

    public static async Task ExecuteWithRetryAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(action);
    }
}
