using System;
using Microsoft.EntityFrameworkCore;

namespace Ticketfy.Data;

public class AppDbContextFactoryImpl<TContext> : IDbContextFactory<TContext> where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options;

    public AppDbContextFactoryImpl(DbContextOptions<TContext> options)
    {
        _options = options;
    }

    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), _options)!;
    }
}
