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
    public class GradeConfiguration : IEntityTypeConfiguration<Grade>
    {
        public void Configure(EntityTypeBuilder<Grade> builder)
        {
            builder.ToTable("Grade");
            builder.HasKey(g => g.GradeId);

            builder.Property(g => g.Score).IsRequired();
            builder.Property(g => g.Comment);
            builder.Property(g => g.GradedAt).IsRequired();

            builder.HasOne(g => g.Submission)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Examiner)
                .WithMany()
                .HasForeignKey(g => g.ExaminerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
