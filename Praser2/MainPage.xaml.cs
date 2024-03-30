using Services;
using System.Text;

namespace Praser2
{
    public partial class MainPage : ContentPage
    {
        Parser parser;

        public MainPage()
        {
            InitializeComponent();
            parser = new Parser();
            var template = new DataTemplate(() =>
            {
                var index = new Label { FontSize = 14, Margin = 10, HorizontalOptions = LayoutOptions.Center };
                index.SetBinding(Label.TextProperty, new Binding { Path = "Index", StringFormat = "{0}" });

                var name = new Label { FontSize = 14, Margin = 10, HorizontalOptions = LayoutOptions.Center };
                name.SetBinding(Label.TextProperty, new Binding { Path = "Name", StringFormat = "{0}" });

                var count = new Label { FontSize = 14, Margin = 10, HorizontalOptions = LayoutOptions.Center };
                count.SetBinding(Label.TextProperty, new Binding { Path = "Count", StringFormat = "{0}" });

                var grid = new Grid
                {
                    Padding = 5
                };
                grid.Add(index, 0, 0);
                grid.Add(name, 1, 0);
                grid.Add(count, 3, 0);
                grid.SetColumnSpan(name, 2);
                return grid;
            });
            List.ItemTemplate = template;
        }

        private async void OnClicked(object sender, EventArgs e)
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick source code"
            });
            if (file == null) return;
            string text;
            using (FileStream fstream = new(file.FullPath.ToString(), FileMode.Open))
            {
                byte[] buffer = new byte[fstream.Length];
                await fstream.ReadAsync(buffer);
                text = Encoding.Default.GetString(buffer);
            }
            var results = parser.CountGilbeMetric(text);
            List.ItemsSource = results.Item1;
            operators.Text = $"Общее количество операторов: {results.Item2}";
            cond_operators.Text = $"Количество условных операторов: {results.Item3}";
            nesting.Text = $"Максимальный уровень вложенности условного оператора: {results.Item4}";
            cl.Text = $"Насыщенность программы условными операторами: {results.Item5}";
        }
    }

}
