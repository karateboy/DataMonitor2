using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class DataMonitor2Context(DbContextOptions<DataMonitor2Context> options) : IdentityDbContext<DataMonitor2.Data.ApplicationUser>(options)
{
}
