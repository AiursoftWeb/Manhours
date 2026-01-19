using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Aiursoft.DbTools.MySql;
using Aiursoft.Manhours.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Manhours.MySql;

[ExcludeFromCodeCoverage]
public class MySqlSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<ManhoursDbContext>
{
    public override string DbType => "MySql";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurMySqlWithCache<MySqlContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override ManhoursDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MySqlContext>();
    }
}
