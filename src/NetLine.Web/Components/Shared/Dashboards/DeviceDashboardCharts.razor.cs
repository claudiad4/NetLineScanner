using ApexCharts;
using Microsoft.AspNetCore.Components;
using NetLine.Application.DTO.Dashboards;

namespace NetLine.Web.Components.Shared.Dashboards;

public partial class DeviceDashboardCharts : ComponentBase
{
    [Parameter] public DeviceDashboardDto? Dashboard { get; set; }

    private readonly List<AlertTypeCountDto> alertReadSeries = [];

    private readonly ApexChartOptions<PingLatencyPointDto> latencyOptions = new()
    {
        Chart = new Chart { Toolbar = new Toolbar { Show = true } },
        Stroke = new Stroke { Curve = Curve.Smooth, Width = 2 },
        Xaxis = new XAxis { Type = XAxisType.Datetime },
        Yaxis = [new YAxis { Title = new AxisTitle { Text = "ms" } }],
        Tooltip = new Tooltip { X = new TooltipX { Format = "dd MMM yyyy HH:mm" } }
    };

    private readonly ApexChartOptions<AlertTypeCountDto> readStatusOptions = new()
    {
        Labels = ["Przeczytane", "Nieprzeczytane"],
        Legend = new Legend { Position = LegendPosition.Bottom }
    };

    private readonly ApexChartOptions<AlertTypeCountDto> alertTypeOptions = new()
    {
        PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar { Horizontal = false, BorderRadius = 4 }
        },
        Xaxis = new XAxis { Title = new AxisTitle { Text = "Typ alertu" } },
        Yaxis = [new YAxis { Title = new AxisTitle { Text = "Liczba" } }]
    };

    protected override void OnParametersSet()
    {
        alertReadSeries.Clear();

        if (Dashboard is null)
        {
            return;
        }

        alertReadSeries.Add(new AlertTypeCountDto
        {
            AlertType = "Przeczytane",
            Count = Dashboard.AlertReadStats.ReadCount
        });

        alertReadSeries.Add(new AlertTypeCountDto
        {
            AlertType = "Nieprzeczytane",
            Count = Dashboard.AlertReadStats.UnreadCount
        });
    }
}
