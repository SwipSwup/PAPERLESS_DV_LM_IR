using Microsoft.EntityFrameworkCore;
using DAL.Models;
using System.Collections.Generic;

namespace DAL
{
    public class PaperlessDBContext : DbContext
    {
        public PaperlessDBContext(DbContextOptions<PaperlessDBContext> options)
            : base(options) { }

        public DbSet<DocumentEntity> Documents { get; set; } = null!;
        public DbSet<TagEntity> Tags { get; set; } = null!;
        public DbSet<AccessLogEntity> AccessLogs { get; set; } = null!;
        public DbSet<DocumentLogEntity> DocumentLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Document-Tag many-to-many
            modelBuilder.Entity<DocumentEntity>()
                .HasMany(d => d.Tags)
                .WithMany(t => t.Documents)
                .UsingEntity<Dictionary<string, object>>(
                    "DocumentTag",
                    j => j.HasOne<TagEntity>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<DocumentEntity>().WithMany().HasForeignKey("DocumentId"));

            // AccessLog -> Document one-to-many
            modelBuilder.Entity<AccessLogEntity>()
                .HasOne(al => al.DocumentEntity)
                .WithMany(d => d.AccessLogs)
                .HasForeignKey(al => al.DocumentId);

            // DocumentLog -> Document one-to-many
            modelBuilder.Entity<DocumentLogEntity>()
                .HasOne(dl => dl.DocumentEntity)
                .WithMany(d => d.Logs)
                .HasForeignKey(dl => dl.DocumentId);
        }
    }
}