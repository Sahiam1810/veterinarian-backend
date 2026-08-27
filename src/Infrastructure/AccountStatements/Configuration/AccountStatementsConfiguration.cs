using Domain.AccountStatements.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Infrastructure.AccountStatements.Configuration;

public sealed class AccountStatementsConfiguration
    : IEntityTypeConfiguration<AccountStatementEntity>
{
    public void Configure(EntityTypeBuilder<AccountStatementEntity> builder)
    {
        builder.ToTable("ACCOUNT_STATEMENTS");

        builder.HasKey(statement => statement.Id);

        builder.Property(statement => statement.Id)
            .HasColumnName("STATEMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(statement => statement.AccountId)
            .HasColumnName("ACCOUNT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(statement => statement.IssueDate)
            .HasColumnName("ISSUE_DATE")
            .HasColumnType("DATE")
            .IsRequired();

        builder.Property(statement => statement.Status)
            .HasConversion(
                status => status.Value,
                value  => StatementStatus.Create(value))
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(StatementStatus.MaxLength)
            .IsRequired();

        builder.Property(statement => statement.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(statement => statement.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(statement => statement.AccountId);

        builder.HasOne<UserAccountEntity>()
            .WithMany()
            .HasForeignKey(statement => statement.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
