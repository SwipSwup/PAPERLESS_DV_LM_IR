using Microsoft.EntityFrameworkCore;
using DAL.Models;
using System.Collections.Generic;

namespace DAL
{
    public class PaperlessDBContext : DbContext
    {
        public PaperlessDBContext(DbContextOptions<PaperlessDBContext> options)
            : base(options) { }

        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<AccessLog> AccessLogs { get; set; } = null!;
        public DbSet<DocumentLog> DocumentLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Document-Tag many-to-many
            modelBuilder.Entity<Document>()
                .HasMany(d => d.Tags)
                .WithMany(t => t.Documents)
                .UsingEntity<Dictionary<string, object>>(
                    "DocumentTag",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<Document>().WithMany().HasForeignKey("DocumentId"));

            // AccessLog -> Document one-to-many
            modelBuilder.Entity<AccessLog>()
                .HasOne(al => al.Document)
                .WithMany(d => d.AccessLogs)
                .HasForeignKey(al => al.DocumentId);

            // DocumentLog -> Document one-to-many
            modelBuilder.Entity<DocumentLog>()
                .HasOne(dl => dl.Document)
                .WithMany(d => d.Logs)
                .HasForeignKey(dl => dl.DocumentId);
        }
    }
}