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
    public class RubricConfiguration : IEntityTypeConfiguration<Rubric>
    {
        public void Configure(EntityTypeBuilder<Rubric> builder)
        {
            builder.ToTable("Rubric");
     
            builder.HasKey(r => r.RubricId);

            builder.Property(r => r.Criteria).IsRequired();
            builder.Property(r => r.MaxScore).IsRequired();

            builder.HasOne(r => r.Exam)
                .WithMany(e => e.Rubrics)
                .HasForeignKey(r => r.ExamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
