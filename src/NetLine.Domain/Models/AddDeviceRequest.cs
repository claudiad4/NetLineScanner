using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Domain.Models;

public record AddDeviceRequest(
    string Ip,
    string UserLabel,
    string Type = "Other"
);
