

using Angur.Domain.Customers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Angur.Infrastructure.Database.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.ComplexProperty(customer => customer.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("first_name").HasMaxLength(FullName.MaxPartLength);
            name.Property(n => n.LastName).HasColumnName("last_name").HasMaxLength(FullName.MaxPartLength);
        });

        builder.Property(customer => customer.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(Email.MaxPartLength);

        builder.HasIndex(customer => customer.Email).IsUnique();

        builder.Property(customer => customer.KycStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(customer => customer.KycRejectionReason).HasMaxLength(500);

        builder.Property<uint>("Version").IsRowVersion();
    }
}
