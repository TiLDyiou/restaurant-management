using RestaurantManagementGUI.ViewModels;
using System.Collections.Specialized;

namespace RestaurantManagementGUI
{
    public partial class ChatPage : ContentPage
    {
        public ChatPage()
        {
            InitializeComponent();
            var vm = new ChatViewModel();
            BindingContext = vm;

            vm.CurrentMessages.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try { MessagesList.ScrollTo(vm.CurrentMessages.Last(), position: ScrollToPosition.End, animate: true); } catch { }
                    });
                }
            };
        }
    }
}