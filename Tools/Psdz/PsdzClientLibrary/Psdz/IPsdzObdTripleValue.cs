﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMW.Rheingold.Psdz.Model.Obd
{
    public interface IPsdzObdTripleValue
    {
        string CalId { get; }

        string ObdId { get; }

        string SubCVN { get; }
    }
}
