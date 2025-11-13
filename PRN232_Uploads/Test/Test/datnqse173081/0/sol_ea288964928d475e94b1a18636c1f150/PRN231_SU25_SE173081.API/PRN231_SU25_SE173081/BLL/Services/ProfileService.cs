using BLL.DTOs;
using DLL;
using DLL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ProfileService(ProfileRepo repo)
    {
        public async Task<List<ProfileResponse>> GetAll()
        {
            var raw = await repo.GetAll();
            var response = raw.Select(x => new ProfileResponse()
            {
                LeopardProfileId = x.LeopardProfileId,
                CareNeeds = x.CareNeeds,
                Characteristics = x.Characteristics,
                LeopardName = x.LeopardName,
                LeopardTypeId = x.LeopardTypeId,
                LeopardTypeName = x.LeopardType?.LeopardTypeName,
                LeopardTypeDescription = x.LeopardType?.Description,
                LeopardTypeOrigin = x.LeopardType?.Origin,
                ModifiedDate = x.ModifiedDate,
                Weight = x.Weight
            }).ToList();
            return response;
        }
        public async Task<ProfileResponse?> GetById(int id)
        {
            var x = await repo.GetById(id);
            if (x == null)
            {
                return null;
            }
            return new ProfileResponse()
            {
                LeopardProfileId = x.LeopardProfileId,
                CareNeeds = x.CareNeeds,
                Characteristics = x.Characteristics,
                LeopardName = x.LeopardName,
                LeopardTypeId = x.LeopardTypeId,
                LeopardTypeName = x.LeopardType?.LeopardTypeName,
                LeopardTypeDescription = x.LeopardType?.Description,
                LeopardTypeOrigin = x.LeopardType?.Origin,
                ModifiedDate = x.ModifiedDate,
                Weight = x.Weight
            };
        }

        public async Task Add(ProfileRequest x)
        {
            var list = await repo.GetAll();
            var count = list.Count();
            var create = new LeopardProfile()
            {
                CareNeeds = x.CareNeeds,
                Characteristics = x.Characteristics,
                LeopardName = x.LeopardName,
                LeopardTypeId = x.LeopardTypeId,
                ModifiedDate = x.ModifiedDate,
                Weight = x.Weight
            };
            await repo.Add(create);
        }

        public async Task Update(int id, ProfileRequest x)
        {
            var item = await repo.GetById(id);
            item.CareNeeds = x.CareNeeds;
            item.Characteristics = x.Characteristics;
            item.LeopardName = x.LeopardName;
            item.LeopardTypeId = x.LeopardTypeId;
            item.ModifiedDate = x.ModifiedDate;
            item.Weight = x.Weight;

            await repo.Update(item);
        }
        public async Task Delete(int id)
        {
            var item = await repo.GetById(id);
            await repo.Delete(item);
        }
        public IQueryable<ProfileResponse> GetHandbagsQueryable()
        {
            return repo.GetProfilesQueryable()
                .Select(x => new ProfileResponse()
                {
                    LeopardProfileId = x.LeopardProfileId,
                    CareNeeds = x.CareNeeds,
                    Characteristics = x.Characteristics,
                    LeopardName = x.LeopardName,
                    LeopardTypeId = x.LeopardTypeId,
                    ModifiedDate = x.ModifiedDate,
                    Weight = x.Weight
                });
        }
    }
}
