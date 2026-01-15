using System.Diagnostics.CodeAnalysis;
using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
