using Microsoft.UI.Xaml;
using Microsoft.Maui.Controls.Xaml;

namespace EegMonitor.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        /*private void InitializeComponent()
        {
            // Add any necessary initialization logic here.
            // This method is required to resolve the CS1061 error.
        }*/
    }

}
