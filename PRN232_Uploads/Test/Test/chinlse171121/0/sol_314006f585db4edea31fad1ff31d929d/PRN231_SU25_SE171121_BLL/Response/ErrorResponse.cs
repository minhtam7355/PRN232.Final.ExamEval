using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_BLL.Response
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

}
