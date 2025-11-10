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

            //builder.HasData(
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("e5d8947f-6794-42b6-ba67-201f366128b8"),
            //        Fullname = "Admin",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("3fe77296-fdb3-4d71-8b99-ef8380c32037"),
            //        Fullname = "Moderator",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("23879117-e09e-40f1-b78f-1493d81baf49"),
            //        Fullname = "Player1",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("91b106fa-7b95-480f-a12a-0e0303454332"),
            //        Fullname = "Player2",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("537f05fd-120c-40b0-b2ec-639756f866ab"),
            //        Fullname = "Player3",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("293191b7-f7b2-4f28-8857-5afa96866a2f"),
            //        Fullname = "Developer1",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("34670beb-a794-4419-adf8-0465eea22a78"),
            //        Fullname = "Developer2",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    },
            //    new UserProfiles
            //    {
            //        UserId = Guid.Parse("c25dc5ef-4e98-421e-90d3-7eb76ba269fe"),
            //        Fullname = "Developer3",
            //        Avatar = "https://media.istockphoto.com/vectors/default-profile-picture-avatar-photo-placeholder-vector-illustration-vector-id1223671392?k=6&m=1223671392&s=170667a&w=0&h=zP3l7WJinOFaGb2i1F4g8IS2ylw0FlIaa6x3tP9sebU=",
            //        Birthday = DateOnly.ParseExact("2002-01-23", "yyyy-MM-dd"),
            //    }
            //);
        }
    }
}
