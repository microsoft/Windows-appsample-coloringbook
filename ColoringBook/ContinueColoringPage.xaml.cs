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

using ColoringBook.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ColoringBook
{
    /// <summary>
    /// ContinueColoringPage allows the user to select a saved coloring page.
    /// </summary>
    public sealed partial class ContinueColoringPage : Page
    {
        public ObservableCollection<ColoringImage> ImageSources { get; private set; }
        public ContinueColoringPage()
        {
            this.InitializeComponent();
            ImageSources = new ObservableCollection<ColoringImage>();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e) => Task.Run(GetInkedImages);

        // Get previously saved coloring pages from the app's local data folder.
        public async Task GetInkedImages()
        {
            try
            {
                IReadOnlyList<StorageFolder> folders = await ApplicationData.Current.LocalFolder.GetFoldersAsync();
                foreach (StorageFolder folder in folders)
                {
                    StorageFile  image = await folder.GetFileAsync(Constants.inkedImageFile); 
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    ImageSources.Add(new ColoringImage { ColoringImageSource = new Uri(image.Path) }));
                }
                
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }

        // Navigate to ColoringPage when user selects image.
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ColoringImage img = (ColoringImage)e.ClickedItem;
            this.Frame.Navigate(typeof(ColoringPage), img.ColoringImageSource);
        }
    }
}
