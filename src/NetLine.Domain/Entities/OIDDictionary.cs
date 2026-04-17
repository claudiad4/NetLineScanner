namespace NetLine.Domain.Entities;

/// <summary>
/// Catalog of SNMP Object Identifiers used by monitoring components.
/// Organized by MIB / functional area.
/// </summary>
public static class OIDDictionary
{
    // System Group (RFC 1213)
    public const string SysDescr = ".1.3.6.1.2.1.1.1.0";
    public const string SysObjectId = ".1.3.6.1.2.1.1.2.0";
    public const string SysUpTime = ".1.3.6.1.2.1.1.3.0";
    public const string SysContact = ".1.3.6.1.2.1.1.4.0";
    public const string SysName = ".1.3.6.1.2.1.1.5.0";
    public const string SysLocation = ".1.3.6.1.2.1.1.6.0";

    // Interfaces Group (IF-MIB)
    public const string SysInterfacesCount = ".1.3.6.1.2.1.2.1.0";
    public const string IfTable = ".1.3.6.1.2.1.2.2.1";
    public const string IfDescr = ".1.3.6.1.2.1.2.2.1.2";
    public const string IfAdminStatus = ".1.3.6.1.2.1.2.2.1.7";
    public const string IfOperStatus = ".1.3.6.1.2.1.2.2.1.8";
    public const string IfInOctets = ".1.3.6.1.2.1.2.2.1.10";
    public const string IfOutOctets = ".1.3.6.1.2.1.2.2.1.16";
    public const string IfInErrors = ".1.3.6.1.2.1.2.2.1.14";
    public const string IfOutErrors = ".1.3.6.1.2.1.2.2.1.20";

    // HOST-RESOURCES-MIB — universal CPU / memory / users
    public const string HrSystemNumUsers = ".1.3.6.1.2.1.25.1.5.0";
    public const string HrSystemProcesses = ".1.3.6.1.2.1.25.1.6.0";
    public const string HrProcessorLoadTable = ".1.3.6.1.2.1.25.3.3.1.2";
    public const string HrStorageTable = ".1.3.6.1.2.1.25.2.3.1";
    public const string HrStorageDescr = ".1.3.6.1.2.1.25.2.3.1.3";
    public const string HrStorageAllocationUnits = ".1.3.6.1.2.1.25.2.3.1.4";
    public const string HrStorageSize = ".1.3.6.1.2.1.25.2.3.1.5";
    public const string HrStorageUsed = ".1.3.6.1.2.1.25.2.3.1.6";

    // UCD-SNMP-MIB — detailed CPU / memory / load average (net-snmp on Linux)
    public const string SsCpuUser = ".1.3.6.1.4.1.2021.11.9.0";
    public const string SsCpuSystem = ".1.3.6.1.4.1.2021.11.10.0";
    public const string SsCpuIdle = ".1.3.6.1.4.1.2021.11.11.0";

    public const string MemTotalReal = ".1.3.6.1.4.1.2021.4.5.0";
    public const string MemAvailReal = ".1.3.6.1.4.1.2021.4.6.0";
    public const string MemTotalFree = ".1.3.6.1.4.1.2021.4.11.0";

    public const string LaLoad1 = ".1.3.6.1.4.1.2021.10.1.3.1";
    public const string LaLoad5 = ".1.3.6.1.4.1.2021.10.1.3.2";
    public const string LaLoad15 = ".1.3.6.1.4.1.2021.10.1.3.3";
}
