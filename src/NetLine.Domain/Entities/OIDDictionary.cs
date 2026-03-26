using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLine.Domain.Entities
{
    internal class OIDDictionary
    {

            // System Group (RFC 1213)
            public const string SysDescr = ".1.3.6.1.2.1.1.1.0";
            public const string SysName = ".1.3.6.1.2.1.1.5.0";
            public const string SysLocation = ".1.3.6.1.2.1.1.6.0";
            public const string SysContact = ".1.3.6.1.2.1.1.4.0";
            public const string SysUpTime = ".1.3.6.1.2.1.1.3.0";

        // Interfaces Group
        public const string SysInterfacesCount = ".1.3.6.1.2.1.2.1.0";
    }
}
