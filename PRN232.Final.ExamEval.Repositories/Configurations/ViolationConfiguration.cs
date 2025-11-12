using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Configurations
{
    public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
    {
        public void Configure(EntityTypeBuilder<Violation> builder)
        {
            builder.ToTable("Violation");
     
            builder.HasKey(v => v.ViolationId);

            builder.Property(v => v.Type).IsRequired();
            builder.Property(v => v.Description);
            builder.Property(v => v.IsConfirmed).IsRequired();

            builder.HasOne(v => v.Submission)
                .WithMany(s => s.Violations)
                .HasForeignKey(v => v.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
