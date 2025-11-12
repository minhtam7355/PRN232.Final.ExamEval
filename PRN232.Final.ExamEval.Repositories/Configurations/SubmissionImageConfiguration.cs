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
    public class SubmissionImageConfiguration : IEntityTypeConfiguration<SubmissionImage>
    {
        public void Configure(EntityTypeBuilder<SubmissionImage> builder)
        {
            builder.ToTable("SubmissionImage");
     
            builder.HasKey(si => si.SubmissionImageId);

            builder.Property(si => si.ImagePath).IsRequired();

            builder.HasOne(si => si.Submission)
                .WithMany(s => s.Images)
                .HasForeignKey(si => si.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
