//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

# undef STOREASSOCIATION
using ColoringBook.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Services.Store;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ColoringBook
{
    /// <summary>
    /// PickColoringPage allows the user to select a coloring page. 
    /// If the user has not purchased the collection, then the user must first purchase the collection.
    /// </summary>
    public sealed partial class PickColoringPage : Page
    {
        public ObservableCollection<ColoringImage> ImageSources { get; private set; }
        
        // The images, text, and storeid associated with an illustrator contain his/her initials

        private string _Initials; 
        private string _StoreId;

        private StoreContext _Context;

        public PickColoringPage()
        {
            this.InitializeComponent();
            ImageSources = new ObservableCollection<ColoringImage>();
            _Context = null;
        }

        // First, check if collection is purchased.
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _Initials = e.Parameter.ToString();
#if STOREASSOCIATION
            _StoreId = Constants.storeids[_Initials];
            bool purchased = await IsPurchased();
            if (purchased)
            {
                ShowAddOn();
            }
            else
            {
                TitleText.Text = "Purchase Collection";
                purchaseButton.Visibility = Visibility.Visible;
                ImageGrid.Opacity = .5;
                ImageGrid.IsItemClickEnabled = false;
            }
#else
            TitleText.Text = "Pick a Coloring Page";
#endif

            await GetImages();
            bio.Text = await GetText();

        }

        // Display settings if user has purchased the collection.
        private void ShowAddOn()
        {
            TitleText.Text = "Pick a Coloring Page";
            purchaseButton.Visibility = Visibility.Collapsed;
            ImageGrid.Opacity = 1;
            ImageGrid.IsItemClickEnabled = true;
        }

        // Has the user purchased this collection?
        private async Task<bool> IsPurchased()
        {
            if (_Context == null)
            {
                _Context = StoreContext.GetDefault();
            }

            // Specify the kinds of add-ons to retrieve.
            var filterList = new List<string>(new[] { "Durable" });
            var idList = new List<string>(new[] { _StoreId });

            StoreProductQueryResult queryResult = await _Context.GetStoreProductsAsync(filterList, idList);

            if (queryResult.ExtendedError != null)
            {
                // The user may be offline or there might be some other server failure.
                Debug.WriteLine($"ExtendedError: {queryResult.ExtendedError.Message}");
                return false;
            }


            foreach (var item in queryResult.Products)
            {
                StoreProduct product = item.Value;
                return product.IsInUserCollection;
            }

            return false;
        }


        // Get the collection's coloring images in the app's Images folder.
        public async Task GetImages()
        {
            string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string path = $"{root}\\Images";

            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                IReadOnlyList<StorageFile> filesInFolder = await folder.GetFilesAsync();
                foreach (StorageFile file in filesInFolder)
                {
                    if (file.Name.Contains(_Initials))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        ImageSources.Add(new ColoringImage { ColoringImageSource = new Uri(file.Path) }));
                    }

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }


        }

        // Retrieve artist's bio from txt file.
        public async Task<String> GetText()
        {
            string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            String path = $"{root}\\Bios";
            String filename = $"{_Initials}.txt";
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                StorageFile file = await folder.GetFileAsync(filename);
                String text = await FileIO.ReadTextAsync(file);
                return text;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return "";
        }

        // Navigate to ColoringPage.
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ColoringImage img = e.ClickedItem as ColoringImage;
            this.Frame.Navigate(typeof(ColoringPage), img.ColoringImageSource);
        }

        // Purchase the collection from the store.
        private async void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_Context == null)
            {
                _Context = StoreContext.GetDefault();
            }

            workingProgressRing.IsActive = true;
            StorePurchaseResult result = await _Context.RequestPurchaseAsync(_StoreId);
            workingProgressRing.IsActive = false;

            if (result.ExtendedError != null)
            {
                // The user may be offline or there might be some other server failure.
                Debug.WriteLine($"ExtendedError: {result.ExtendedError.Message}");
                errorText.Text = result.ExtendedError.Message;
                return;
            }

            switch (result.Status)
            {
                case StorePurchaseStatus.AlreadyPurchased:
                    Debug.WriteLine("The user has already purchased the product.");
                    break;

                case StorePurchaseStatus.Succeeded:
                    Debug.WriteLine("The purchase was successful.");
                    ShowAddOn();
                    break;

                case StorePurchaseStatus.NotPurchased:
                    Debug.WriteLine("The user cancelled the purchase.");
                    break;

                case StorePurchaseStatus.NetworkError:
                    Debug.WriteLine("The purchase was unsuccessful due to a network error.");
                    errorText.Text = "The purchase was unsuccessful due to a network error.";
                    break;

                case StorePurchaseStatus.ServerError:
                    Debug.WriteLine("The purchase was unsuccessful due to a server error.");
                    errorText.Text = "The purchase was unsuccessful due to a server error.";
                    break;

                default:
                    Debug.WriteLine("The purchase was unsuccessful due to an unknown error.");
                    errorText.Text = "The purchase was unsuccessful due to an unknown error.";
                    break;
            }

        }
    }
}
