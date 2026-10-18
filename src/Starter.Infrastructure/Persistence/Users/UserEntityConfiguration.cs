using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Starter.Domain.Users;

namespace Starter.Infrastructure.Persistence.Users;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    private const string TableName = "users";
    private const string IdColumnName = "id";
    private const string EmailColumnName = "email";
    private const string FirstNameColumnName = "first_name";
    private const string LastNameColumnName = "last_name";
    private const string CreatedAtColumnName = "created_at";
    private const string PrimaryKeyName = "pk_users";
    internal const string EmailUniqueIndexName = "ix_users_email";

    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(userEntity => userEntity.Id)
            .HasName(PrimaryKeyName);

        builder.Property(userEntity => userEntity.Id)
            .HasColumnName(IdColumnName)
            .ValueGeneratedNever();

        builder.Property(userEntity => userEntity.Email)
            .HasColumnName(EmailColumnName)
            .HasMaxLength(UserConstraints.EmailMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.FirstName)
            .HasColumnName(FirstNameColumnName)
            .HasMaxLength(UserConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.LastName)
            .HasColumnName(LastNameColumnName)
            .HasMaxLength(UserConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.CreatedAt)
            .HasColumnName(CreatedAtColumnName)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(userEntity => userEntity.Email)
            .HasDatabaseName(EmailUniqueIndexName)
            .IsUnique();
    }
}
