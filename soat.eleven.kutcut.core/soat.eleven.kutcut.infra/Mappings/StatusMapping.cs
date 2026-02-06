using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Mappings
{
    public class StatusMapping : IEntityTypeConfiguration<StatusModel>
    {
        public void Configure(EntityTypeBuilder<StatusModel> builder)
        {
            builder.ToTable("status");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .HasColumnName("id")
                .HasConversion<int>()
                .ValueGeneratedNever();

            builder.Property(s => s.Description)
                .HasColumnName("description")
                .HasColumnType("varchar");
        }
    }
}
