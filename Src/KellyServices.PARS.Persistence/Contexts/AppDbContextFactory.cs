using KellyServices.PARS.Persistence.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KellyServices.PARS.Persistence.Contexts
{
    public class AppDbContextFactory : DesignTimeDbContextFactoryBase<AppDbContext>
    {
        protected override AppDbContext CreateNewInstance(DbContextOptions<AppDbContext> options)
        {
            return new AppDbContext(options);
        }
    }
}
