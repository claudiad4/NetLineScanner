using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace NetLine.Infrastructure;

public class AppUser : IdentityUser
{
    public int? OfficeId { get; set; }
}
