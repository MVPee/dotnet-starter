using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Starter.Domain.Users;

namespace Starter.Infrastructure.Persistence.Users;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    internal const string EmailUniqueIndexName = "ix_users_email";

    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(userEntity => userEntity.Id)
            .HasName("pk_users");

        builder.Property(userEntity => userEntity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(userEntity => userEntity.Email)
            .HasColumnName("email")
            .HasMaxLength(UserConstraints.EmailMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(UserConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(UserConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(userEntity => userEntity.CreatedAt)
            .HasColumnName("created_at")
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(userEntity => userEntity.Email)
            .HasDatabaseName(EmailUniqueIndexName)
            .IsUnique();
    }
}
