

using Angur.Domain.Accounts;
using Angur.Domain.Common;
using Angur.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Angur.Infrastructure.Database.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .ValueGeneratedNever();

        builder.Property(account => account.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value));

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(account => account.Number)
            .HasConversion(number => number.Value, value => AccountNumber.Create(value).Value)
            .HasMaxLength(AccountNumber.Length)
            .IsFixedLength();

        builder.HasIndex(account => account.Number).IsUnique();

        builder.ComplexProperty(account => account.Balance, balance =>
        {
            balance.Property(money => money.Amount).HasColumnName("balance").HasPrecision(19, 4);
            balance.Property(money => money.Currency)
                .HasColumnName("currency")
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value)
                .HasMaxLength(3)
                .IsFixedLength();
        });

        builder.Ignore(account => account.Currency);

        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(account => account.FreezeReason).HasMaxLength(500);

        builder.Property<uint>("Version").IsRowVersion();
    }
}
