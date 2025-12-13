using System.Windows.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Admin;
using RestaurantManagementGUI.Views.Authentication;
using RestaurantManagementGUI.Views.Staff;

namespace RestaurantManagementGUI.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IUserSession _userSession;
        private readonly IServiceProvider _serviceProvider;

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LogoutCommand { get; }
        public ICommand NavigateUsersCommand { get; }
        public ICommand NavigateFoodCategoriesCommand { get; }
        public ICommand NavigateFoodMenuCommand { get; }
        public ICommand NavigateOrdersCommand { get; }
        public ICommand NavigateTablesCommand { get; }
        public ICommand NavigateBillCommand { get; }
        public ICommand NavigateRevenueCommand { get; }
        public ICommand NavigateChatCommand { get; }

        public DashboardViewModel(IUserSession userSession, IServiceProvider serviceProvider)
        {
            _userSession = userSession;
            _serviceProvider = serviceProvider;

            // Khởi tạo lời chào
            UpdateUserInfo();

            // Khởi tạo Commands
            LogoutCommand = new Command(async () => await ExecuteLogout());
            NavigateUsersCommand = new Command(async () => await ExecuteNavigateUsers());

            // Các lệnh điều hướng đơn giản
            NavigateFoodCategoriesCommand = new Command(async () => await NavigateTo<QuanLyMonAnPage>());
            NavigateFoodMenuCommand = new Command(async () => await NavigateTo<FoodMenuPage>());
            NavigateOrdersCommand = new Command(async () => await NavigateTo<OrdersPage>());
            NavigateTablesCommand = new Command(async () => await NavigateTo<TablesPage>());
            NavigateBillCommand = new Command(async () => await NavigateTo<BillGenerationPage>());
            NavigateRevenueCommand = new Command(async () => await NavigateTo<RevenueReportPage>());
            NavigateChatCommand = new Command(async () => await NavigateTo<ChatPage>());
        }

        public void UpdateUserInfo()
        {
            // Lấy tên từ Session (đã lưu lúc LoginViewModel)
            string name = !string.IsNullOrEmpty(_userSession.TenNV) ? _userSession.TenNV : "User";
            WelcomeMessage = $"Xin chào, {name}";
        }

        private async Task ExecuteNavigateUsers()
        {
            // Logic cũ: Admin -> UsersPage, Khác -> ChefAndUserProfilePage
            if (_userSession.IsAdmin)
            {
                await NavigateTo<UsersPage>();
            }
            else
            {
                await NavigateTo<ChefAndUserProfilePage>();
            }
        }

        private async Task ExecuteLogout()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Có", "Không");
            if (!confirm) return;

            // Xóa dữ liệu
            SecureStorage.RemoveAll();
            _userSession.Clear();

            // Ngắt Socket (nếu có logic này trong SocketListener, gọi ở đây)
            // SocketListener.Instance.Disconnect();

            // Về trang Login
            Application.Current.MainPage = new NavigationPage(new LoginPage(_serviceProvider.GetService<LoginViewModel>()));
        }

        // Hàm điều hướng Generic giúp code gọn hơn
        private async Task NavigateTo<T>() where T : Page
        {
            // Lấy Page từ DI Container để đảm bảo nó có ViewModel của nó
            var page = _serviceProvider.GetService<T>();
            if (page != null)
            {
                await Application.Current.MainPage.Navigation.PushAsync(page);
            }
        }
    }
}