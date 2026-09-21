using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Clients.Configuration;

public sealed class ClientConfiguration : IEntityTypeConfiguration<ClientEntity>
{
    public void Configure(EntityTypeBuilder<ClientEntity> builder)
    {
        builder.ToTable("CLIENTS");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .HasColumnName("CLIENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(client => client.FullName)
            .HasColumnName("FULL_NAME")
            .HasColumnType("VARCHAR2(150)")
            .HasConversion(name => name.Value, value => ClientFullName.Create(value))
            .IsRequired();

        builder.Property(client => client.Email)
            .HasColumnName("EMAIL")
            .HasColumnType("VARCHAR2(150)")
            .HasConversion(email => email.Value, value => ClientEmail.Create(value))
            .IsRequired();

        builder.HasIndex(client => client.Email)
            .IsUnique()
            .HasDatabaseName("UX_CLIENTS_EMAIL");

        builder.Property(client => client.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasColumnType("NUMBER(1)")
            .HasConversion(
                value => value ? 1 : 0,
                value => value == 1)
            .IsRequired();

        builder.Property(client => client.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(client => client.IdentificationNumber)
            .HasColumnName("IDENTIFICATION_NUMBER")
            .HasMaxLength(ClientIdentificationNumber.MaxLength)
            .HasConversion(
                idNumber => idNumber.Value,
                value => ClientIdentificationNumber.Create(value))
            .IsRequired();

        builder.HasIndex(client => client.IdentificationNumber)
            .IsUnique();

        // Oracle permite varios NULLs; el índice único queda para el caso no nulo.
        builder.HasIndex(client => client.UserId)
            .IsUnique();

        builder.Property(client => client.Address)
            .HasColumnName("ADDRESS")
            .HasMaxLength(ClientAddress.MaxLength)
            .HasConversion(
                address => address.Value,
                value => ClientAddress.Create(value))
            .IsRequired(false);

        builder.Property(client => client.PhoneNumber)
            .HasColumnName("PHONE_NUMBER")
            .HasMaxLength(ClientPhoneNumber.MaxLength)
            .HasConversion(
                phone => phone.Value,
                value => ClientPhoneNumber.Create(value))
            .IsRequired();

        builder.HasIndex(client => client.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("UX_CLIENTS_PHONE_NUMBER");

        builder.Property(client => client.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(client => client.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .IsRequired(false);

        builder.HasOne(client => client.User)
            .WithMany()
            .HasForeignKey(client => client.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
