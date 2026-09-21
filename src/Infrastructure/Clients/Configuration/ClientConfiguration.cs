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
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(client => client.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired(false);

        builder.Property(client => client.FullName)
            .HasColumnName("FULL_NAME")
            .HasColumnType("VARCHAR2(150)")
            .HasConversion(name => name.Value, str => ClientFullName.Create(str))
            .IsRequired();

        builder.Property(client => client.Email)
            .HasColumnName("EMAIL")
            .HasColumnType("VARCHAR2(150)")
            .HasConversion(email => email.Value, str => ClientEmail.Create(str))
            .IsRequired();

        builder.HasIndex(client => client.Email)
            .IsUnique()
            .HasDatabaseName("UX_CLIENTS_EMAIL");

        builder.Property(client => client.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(client => client.IdentificationNumber)
            .HasColumnName("IDENTIFICATION_NUMBER")
            .HasMaxLength(ClientIdentificationNumber.MaxLength)
            .HasConversion(
                idNumber => idNumber.Value,
                str => ClientIdentificationNumber.Create(str))
            .IsRequired();

        builder.HasIndex(client => client.IdentificationNumber)
            .IsUnique();

        // Único, pero USER_ID es opcional: Oracle admite varios NULL en un índice único.
        builder.HasIndex(client => client.UserId)
            .IsUnique();

        builder.Property(client => client.Address)
            .HasColumnName("ADDRESS")
            .HasMaxLength(ClientAddress.MaxLength)
            .HasConversion(
                address => address.Value,
                str => ClientAddress.Create(str))
            .IsRequired(false);

        builder.Property(client => client.PhoneNumber)
            .HasColumnName("PHONE_NUMBER")
            .HasMaxLength(ClientPhoneNumber.MaxLength)
            .HasConversion(
                phone => phone.Value,
                str => ClientPhoneNumber.Create(str))
            .IsRequired();

        // UNIQUE obligatorio: el teléfono es requerido, sin filtro.
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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
