// App.xaml.cs - Application initialization
namespace EegMonitor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
            //Application.CreateWindow();
        }
    }
}
