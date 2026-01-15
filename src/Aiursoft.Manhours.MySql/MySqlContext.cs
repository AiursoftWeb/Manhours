using System.Diagnostics.CodeAnalysis;
using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : TemplateDbContext(options);
