using PRN231_SU25_SE171121_BLL.Interfaces;
using PRN231_SU25_SE171121_BLL.Response;
using PRN231_SU25_SE171121_DAL.Entities;
using PRN231_SU25_SE171121_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_BLL.Services
{
    public class LeopardProfileService:ILeopardProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LeopardProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IEnumerable<LeopardProfileResponse>> GetAllAsync()
        {
            var profile = await _unitOfWork.LeopardProfiles.GetAllIncludingAsync(h => h.LeopardType!);
             return profile.Select(b => new LeopardProfileResponse
             {
                 LeopardProfileId = b.LeopardProfileId,
                 LeopardName = b.LeopardName,
                 LeopardTypeId = b.LeopardTypeId,
                 Weight = (double)b.Weight,
                 Characteristics = b.Characteristics,
                 CareNeeds = b.CareNeeds,
                 ModifiedDate = b.ModifiedDate.ToString("yyyy-MM-dd")
             });
         
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _unitOfWork.LeopardProfiles.GetAsync(h => h.LeopardProfileId == id);
            if (entity == null) return false;

            _unitOfWork.LeopardProfiles.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
