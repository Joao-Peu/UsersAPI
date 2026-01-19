using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsersAPI.Domain.Entities;

namespace UsersAPI.Infrastructure.Persistence;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
               .IsUnique();

        builder.OwnsOne(u => u.Password, b =>
        {
            b.Property(p => p.HashValue)
                .HasColumnName("Password")
                .IsRequired()
                .HasMaxLength(255);
        });

        builder.Property("Role")
               .HasConversion<string>()
               .HasColumnName("Role")
               .IsRequired();
    }
}