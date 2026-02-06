using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Mappings
{
    public class NotificationTypeMapping : IEntityTypeConfiguration<NotificationTypeModel>
    {
        public void Configure(EntityTypeBuilder<NotificationTypeModel> builder)
        {
            builder.ToTable("notification_type");

            builder.HasKey(nt => nt.Id);

            builder.Property(nt => nt.Id)
                .HasColumnName("id")
                .HasConversion<int>()
                .ValueGeneratedNever();

            builder.Property(nt => nt.Description)
                .HasColumnName("description")
                .HasColumnType("varchar");
        }
    }
}
