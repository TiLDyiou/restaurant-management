using RestaurantManagementGUI.ViewModels;
using System.Collections.Specialized;

namespace RestaurantManagementGUI
{
    public partial class ChatPage : ContentPage
    {
        private readonly ChatViewModel _viewModel;

        public ChatPage(ChatViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            _viewModel.CurrentMessages.CollectionChanged += OnCurrentMessagesChanged;
        }

        private void OnCurrentMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Task.Delay(100);
                        if (MessageScrollView != null)
                        {
                            await MessageScrollView.ScrollToAsync(0, MessageScrollView.ContentSize.Height, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Auto scroll error: {ex.Message}");
                    }
                });
            }
        }

        private void OnBackToSidebarClicked(object sender, EventArgs e)
        {
            if (BindingContext is ChatViewModel vm)
            {
                vm.SelectedConversation = null;
            }
        }

        private async void OnImageTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Image image && image.Source is UriImageSource uriSource)
                {
                    string url = uriSource.Uri.ToString();
                    await DisplayAlert("Xem ảnh", "Đường dẫn: " + url, "Đóng");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image tap error: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.CurrentMessages.CollectionChanged -= OnCurrentMessagesChanged;
                _viewModel.ClearSelection();
            }
        }
    }
}