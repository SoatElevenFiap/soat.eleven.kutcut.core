using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Mappings
{
    public class NotificationMapping : IEntityTypeConfiguration<NotificationModel>
    {
        public void Configure(EntityTypeBuilder<NotificationModel> builder)
        {
            builder.ToTable("notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedOnAdd();

            builder.Property(n => n.UserId)
                .HasColumnName("user_id")
                .HasColumnType("uuid");

            builder.Property(n => n.VideoId)
                .HasColumnName("video_id")
                .HasColumnType("uuid");

            builder.Property(n => n.Type)
                .HasColumnName("type")
                .HasConversion<int?>();

            builder.Property(n => n.Description)
                .HasColumnName("description")
                .HasColumnType("varchar");

            builder.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            builder.HasOne(n => n.Video)
                .WithMany(v => v.Notifications)
                .HasForeignKey(n => n.VideoId)
                .HasConstraintName("fk_notifications_video_id")
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(n => n.TypeNavigation)
                .WithMany(nt => nt.Notifications)
                .HasForeignKey(n => n.Type)
                .HasConstraintName("fk_notifications_type")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
