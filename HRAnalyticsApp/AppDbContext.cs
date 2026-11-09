using HRAnalyticsApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRAnalyticsApp
{
    public class AppDbContext : DbContext
    {
        public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
        public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
        public DbSet<TimeLogEntity> TimeLogs => Set<TimeLogEntity>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DepartmentEntity>(entity =>
            {
                entity.HasKey(d => d.Id); // первинний ключ

                entity.Property(d => d.DepartmentName)
                    .IsRequired()
                    .HasMaxLength(200);
            });
    
            modelBuilder.Entity<EmployeeEntity>(entity =>
            {
                entity.HasKey(e => e.Id); 

                entity.Property(e => e.EmployeeId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => e.EmployeeId).IsUnique(); 

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TimeLogEntity>(entity =>
            {
                entity.HasKey(t => t.Id); 

                entity.Property(t => t.Date).IsRequired();
                entity.Property(t => t.HoursWorked).IsRequired();

                entity.Property(t => t.ProjectCode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(t => t.ProjectCode); // швидка статистика за проєктами (індекс)

                entity.HasOne(t => t.Employee)
                    .WithMany(e => e.TimeLogs)
                    .HasForeignKey(t => t.EmployeeId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}