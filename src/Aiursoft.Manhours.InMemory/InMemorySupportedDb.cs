using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Aiursoft.Manhours.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Manhours.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<ManhoursDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override ManhoursDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}
