using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_BLL.Response
{
    public class LeopardProfileResponse
    {
        public int LeopardProfileId { get; set; }
        public string LeopardName { get; set; } = null!;
        public string? Characteristics { get; set; }
        public double? Weight { get; set; }
        public string? CareNeeds { get; set; }
        public string? ModifiedDate { get; set; }

        public int LeopardTypeId { get; set; }
    }
}
