using Microsoft.EntityFrameworkCore;
using Server.API.Entities;

namespace Server.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PageEntity> Pages { get; set; }
}