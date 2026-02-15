using Microsoft.EntityFrameworkCore;
using soat.eleven.kutcut.infra.Mappings;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<VideoModel> Videos => Set<VideoModel>();
        public DbSet<StatusModel> Statuses => Set<StatusModel>();
        public DbSet<NotificationModel> Notifications => Set<NotificationModel>();
        public DbSet<NotificationTypeModel> NotificationTypes => Set<NotificationTypeModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply Fluent API Mappings
            modelBuilder.ApplyConfiguration(new StatusMapping());
            modelBuilder.ApplyConfiguration(new NotificationTypeMapping());
            modelBuilder.ApplyConfiguration(new VideoMapping());
            modelBuilder.ApplyConfiguration(new NotificationMapping());

            // Seed Data - Status
            modelBuilder.Entity<StatusModel>().HasData(
                new StatusModel { Id = StatusEnum.Pendente, Description = "Pendente" },
                new StatusModel { Id = StatusEnum.Uploaded, Description = "Uploaded" },
                new StatusModel { Id = StatusEnum.EmProcessamento, Description = "Em processamento" },
                new StatusModel { Id = StatusEnum.ProcessadoComSucesso, Description = "Processado com sucesso" },
                new StatusModel { Id = StatusEnum.ProcessadoComErro, Description = "Processado com erro" }
            );

            // Seed Data - NotificationType
            modelBuilder.Entity<NotificationTypeModel>().HasData(
                new NotificationTypeModel { Id = NotificationTypeEnum.ProcessadoComSucesso, Description = "Processado com sucesso" },
                new NotificationTypeModel { Id = NotificationTypeEnum.ProcessadoComErro, Description = "Processado com erro" }
            );
        }
    }
}
