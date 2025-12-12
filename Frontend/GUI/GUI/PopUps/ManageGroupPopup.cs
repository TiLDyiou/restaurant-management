using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.PopUps
{
    public class ManageGroupPopup : ContentPage
    {
        private CollectionView groupList;
        private ObservableCollection<ConversationModel> groups;
        public event Action<ConversationModel> OnGroupSelected;

        public ManageGroupPopup(List<ConversationModel> groupsList)
        {
            groups = new ObservableCollection<ConversationModel>(groupsList);

            Title = "Quản lý nhóm";
            BackgroundColor = Colors.White;

            var mainLayout = new VerticalStackLayout { Padding = 20, Spacing = 15 };

            var infoLabel = new Label
            {
                Text = $"Bạn đang tham gia {groups.Count} nhóm",
                FontSize = 14,
                TextColor = Colors.Gray
            };

            groupList = new CollectionView
            {
                SelectionMode = SelectionMode.None,
                ItemsSource = groups,
                ItemTemplate = new DataTemplate(() =>
                {
                    var grid = new Grid
                    {
                        Padding = 15,
                        ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = 50 },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                        BackgroundColor = Color.FromArgb("#F5F5F5"),
                        Margin = new Thickness(0, 5)
                    };

                    var avatar = new Frame
                    {
                        WidthRequest = 40,
                        HeightRequest = 40,
                        CornerRadius = 20,
                        BackgroundColor = Color.FromArgb("#2196F3"),
                        Padding = 0,
                        IsClippedToBounds = true,
                        Content = new Label
                        {
                            Text = "👥",
                            FontSize = 20,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };

                    var infoStack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };

                    var nameLabel = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold };
                    nameLabel.SetBinding(Label.TextProperty, "DisplayName");

                    var statusLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
                    statusLabel.SetBinding(Label.TextProperty, "StatusText");

                    infoStack.Children.Add(nameLabel);
                    infoStack.Children.Add(statusLabel);

                    var viewButton = new Button
                    {
                        Text = "Xem",
                        BackgroundColor = Color.FromArgb("#2196F3"),
                        TextColor = Colors.White,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = new Thickness(15, 5)
                    };
                    viewButton.Clicked += async (s, e) =>
                    {
                        var btn = s as Button;
                        var group = btn?.BindingContext as ConversationModel;
                        if (group != null)
                        {
                            OnGroupSelected?.Invoke(group);
                            await Navigation.PopModalAsync();
                        }
                    };

                    grid.Children.Add(avatar);
                    Grid.SetColumn(avatar, 0);
                    grid.Children.Add(infoStack);
                    Grid.SetColumn(infoStack, 1);
                    grid.Children.Add(viewButton);
                    Grid.SetColumn(viewButton, 2);

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

            mainLayout.Children.Add(new Label { Text = "Danh sách nhóm", FontSize = 18, FontAttributes = FontAttributes.Bold });
            mainLayout.Children.Add(infoLabel);
            mainLayout.Children.Add(new ScrollView { Content = groupList, VerticalOptions = LayoutOptions.FillAndExpand });
            mainLayout.Children.Add(closeButton);

            Content = mainLayout;
        }
    }
}
