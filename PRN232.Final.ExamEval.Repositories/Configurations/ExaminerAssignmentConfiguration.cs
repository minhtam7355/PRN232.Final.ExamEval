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
    public class ExaminerAssignmentConfiguration : IEntityTypeConfiguration<ExaminerAssignment>
    {
        public void Configure(EntityTypeBuilder<ExaminerAssignment> builder)
        {
            builder.ToTable("ExaminerAssignment");
            builder.HasKey(ea => ea.ExaminerAssignmentId);

            builder.Property(ea => ea.ExamId).IsRequired();
            builder.Property(ea => ea.ExaminerId).IsRequired();

            builder.HasOne(ea => ea.Exam)
                .WithMany(e => e.ExaminerAssignments)
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ea => ea.Examiner)
                .WithMany()
                .HasForeignKey(ea => ea.ExaminerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
