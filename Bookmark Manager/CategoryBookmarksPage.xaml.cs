using Bookmark_Manager.Common;
using Bookmark_Manager.Data;
using System;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Bookmark_Manager
{
    public sealed partial class CategoryBookmarksPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private string categoryId;

        public CategoryBookmarksPage()
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

            categoryId = e.Parameter as string;
            if (!string.IsNullOrEmpty(categoryId))
            {
                var category = BookmarkDataManager.Instance.Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    CategoryPivotItem.Header = category.Name.ToLower();
                }

                RefreshBookmarks();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void RefreshBookmarks()
        {
            if (!string.IsNullOrEmpty(categoryId))
            {
                var categoryBookmarks = BookmarkDataManager.Instance.Bookmarks.Where(b => b.CategoryId == categoryId).ToList();
                BookmarksGridView.ItemsSource = categoryBookmarks;
            }
        }

        private async void BookmarksGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var bookmark = e.ClickedItem as Bookmark;
            if (bookmark != null && !string.IsNullOrWhiteSpace(bookmark.Url))
            {
                try
                {
                    var uri = new Uri(bookmark.Url);
                    await Launcher.LaunchUriAsync(uri);
                }
                catch (Exception)
                {
                    // TODO
                }
            }
        }

        #region Long-press Context Menu Support

        private void BookmarkGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                ShowContextMenu(sender as FrameworkElement);
            }
        }

        private void ShowContextMenu(FrameworkElement element)
        {
            if (element == null) return;
            var bookmark = element.DataContext as Bookmark;
            if (bookmark == null) return;

            var menu = new MenuFlyout();

            var editItem = new MenuFlyoutItem { Text = "edit" };
            editItem.Click += (s, args) =>
            {
                Frame.Navigate(typeof(AddBookmarkPage), bookmark.Id);
            };

            var deleteItem = new MenuFlyoutItem { Text = "delete" };
            deleteItem.Click += async (s, args) =>
            {
                BookmarkDataManager.Instance.Bookmarks.Remove(bookmark);
                await BookmarkDataManager.Instance.SaveAsync();
                RefreshBookmarks();
            };

            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);

            menu.ShowAt(element);
        }

        #endregion

        #region Screen sizing calculations

        private void BookmarksGridView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetGridItemSize();
        }

        private void SetGridItemSize()
        {
            var wrapGrid = BookmarksGridView.ItemsPanelRoot as ItemsWrapGrid;
            if (wrapGrid != null)
            {
                double availableWidth = BookmarksGridView.ActualWidth;
                if (availableWidth == 0)
                {
                    availableWidth = Window.Current.Bounds.Width - 38; // fallback
                }
                double itemSize = Math.Floor((availableWidth - 12) / 3.0);
                if (itemSize > 0)
                {
                    wrapGrid.ItemWidth = itemSize;
                    wrapGrid.ItemHeight = itemSize;
                }
            }
        }

        #endregion
    }
}
