namespace RestaurantManagementGUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("NoTitleBar", (handler, view) =>
            {
#if WINDOWS
    var nativeWindow = handler.PlatformView;
    IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    
    if (appWindow.TitleBar != null)
    {
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
    }
#endif
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new LoginPage()));
        }
    }
}