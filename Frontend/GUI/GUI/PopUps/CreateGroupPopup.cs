using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.PopUps
{
    public class CreateGroupPopup : ContentPage
    {
        private Entry groupNameEntry;
        private CollectionView memberList;
        private ObservableCollection<ConversationModel> friends;
        public event Action<string, List<ConversationModel>> OnGroupCreated;

        public CreateGroupPopup(List<ConversationModel> friendsList)
        {
            friends = new ObservableCollection<ConversationModel>(friendsList);

            Title = "Tạo nhóm";
            BackgroundColor = Colors.White;

            var mainLayout = new VerticalStackLayout { Padding = 20, Spacing = 15 };

            groupNameEntry = new Entry
            {
                Placeholder = "Nhập tên nhóm...",
                BackgroundColor = Color.FromArgb("#F0F0F0"),
                HeightRequest = 40
            };

            memberList = new CollectionView
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
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                    };

                    var nameLabel = new Label
                    {
                        FontSize = 14,
                        VerticalOptions = LayoutOptions.Center
                    };
                    nameLabel.SetBinding(Label.TextProperty, "DisplayName");

                    var checkbox = new CheckBox { VerticalOptions = LayoutOptions.Center };
                    checkbox.CheckedChanged += (s, e) =>
                    {
                        var cb = s as CheckBox;
                        var friend = cb?.BindingContext as ConversationModel;
                        if (friend != null)
                        {
                            friend.IsSelected = e.Value;
                        }
                    };

                    grid.Children.Add(nameLabel);
                    Grid.SetColumn(nameLabel, 0);
                    grid.Children.Add(checkbox);
                    Grid.SetColumn(checkbox, 1);

                    return grid;
                })
            };

            var buttonStack = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };

            var createButton = new Button
            {
                Text = "Tạo nhóm",
                BackgroundColor = Color.FromArgb("#2196F3"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            createButton.Clicked += OnCreateClicked;

            var cancelButton = new Button
            {
                Text = "Hủy",
                BackgroundColor = Color.FromArgb("#9E9E9E"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            cancelButton.Clicked += async (s, e) => await Navigation.PopModalAsync();

            buttonStack.Children.Add(createButton);
            buttonStack.Children.Add(cancelButton);

            mainLayout.Children.Add(new Label { Text = "Tên nhóm", FontSize = 16, FontAttributes = FontAttributes.Bold });
            mainLayout.Children.Add(groupNameEntry);
            mainLayout.Children.Add(new Label { Text = "Chọn thành viên", FontSize = 16, FontAttributes = FontAttributes.Bold });
            mainLayout.Children.Add(new ScrollView { Content = memberList, VerticalOptions = LayoutOptions.FillAndExpand });
            mainLayout.Children.Add(buttonStack);

            Content = mainLayout;
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(groupNameEntry.Text))
            {
                await DisplayAlert("Thông báo", "Vui lòng nhập tên nhóm", "OK");
                return;
            }

            var selectedMembers = friends.Where(f => f.IsSelected).ToList();

            if (selectedMembers.Count == 0)
            {
                await DisplayAlert("Thông báo", "Vui lòng chọn ít nhất 1 thành viên", "OK");
                return;
            }

            OnGroupCreated?.Invoke(groupNameEntry.Text, selectedMembers);
            await Navigation.PopModalAsync();
        }
    }

}
