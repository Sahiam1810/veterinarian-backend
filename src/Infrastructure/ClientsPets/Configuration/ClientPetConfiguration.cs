using Domain.ClientsPets.Entities;
using Domain.ClientsPets.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ClientsPets.Configuration;

public sealed class ClientPetConfiguration : IEntityTypeConfiguration<ClientPetEntity>
{
    public void Configure(EntityTypeBuilder<ClientPetEntity> builder)
    {
        builder.ToTable("CLIENTS_PETS");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("CLIENT_PET_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value)).IsRequired().ValueGeneratedNever();
        builder.Property(x => x.ClientId).HasColumnName("CLIENT_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(x => x.PetId).HasColumnName("PET_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(x => x.IsPrimaryOwner).HasColumnName("IS_PRIMARY_OWNER").HasColumnType("CHAR(1)").HasMaxLength(1)
            .HasConversion(owner => owner.Value ? "Y" : "N", value => PrimaryOwner.Create(value == "Y")).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(x => new { x.ClientId, x.PetId }).IsUnique();
        builder.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Pet).WithMany().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Restrict);
    }
}
