using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.PopUps
{
    public class RemoveFriendPopup : ContentPage
    {
        private CollectionView friendList;
        private ObservableCollection<ConversationModel> friends;
        public event Action<ConversationModel> OnFriendRemoved;

        public RemoveFriendPopup(List<ConversationModel> friendsList)
        {
            friends = new ObservableCollection<ConversationModel>(friendsList);

            Title = "Xóa bạn";
            BackgroundColor = Colors.White;

            var mainLayout = new VerticalStackLayout { Padding = 20, Spacing = 15 };

            friendList = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemsSource = friends,
                ItemTemplate = new DataTemplate(() =>
                {
                    var grid = new Grid
                    {
                        Padding = 10,
                        ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = 50 },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                    };

                    var avatar = new Frame
                    {
                        WidthRequest = 40,
                        HeightRequest = 40,
                        CornerRadius = 20,
                        BackgroundColor = Colors.LightGray,
                        Padding = 0,
                        IsClippedToBounds = true,
                        Content = new Label
                        {
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                    ((Label)avatar.Content).SetBinding(Label.TextProperty, "InitialLetter");

                    var infoStack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };

                    var nameLabel = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold };
                    nameLabel.SetBinding(Label.TextProperty, "DisplayName");

                    var statusLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
                    statusLabel.SetBinding(Label.TextProperty, "StatusText");

                    infoStack.Children.Add(nameLabel);
                    infoStack.Children.Add(statusLabel);

                    var removeButton = new Button
                    {
                        Text = "Xóa",
                        BackgroundColor = Color.FromArgb("#F44336"),
                        TextColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = new Thickness(15, 5)
                    };
                    removeButton.Clicked += async (s, e) =>
                    {
                        var btn = s as Button;
                        var friend = btn?.BindingContext as ConversationModel;
                        if (friend != null)
                        {
                            bool answer = await DisplayAlert("Xác nhận",
                                $"Bạn có chắc muốn xóa {friend.DisplayName} khỏi danh sách bạn bè?",
                                "Có", "Không");

                            if (answer)
                            {
                                friends.Remove(friend);
                                OnFriendRemoved?.Invoke(friend);
                            }
                        }
                    };

                    grid.Children.Add(avatar);
                    Grid.SetColumn(avatar, 0);
                    grid.Children.Add(infoStack);
                    Grid.SetColumn(infoStack, 1);
                    grid.Children.Add(removeButton);
                    Grid.SetColumn(removeButton, 2);

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

            mainLayout.Children.Add(new Label { Text = "Danh sách bạn bè", FontSize = 18, FontAttributes = FontAttributes.Bold });
            mainLayout.Children.Add(new ScrollView { Content = friendList, VerticalOptions = LayoutOptions.FillAndExpand });
            mainLayout.Children.Add(closeButton);

            Content = mainLayout;
        }
    }
}
