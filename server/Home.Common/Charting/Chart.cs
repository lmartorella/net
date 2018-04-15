
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucky.Charting
{
    public class CategoryChart
    {
        private PlotModel _plotModel;
        private CategoryAxis _categoryAxis;

        public CategoryChart(string title, string xAxisName, string yAxisName)
        {
            var transparent = OxyColor.FromArgb(0, 0, 0, 0);
            _plotModel = new PlotModel { Title = title, PlotAreaBorderColor = transparent };
            _categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = xAxisName, TicklineColor = transparent };
            _plotModel.Axes.Add(_categoryAxis);
            _plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = yAxisName });
        }

        public void AddSerie(IEnumerable<Tuple<double, string>> values)
        {
            var serie = new ColumnSeries { FillColor = OxyColor.FromRgb(132, 132, 132) };
            serie.ItemsSource = values.Select(v => new ColumnItem(v.Item1));
            _categoryAxis.ItemsSource = values.Select(v => v.Item2);
            _plotModel.Series.Add(serie);
        }

        public Stream ToPng(int width, int height)
        {
            var exporter = new OxyPlot.Wpf.PngExporter { Height = height, Width = width, Background = OxyColors.Transparent };
            MemoryStream ms = new MemoryStream();
            exporter.Export(_plotModel, ms);
            ms.Position = 0;
            return ms;
        }
    }
}
