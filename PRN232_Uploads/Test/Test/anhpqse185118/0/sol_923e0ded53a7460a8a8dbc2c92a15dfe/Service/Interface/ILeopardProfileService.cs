using Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ILeopardProfileService
    {
        IEnumerable<LeopardProfile> GetAll();
        LeopardProfile Get(int id);
        void Create(LeopardProfile handbag);
        void Update(LeopardProfile handbag);
        void Delete(int id);
        IEnumerable<IGrouping<string, LeopardProfile>> Search(string modelName, string material);
    }

}
