using Angur.Application.Abstractions;
using Angur.Domain.Accounts;
using Angur.Domain.Customers;
using Angur.Infrastructure.Accounts;

using Microsoft.EntityFrameworkCore;

namespace Angur.Infrastructure.Database;

internal sealed class AngurDbContext(DbContextOptions<AngurDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AngurDbContext).Assembly);
        modelBuilder.HasSequence<long>(AccountNumberGenerator.SequenceName);
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesAsync(cancellationToken);
}
