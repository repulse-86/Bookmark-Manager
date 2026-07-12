using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace Bookmark_Manager.Data
{
    public class Bookmark
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Snippet { get; set; }
        public string CategoryId { get; set; }

        public string Thumbnail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ImageUrl))
                {
                    return ImageUrl;
                }
                return "";
            }
        }

        public JsonObject ToJsonObject()
        {
            var obj = new JsonObject();
            obj.SetNamedValue("Id", JsonValue.CreateStringValue(Id ?? ""));
            obj.SetNamedValue("Url", JsonValue.CreateStringValue(Url ?? ""));
            obj.SetNamedValue("Title", JsonValue.CreateStringValue(Title ?? ""));
            obj.SetNamedValue("ImageUrl", JsonValue.CreateStringValue(ImageUrl ?? ""));
            obj.SetNamedValue("Snippet", JsonValue.CreateStringValue(Snippet ?? ""));
            obj.SetNamedValue("CategoryId", JsonValue.CreateStringValue(CategoryId ?? ""));
            return obj;
        }

        public static Bookmark FromJsonObject(JsonObject obj)
        {
            return new Bookmark
            {
                Id = obj.ContainsKey("Id") ? obj.GetNamedString("Id") : "",
                Url = obj.ContainsKey("Url") ? obj.GetNamedString("Url") : "",
                Title = obj.ContainsKey("Title") ? obj.GetNamedString("Title") : "",
                ImageUrl = obj.ContainsKey("ImageUrl") ? obj.GetNamedString("ImageUrl") : "",
                Snippet = obj.ContainsKey("Snippet") ? obj.GetNamedString("Snippet") : "",
                CategoryId = obj.ContainsKey("CategoryId") ? obj.GetNamedString("CategoryId") : ""
            };
        }
    }

    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public JsonObject ToJsonObject()
        {
            var obj = new JsonObject();
            obj.SetNamedValue("Id", JsonValue.CreateStringValue(Id ?? ""));
            obj.SetNamedValue("Name", JsonValue.CreateStringValue(Name ?? ""));
            return obj;
        }

        public static Category FromJsonObject(JsonObject obj)
        {
            return new Category
            {
                Id = obj.ContainsKey("Id") ? obj.GetNamedString("Id") : "",
                Name = obj.ContainsKey("Name") ? obj.GetNamedString("Name") : ""
            };
        }
    }

    public sealed class BookmarkDataManager
    {
        private static readonly BookmarkDataManager _instance = new BookmarkDataManager();
        public static BookmarkDataManager Instance { get { return _instance; } }

        private const string DataFileName = "bookmark_data.json";

        public ObservableCollection<Bookmark> Bookmarks { get; private set; }
        public ObservableCollection<Category> Categories { get; private set; }

        private BookmarkDataManager()
        {
            Bookmarks = new ObservableCollection<Bookmark>();
            Categories = new ObservableCollection<Category>();
        }

        public async Task LoadAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = null;
                try
                {
                    file = await localFolder.GetFileAsync(DataFileName);
                }
                catch (FileNotFoundException)
                {
                    // File does not exist, fallback to seed defaults
                }

                if (file != null)
                {
                    string jsonText = await FileIO.ReadTextAsync(file);
                    if (!string.IsNullOrWhiteSpace(jsonText))
                    {
                        JsonObject jsonObject = JsonObject.Parse(jsonText);

                        Bookmarks.Clear();
                        if (jsonObject.ContainsKey("Bookmarks"))
                        {
                            JsonArray bookmarkArray = jsonObject["Bookmarks"].GetArray();
                            foreach (JsonValue val in bookmarkArray)
                            {
                                Bookmarks.Add(Bookmark.FromJsonObject(val.GetObject()));
                            }
                        }

                        Categories.Clear();
                        if (jsonObject.ContainsKey("Categories"))
                        {
                            JsonArray categoryArray = jsonObject["Categories"].GetArray();
                            foreach (JsonValue val in categoryArray)
                            {
                                Categories.Add(Category.FromJsonObject(val.GetObject()));
                            }
                        }
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to defaults on load failure
            }

            // Seed default values
            await SeedDefaultsAsync();
            await SaveAsync();
        }

        private async Task SeedDefaultsAsync()
        {
            Bookmarks.Clear();
            Categories.Clear();

            // Default Categories
            var catTech = new Category { Id = Guid.NewGuid().ToString(), Name = "Tech" };
            var catNews = new Category { Id = Guid.NewGuid().ToString(), Name = "News" };
            var catSocial = new Category { Id = Guid.NewGuid().ToString(), Name = "Social" };

            Categories.Add(catTech);
            Categories.Add(catNews);
            Categories.Add(catSocial);

            var seedEntries = new[]
            {
                new { Url = "https://discord.com", Category = catSocial },
                new { Url = "https://www.bbc.com", Category = catNews },
                new { Url = "https://www.youtube.com", Category = catTech },
                new { Url = "https://github.com", Category = catTech },
                new { Url = "https://www.wikipedia.org", Category = catNews }
            };

            foreach (var entry in seedEntries)
            {
                var preview = await ParseLinkPreviewAsync(entry.Url);
                Bookmarks.Add(new Bookmark
                {
                    Id = Guid.NewGuid().ToString(),
                    Url = entry.Url,
                    Title = !string.IsNullOrWhiteSpace(preview.Title) ? preview.Title : entry.Url,
                    ImageUrl = preview.ImageUrl,
                    Snippet = preview.Snippet,
                    CategoryId = entry.Category.Id
                });
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var jsonObject = new JsonObject();

                var bookmarkArray = new JsonArray();
                foreach (var b in Bookmarks)
                {
                    bookmarkArray.Add(b.ToJsonObject());
                }
                jsonObject.SetNamedValue("Bookmarks", bookmarkArray);

                var categoryArray = new JsonArray();
                foreach (var c in Categories)
                {
                    categoryArray.Add(c.ToJsonObject());
                }
                jsonObject.SetNamedValue("Categories", categoryArray);

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync(DataFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, jsonObject.Stringify());
            }
            catch (Exception)
            {
                // Ignore save failures
            }
        }

        public async Task AddCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var newCategory = new Category
            {
                Id = Guid.NewGuid().ToString(),
                Name = name.Trim()
            };

            Categories.Add(newCategory);
            await SaveAsync();
        }

        public async Task UpdateCategoryAsync(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var category = Categories.FirstOrDefault(c => c.Id == id);
            if (category == null) return;

            category.Name = name.Trim();

            // Force collection update
            int idx = Categories.IndexOf(category);
            if (idx >= 0)
            {
                Categories[idx] = category;
            }

            await SaveAsync();
        }

        public async Task DeleteCategoryAsync(string categoryId)
        {
            var category = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category != null)
            {
                Categories.Remove(category);

                // Update bookmarks belonging to this category to be uncategorized (CategoryId = "")
                foreach (var bookmark in Bookmarks)
                {
                    if (bookmark.CategoryId == categoryId)
                    {
                        bookmark.CategoryId = string.Empty;
                    }
                }

                await SaveAsync();
            }
        }

        public async Task AddBookmarkAsync(string url, string categoryId)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            string normalizedUrl = url.Trim();
            if (!normalizedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !normalizedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = "http://" + normalizedUrl;
            }

            var bookmark = new Bookmark
            {
                Id = Guid.NewGuid().ToString(),
                Url = normalizedUrl,
                CategoryId = categoryId
            };

            // Parse link preview asynchronously
            var preview = await ParseLinkPreviewAsync(normalizedUrl);
            bookmark.Title = !string.IsNullOrWhiteSpace(preview.Title) ? preview.Title : GetHostName(normalizedUrl);
            bookmark.ImageUrl = preview.ImageUrl;
            bookmark.Snippet = preview.Snippet;

            Bookmarks.Add(bookmark);
            await SaveAsync();
        }

        public async Task UpdateBookmarkAsync(string id, string url, string categoryId)
        {
            var bookmark = Bookmarks.FirstOrDefault(b => b.Id == id);
            if (bookmark == null) return;

            if (string.IsNullOrWhiteSpace(url)) return;
            string normalizedUrl = url.Trim();
            if (!normalizedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !normalizedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = "http://" + normalizedUrl;
            }

            bookmark.Url = normalizedUrl;
            bookmark.CategoryId = categoryId;

            // Re-parse preview
            var preview = await ParseLinkPreviewAsync(normalizedUrl);
            bookmark.Title = !string.IsNullOrWhiteSpace(preview.Title) ? preview.Title : GetHostName(normalizedUrl);
            bookmark.ImageUrl = preview.ImageUrl;
            bookmark.Snippet = preview.Snippet;

            // Force collection changed event
            int idx = Bookmarks.IndexOf(bookmark);
            if (idx >= 0)
            {
                Bookmarks[idx] = bookmark;
            }

            await SaveAsync();
        }

        private string GetHostName(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }

        public async Task<ParsedPreview> ParseLinkPreviewAsync(string url)
        {
            var result = new ParsedPreview();
            try
            {
                using (var client = new HttpClient())
                {
                    // Set a timeout so parsing doesn't hang indefinitely
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BookmarkManager/1.0)");

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string html = await response.Content.ReadAsStringAsync();

                        // Parse title
                        var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        if (titleMatch.Success)
                        {
                            result.Title = WebDecode(titleMatch.Groups[1].Value.Trim());
                        }

                        // Parse OpenGraph Image: <meta property="og:image" content="..." />
                        var imageMatch = Regex.Match(html, @"<meta\s+[^>]*(?:property|name)=[""']og:image[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                        if (!imageMatch.Success)
                        {
                            imageMatch = Regex.Match(html, @"<meta\s+[^>]*content=[""']([^""']+)[""']\s+(?:property|name)=[""']og:image[""']", RegexOptions.IgnoreCase);
                        }
                        if (imageMatch.Success)
                        {
                            string imgUrl = imageMatch.Groups[1].Value.Trim();
                            // Handle relative image paths
                            if (imgUrl.StartsWith("/"))
                            {
                                try
                                {
                                    var uri = new Uri(url);
                                    imgUrl = uri.Scheme + "://" + uri.Host + imgUrl;
                                }
                                catch {}
                            }
                            result.ImageUrl = imgUrl;
                        }

                        // Parse Description/Snippet
                        var descMatch = Regex.Match(html, @"<meta\s+[^>]*(?:name|property)=[""'](?:description|og:description)[""']\s+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                        if (!descMatch.Success)
                        {
                            descMatch = Regex.Match(html, @"<meta\s+[^>]*content=[""']([^""']+)[""']\s+(?:name|property)=[""'](?:description|og:description)[""']", RegexOptions.IgnoreCase);
                        }
                        if (descMatch.Success)
                        {
                            string rawDesc = WebDecode(descMatch.Groups[1].Value.Trim());
                            result.Snippet = ClampWords(rawDesc, 12);
                        }
                        else
                        {
                            result.Snippet = ClampWords(result.Title, 12);
                        }
                    }
                }
            }
            catch
            {
                // Ignore and return defaults/fallbacks on failure
            }

            return result;
        }

        private string WebDecode(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Simple manual html entity decoding for common entities
            return input
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&#39;", "'");
        }

        private string ClampWords(string input, int maxWords)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var words = input.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= maxWords) return input;
            return string.Join(" ", words.Take(maxWords)) + "...";
        }
    }

    public class ParsedPreview
    {
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }
}
