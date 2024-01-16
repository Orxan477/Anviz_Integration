using Microsoft.EntityFrameworkCore;

namespace Anviz_Integration_Api.Model
{
	public class AppDbContext:DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
		{

		}

		public DbSet<Log> Logs { get; set; }
	}
}

