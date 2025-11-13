using Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interface
{
    public interface ILeopardProfileRepository
    {
        IEnumerable<LeopardProfile> GetAllWithType();
        LeopardProfile GetById(int id);
        void Add(LeopardProfile handbag);
        void Update(LeopardProfile handbag);
        void Delete(int id);
        IEnumerable<IGrouping<string, LeopardProfile>> Search(string modelName, string material);
    }

}
