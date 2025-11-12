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
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfile");

            builder.HasKey(u => u.UserId);
            builder.Property(u => u.Fullname);
            builder.Property(u => u.Avatar);
            builder.Property(u => u.Bio);
            builder.Property(u => u.Birthday);
            builder.Property(u => u.StudentID);
            builder.Property(u => u.EmployeeID);

            builder.HasOne(u => u.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
