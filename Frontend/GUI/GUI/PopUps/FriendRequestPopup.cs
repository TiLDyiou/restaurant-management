using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.PopUps
{
    public class FriendRequestPopup : ContentPage
    {
        private CollectionView requestList;
        private ObservableCollection<FriendRequestModel> requests;
        public event Action<FriendRequestModel> OnRequestAccepted;
        public event Action<FriendRequestModel> OnRequestRejected;

        public FriendRequestPopup(ObservableCollection<FriendRequestModel> pendingRequests)
        {
            requests = pendingRequests;

            Title = "Lời mời kết bạn";
            BackgroundColor = Colors.White;

            var mainLayout = new VerticalStackLayout { Padding = 20, Spacing = 15 };

            requestList = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemsSource = requests,
                ItemTemplate = new DataTemplate(() =>
                {
                    var grid = new Grid
                    {
                        Padding = 15,
                        RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                        BackgroundColor = Color.FromArgb("#F5F5F5"),
                        Margin = new Thickness(0, 10)
                    };

                    var nameLabel = new Label
                    {
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold
                    };
                    nameLabel.SetBinding(Label.TextProperty, "SenderName");

                    var codeLabel = new Label
                    {
                        FontSize = 12,
                        TextColor = Colors.Gray
                    };
                    codeLabel.SetBinding(Label.TextProperty, new Binding("SenderCode", stringFormat: "Mã NV: {0}"));

                    var buttonStack = new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    var acceptButton = new Button
                    {
                        Text = "Chấp nhận",
                        BackgroundColor = Color.FromArgb("#4CAF50"),
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };
                    acceptButton.Clicked += async (s, e) =>
                    {
                        var btn = s as Button;
                        var request = btn?.BindingContext as FriendRequestModel;
                        if (request != null)
                        {
                            OnRequestAccepted?.Invoke(request);
                            if (requests.Count == 0)
                            {
                                await Navigation.PopModalAsync();
                            }
                        }
                    };

                    var rejectButton = new Button
                    {
                        Text = "Từ chối",
                        BackgroundColor = Color.FromArgb("#F44336"),
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    };
                    rejectButton.Clicked += async (s, e) =>
                    {
                        var btn = s as Button;
                        var request = btn?.BindingContext as FriendRequestModel;
                        if (request != null)
                        {
                            OnRequestRejected?.Invoke(request);
                            if (requests.Count == 0)
                            {
                                await Navigation.PopModalAsync();
                            }
                        }
                    };

                    buttonStack.Children.Add(acceptButton);
                    buttonStack.Children.Add(rejectButton);

                    grid.Children.Add(nameLabel);
                    Grid.SetRow(nameLabel, 0);
                    grid.Children.Add(codeLabel);
                    Grid.SetRow(codeLabel, 1);
                    grid.Children.Add(buttonStack);
                    Grid.SetRow(buttonStack, 2);

                    return grid;
                })
            };

            var closeButton = new Button
            {
                Text = "Đóng",
                BackgroundColor = Color.FromArgb("#9E9E9E"),
                TextColor = Colors.White
            };
            closeButton.Clicked += async (s, e) => await Navigation.PopModalAsync();

            mainLayout.Children.Add(new Label { Text = "Lời mời kết bạn", FontSize = 18, FontAttributes = FontAttributes.Bold });
            mainLayout.Children.Add(new ScrollView { Content = requestList, VerticalOptions = LayoutOptions.FillAndExpand });
            mainLayout.Children.Add(closeButton);

            Content = mainLayout;
        }
    }
}