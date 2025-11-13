using System;
using System.Collections.Generic;

namespace PRN231_SU25_SE171121_DAL.Entities;

public partial class LeopardProfile
{
    public int LeopardProfileId { get; set; }

    public int LeopardTypeId { get; set; }

    public string LeopardName { get; set; } = null!;

    public double Weight { get; set; }

    public string Characteristics { get; set; } = null!;

    public string CareNeeds { get; set; } = null!;

    public DateTime ModifiedDate { get; set; }

    public virtual LeopardType LeopardType { get; set; } = null!;
}
