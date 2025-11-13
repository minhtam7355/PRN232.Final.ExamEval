using PRN231_SU25_SE171121_BLL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_BLL.Interfaces
{
    public interface ILeopardProfileService
    {
        Task<IEnumerable<LeopardProfileResponse>> GetAllAsync();
        Task<bool> DeleteAsync(int id);
    }
}
