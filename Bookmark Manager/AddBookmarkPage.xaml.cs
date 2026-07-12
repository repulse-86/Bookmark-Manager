using Bookmark_Manager.Common;
using Bookmark_Manager.Data;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Bookmark_Manager
{
    public sealed partial class AddBookmarkPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private string editBookmarkId = null;

        public AddBookmarkPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);

            CategoryComboBox.ItemsSource = BookmarkDataManager.Instance.Categories;

            string idParam = e.Parameter as string;
            if (!string.IsNullOrEmpty(idParam))
            {
                var bookmark = BookmarkDataManager.Instance.Bookmarks.FirstOrDefault(b => b.Id == idParam);
                if (bookmark != null)
                {
                    editBookmarkId = idParam;
                    PageHeader.Text = "edit bookmark";
                    UrlTextBox.Text = bookmark.Url;

                    var cat = BookmarkDataManager.Instance.Categories.FirstOrDefault(c => c.Id == bookmark.CategoryId);
                    if (cat != null)
                    {
                        CategoryComboBox.SelectedItem = cat;
                    }
                    else if (BookmarkDataManager.Instance.Categories.Count > 0)
                    {
                        CategoryComboBox.SelectedIndex = 0;
                    }
                }
            }
            else
            {
                editBookmarkId = null;
                PageHeader.Text = "add bookmark";
                UrlTextBox.Text = string.Empty;
                if (BookmarkDataManager.Instance.Categories.Count > 0)
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            var selectedCategory = CategoryComboBox.SelectedItem as Category;
            string categoryId = selectedCategory != null ? selectedCategory.Id : string.Empty;
            
            SaveButton.IsEnabled = false;
            UrlTextBox.IsEnabled = false;
            CategoryComboBox.IsEnabled = false;
            LoadingProgress.IsActive = true;

            try
            {
                if (!string.IsNullOrEmpty(editBookmarkId))
                {
                    await BookmarkDataManager.Instance.UpdateBookmarkAsync(editBookmarkId, url, categoryId);
                }
                else
                {
                    await BookmarkDataManager.Instance.AddBookmarkAsync(url, categoryId);
                }
            }
            finally
            {
                LoadingProgress.IsActive = false;
                SaveButton.IsEnabled = true;
                UrlTextBox.IsEnabled = true;
                CategoryComboBox.IsEnabled = true;
            }

            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
    }
}
