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
    public sealed partial class PivotPage : Page
    {
        private readonly NavigationHelper navigationHelper;

        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            await BookmarkDataManager.Instance.LoadAsync();

            BookmarksGridView.ItemsSource = BookmarkDataManager.Instance.Bookmarks;
            CategoriesGridView.ItemsSource = BookmarkDataManager.Instance.Categories;
            FullScreenCategoriesListView.ItemsSource = BookmarkDataManager.Instance.Categories;

            SetSelectionMode(false);

            CategorySelectionOverlay.Visibility = Visibility.Collapsed;

            SetGridItemSizes();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {

        }

        private void SetSelectionMode(bool isMultiple)
        {
            if (isMultiple)
            {
                BookmarksGridView.SelectionMode = ListViewSelectionMode.Multiple;
                BookmarksGridView.IsItemClickEnabled = false;
            }
            else
            {
                if (BookmarksGridView.SelectionMode == ListViewSelectionMode.Multiple)
                {
                    BookmarksGridView.SelectedItems.Clear();
                }
                BookmarksGridView.SelectionMode = ListViewSelectionMode.None;
                BookmarksGridView.IsItemClickEnabled = true;
            }

            UpdateAppBarButtons();
        }

        private void UpdateAppBarButtons()
        {
            bool isMultiple = BookmarksGridView.SelectionMode == ListViewSelectionMode.Multiple;

            if (isMultiple)
            {
                AddBookmarkButton.Visibility = Visibility.Collapsed;
                AddCategoryButton.Visibility = Visibility.Collapsed;
                SearchButton.Visibility = Visibility.Collapsed;
                AboutButton.Visibility = Visibility.Collapsed;

                AddToCategoryButton.Visibility = Visibility.Visible;
                AddToCategoryButton.IsEnabled = BookmarksGridView.SelectedItems.Count > 0;
                CancelSelectionButton.Visibility = Visibility.Visible;
            }
            else
            {
                AddBookmarkButton.Visibility = Visibility.Visible;
                AddCategoryButton.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Visible;
                AboutButton.Visibility = Visibility.Visible;

                AddToCategoryButton.Visibility = Visibility.Collapsed;
                AddToCategoryButton.IsEnabled = false;
                CancelSelectionButton.Visibility = Visibility.Collapsed;
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

                }
            }
        }

        private void BookmarksGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAppBarButtons();
        }

        private void CategoriesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var category = e.ClickedItem as Category;
            if (category != null)
            {
                Frame.Navigate(typeof(CategoryBookmarksPage), category.Id);
            }
        }

        private void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddBookmarkPage));
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddCategoryPage));
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SearchBookmarksPage));
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private void AddToCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            CategorySelectionOverlay.Visibility = Visibility.Visible;
        }

        private void CancelSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SetSelectionMode(false);
        }

        private async void FullScreenCategoriesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var targetCategory = e.ClickedItem as Category;
            if (targetCategory != null)
            {
                var selectedBookmarks = BookmarksGridView.SelectedItems.Cast<Bookmark>().ToList();
                foreach (var bookmark in selectedBookmarks)
                {
                    bookmark.CategoryId = targetCategory.Id;
                }

                await BookmarkDataManager.Instance.SaveAsync();

                CategorySelectionOverlay.Visibility = Visibility.Collapsed;

                SetSelectionMode(false);
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
            };

            var selectItem = new MenuFlyoutItem { Text = "select" };
            selectItem.Click += (s, args) =>
            {
                SetSelectionMode(true);
                BookmarksGridView.SelectedItems.Add(bookmark);
            };

            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);
            menu.Items.Add(selectItem);

            menu.ShowAt(element);
        }

        private void CategoryGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                ShowCategoryContextMenu(sender as FrameworkElement);
            }
        }

        private void ShowCategoryContextMenu(FrameworkElement element)
        {
            if (element == null) return;
            var category = element.DataContext as Category;
            if (category == null) return;

            var menu = new MenuFlyout();

            var editItem = new MenuFlyoutItem { Text = "edit" };
            editItem.Click += (s, args) =>
            {
                Frame.Navigate(typeof(AddCategoryPage), category.Id);
            };

            var deleteItem = new MenuFlyoutItem { Text = "delete" };
            deleteItem.Click += async (s, args) =>
            {
                await BookmarkDataManager.Instance.DeleteCategoryAsync(category.Id);
            };

            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);

            menu.ShowAt(element);
        }

        #endregion

        #region Screen sizing calculations

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetGridItemSizes();
        }

        private void SetGridItemSizes()
        {
            SetGridViewItemSize(BookmarksGridView);
            SetGridViewItemSize(CategoriesGridView);
        }

        private void SetGridViewItemSize(GridView gridView)
        {
            if (gridView == null) return;
            var wrapGrid = gridView.ItemsPanelRoot as ItemsWrapGrid;
            if (wrapGrid != null)
            {
                double availableWidth = gridView.ActualWidth;
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

        #region NavigationHelper registration and Hardware BackPressed Handling

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }

        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (CategorySelectionOverlay.Visibility == Visibility.Visible)
            {
                CategorySelectionOverlay.Visibility = Visibility.Collapsed;
                e.Handled = true; // Prevent the app from exiting
            }
            else if (BookmarksGridView.SelectionMode == ListViewSelectionMode.Multiple)
            {
                // When multi-selecting, clicking back deselects everything and exits selection mode
                SetSelectionMode(false);
                e.Handled = true; // Handled, prevent exiting the app
            }
        }

        #endregion
    }
}
