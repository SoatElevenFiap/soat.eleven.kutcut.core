using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Mappings
{
    public class VideoMapping : IEntityTypeConfiguration<VideoModel>
    {
        public void Configure(EntityTypeBuilder<VideoModel> builder)
        {
            builder.ToTable("videos");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            builder.Property(v => v.Title)
                .HasColumnName("title")
                .HasColumnType("varchar");

            builder.Property(v => v.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(v => v.Filename)
                .HasColumnName("filename")
                .HasColumnType("varchar")
                .IsRequired();

            builder.Property(v => v.Status)
                .HasColumnName("status")
                .HasConversion<int?>();

            builder.Property(v => v.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(v => v.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            builder.HasOne(v => v.StatusNavigation)
                .WithMany(s => s.Videos)
                .HasForeignKey(v => v.Status)
                .HasConstraintName("fk_videos_status")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
