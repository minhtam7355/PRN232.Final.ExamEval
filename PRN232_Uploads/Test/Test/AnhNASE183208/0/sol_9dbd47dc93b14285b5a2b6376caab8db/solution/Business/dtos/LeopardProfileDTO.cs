using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.dtos
{
    public class LeopardProfileDTO
    {
        public int LeopardProfileId;
        public int LeopardTypeId;
        public string LeopardName;
        public double Weight;
        public string Characteristics;
        public string CareNeeds;
        public DateTime ModifiedDate;
    }
}
