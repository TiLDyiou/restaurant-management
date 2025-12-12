using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantManagementGUI.PopUps
{
        public class AddFriendPopup : ContentPage
        {
            private Entry searchEntry;
            private CollectionView employeeList;
            private ObservableCollection<EmployeeModel> employees;
            private ObservableCollection<EmployeeModel> filteredEmployees;
            public event Action<List<EmployeeModel>> OnFriendSelected;

            public AddFriendPopup(List<EmployeeModel> allEmployees)
            {
                employees = new ObservableCollection<EmployeeModel>(allEmployees);
                filteredEmployees = new ObservableCollection<EmployeeModel>(allEmployees);

                Title = "Thêm bạn";
                BackgroundColor = Colors.White;

                var mainLayout = new VerticalStackLayout { Padding = 20, Spacing = 15 };

                // Search box
                searchEntry = new Entry
                {
                    Placeholder = "Nhập mã nhân viên hoặc tên...",
                    BackgroundColor = Color.FromArgb("#F0F0F0"),
                    HeightRequest = 40
                };
                searchEntry.TextChanged += OnSearchTextChanged;

                // Employee list
                employeeList = new CollectionView
                {
                    SelectionMode = SelectionMode.None,
                    ItemsSource = filteredEmployees,
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

                        var infoStack = new VerticalStackLayout { Spacing = 5 };

                        var nameLabel = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
                        nameLabel.SetBinding(Label.TextProperty, "Name");

                        var positionLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
                        positionLabel.SetBinding(Label.TextProperty, "Position");

                        var codeLabel = new Label { FontSize = 12, TextColor = Colors.DarkGray };
                        codeLabel.SetBinding(Label.TextProperty, new Binding("EmployeeCode", stringFormat: "Mã: {0}"));

                        infoStack.Children.Add(nameLabel);
                        infoStack.Children.Add(positionLabel);
                        infoStack.Children.Add(codeLabel);

                        var checkbox = new CheckBox { VerticalOptions = LayoutOptions.Center };
                        checkbox.SetBinding(CheckBox.IsCheckedProperty, "IsSelected");

                        grid.Children.Add(infoStack);
                        Grid.SetColumn(infoStack, 0);
                        grid.Children.Add(checkbox);
                        Grid.SetColumn(checkbox, 1);

                        return grid;
                    })
                };

                // Buttons
                var buttonStack = new HorizontalStackLayout
                {
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var sendButton = new Button
                {
                    Text = "Gửi lời mời",
                    BackgroundColor = Color.FromArgb("#4CAF50"),
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };
                sendButton.Clicked += OnSendClicked;

                var cancelButton = new Button
                {
                    Text = "Hủy",
                    BackgroundColor = Color.FromArgb("#F44336"),
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };
                cancelButton.Clicked += async (s, e) => await Navigation.PopModalAsync();

                buttonStack.Children.Add(sendButton);
                buttonStack.Children.Add(cancelButton);

                mainLayout.Children.Add(new Label { Text = "Tìm kiếm nhân viên", FontSize = 18, FontAttributes = FontAttributes.Bold });
                mainLayout.Children.Add(searchEntry);
                mainLayout.Children.Add(new ScrollView { Content = employeeList, VerticalOptions = LayoutOptions.FillAndExpand });
                mainLayout.Children.Add(buttonStack);

                Content = mainLayout;
            }

            private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
            {
                var searchText = e.NewTextValue?.ToLower() ?? "";
                filteredEmployees.Clear();

                var filtered = employees.Where(emp =>
                    emp.Name.ToLower().Contains(searchText) ||
                    emp.EmployeeCode.ToLower().Contains(searchText) ||
                    emp.Position.ToLower().Contains(searchText)
                );

                foreach (var emp in filtered)
                {
                    filteredEmployees.Add(emp);
                }
            }

            private async void OnSendClicked(object sender, EventArgs e)
            {
                var selected = employees.Where(emp => emp.IsSelected).ToList();

                if (selected.Count == 0)
                {
                    await DisplayAlert("Thông báo", "Vui lòng chọn ít nhất 1 nhân viên", "OK");
                    return;
                }

                OnFriendSelected?.Invoke(selected);
                await Navigation.PopModalAsync();
            }
        }

    }
