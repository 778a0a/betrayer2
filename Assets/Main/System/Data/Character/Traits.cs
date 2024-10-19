using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Flags]
public enum Traits
{
    None = 0,
    Knight = 1 << 0,
    Drillmaster = 1 << 1,
    Pirate = 1 << 2,
    Admiral = 1 << 3,
    Hunter = 1 << 4,
    Mountaineer = 1 << 5,
    Merchant = 1 << 6,
    DivineSpeed = 1 << 7,
}
