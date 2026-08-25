using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options) : DbContext(options)
{
    
}
