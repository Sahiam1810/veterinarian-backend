using Domain.ChatUserProfiles.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.ChatUserProfiles.Configuration;

public sealed class ChatUserProfileConfiguration : IEntityTypeConfiguration<ChatUserProfileEntity>
{
    public void Configure(EntityTypeBuilder<ChatUserProfileEntity> builder)
    {
        builder.ToTable("CHAT_USER_PROFILES");

        builder.HasKey(profile => profile.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(profile => profile.Id)
            .HasColumnName("CHAT_USER_PROFILE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(profile => profile.PersonId)
            .HasColumnName("PERSON_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(profile => profile.PersonId);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(profile => profile.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(profile => profile.DisplayName)
            .HasColumnName("DISPLAY_NAME")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(ProfileDisplayName.MaxLength)
            .HasConversion(
                name => name.Value,
                value => ProfileDisplayName.Create(value))
            .IsRequired(false);

        builder.Property(profile => profile.AvatarUrl)
            .HasColumnName("AVATAR_URL")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(ProfileAvatarUrl.MaxLength)
            .HasConversion(
                url => url.Value,
                value => ProfileAvatarUrl.Create(value))
            .IsRequired(false);

        builder.Property(profile => profile.Bio)
            .HasColumnName("BIO")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(ProfileBio.MaxLength)
            .HasConversion(
                bio => bio.Value,
                value => ProfileBio.Create(value))
            .IsRequired(false);

        builder.Property(profile => profile.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(profile => profile.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
