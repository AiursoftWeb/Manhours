using Aiursoft.Manhours.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Manhours.MySql;

public class MySqlContext(DbContextOptions<MySqlContext> options) : TemplateDbContext(options);
