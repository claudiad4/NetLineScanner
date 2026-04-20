using ApexCharts;
using Microsoft.AspNetCore.Components;
using NetLine.Application.DTO.Dashboards;

namespace NetLine.Web.Components.Shared.Dashboards;

public partial class OfficeDashboardCharts : ComponentBase
{
    [Parameter] public OfficeDashboardDto? Dashboard { get; set; }

    private readonly ApexChartOptions<DeviceStatusCountDto> healthOptions = new()
    {
        Legend = new Legend { Position = LegendPosition.Bottom }
    };

    private readonly ApexChartOptions<DailyAlertTrendPointDto> trendOptions = new()
    {
        Chart = new Chart { Toolbar = new Toolbar { Show = true } },
        Stroke = new Stroke { Curve = Curve.Smooth, Width = 2 },
        Xaxis = new XAxis { Type = XAxisType.Datetime },
        Yaxis = [new YAxis { Title = new AxisTitle { Text = "Alerty" } }],
        Tooltip = new Tooltip { X = new TooltipX { Format = "dd MMM yyyy" } }
    };

    private readonly ApexChartOptions<DeviceAlertCountDto> topDevicesOptions = new()
    {
        PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar { BorderRadius = 4, Horizontal = false }
        },
        Xaxis = new XAxis { Title = new AxisTitle { Text = "Urządzenie" } },
        Yaxis = [new YAxis { Title = new AxisTitle { Text = "Liczba alertów" } }]
    };
}
