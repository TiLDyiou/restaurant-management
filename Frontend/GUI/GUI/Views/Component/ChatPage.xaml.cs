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

        private void OnImageTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Image image && image.Source != null)
                {
                    FullScreenImageView.Source = image.Source;
                    FullScreenImageOverlay.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image tap error: {ex.Message}");
            }
        }

        private void OnCloseImageTapped(object sender, EventArgs e)
        {
            FullScreenImageOverlay.IsVisible = false;
            FullScreenImageView.Source = null;
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