using Microsoft.EntityFrameworkCore;
using SecretVault.Models;

namespace SecretVault.Data;

public class VaultDbContext : DbContext
{
    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options) { }

    public DbSet<VaultEntry> VaultEntries { get; set; }
}