using System;

namespace NetLine.Web.Components.Shared.Devices;

public enum NetworkSubCategory
{
    All,
    Ping,
    Dns,
    Ports,
    Interfaces
}

public static class NetworkSubCategoryFilter
{
    public static bool Matches(string metricKey, NetworkSubCategory sub) => sub switch
    {
        NetworkSubCategory.All        => true,
        NetworkSubCategory.Ping       => metricKey.StartsWith("ping.",   StringComparison.Ordinal),
        NetworkSubCategory.Dns        => metricKey.StartsWith("dns.",    StringComparison.Ordinal),
        NetworkSubCategory.Ports      => metricKey.StartsWith("port.",   StringComparison.Ordinal),
        NetworkSubCategory.Interfaces => metricKey.StartsWith("net.if.", StringComparison.Ordinal),
        _ => true
    };
}
