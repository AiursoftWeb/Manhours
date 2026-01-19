using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : ManhoursDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
