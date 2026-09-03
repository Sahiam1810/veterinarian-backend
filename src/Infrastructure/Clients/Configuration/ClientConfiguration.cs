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

        // Un usuario no puede tener dos perfiles de cliente -- si no, /clients/me
        // (GetByUserIdAsync + FirstOrDefault) sería no determinístico.
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
                phone => phone == null ? null : phone.Value,
                str => ClientPhoneNumber.CreateOptional(str))
            .IsRequired(false);

        builder.Property(client => client.RegistrationDate)
            .HasColumnName("REGISTRATION_DATE")
            .IsRequired();

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
