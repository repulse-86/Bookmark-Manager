using Bookmark_Manager.Common;
using Bookmark_Manager.Data;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Bookmark_Manager
{
    public sealed partial class AddCategoryPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private string editCategoryId = null;

        public AddCategoryPage()
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

            string idParam = e.Parameter as string;
            if (!string.IsNullOrEmpty(idParam))
            {
                var category = BookmarkDataManager.Instance.Categories.FirstOrDefault(c => c.Id == idParam);
                if (category != null)
                {
                    editCategoryId = idParam;
                    PageHeader.Text = "edit category";
                    CategoryNameTextBox.Text = category.Name;
                }
            }
            else
            {
                editCategoryId = null;
                PageHeader.Text = "add category";
                CategoryNameTextBox.Text = string.Empty;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = CategoryNameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                if (!string.IsNullOrEmpty(editCategoryId))
                {
                    await BookmarkDataManager.Instance.UpdateCategoryAsync(editCategoryId, categoryName);
                }
                else
                {
                    await BookmarkDataManager.Instance.AddCategoryAsync(categoryName);
                }

                if (this.Frame.CanGoBack)
                {
                    this.Frame.GoBack();
                }
            }
        }
    }
}
