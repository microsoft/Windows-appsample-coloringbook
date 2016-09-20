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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ColoringBook
{
    /// <summary>
    /// The WelcomePage allows the user to pick a coloring page, or if there are saved coloring pages, to continue coloring.
    /// </summary>
    public sealed partial class Welcome : Page, INotifyPropertyChanged 
    {
        public bool ContinueButtonVisibility { get; private set; } 

        public Welcome()
        {
            this.InitializeComponent();
            CheckForSavedColoringPages();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Checks if there are saved coloring pages in the app's local folder.
        private async Task CheckForSavedColoringPages()
        {
            try
            {
                var folders = await ApplicationData.Current.LocalFolder.GetFoldersAsync();
                ContinueButtonVisibility = (folders.Count > 0);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContinueButtonVisibility)));
            } catch(Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        
        // User selects Pick Coloring Page.
        private void Color_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PickCollectionPage));
        }

        // User selects Continue Coloring.
        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ContinueColoringPage));
        }

    }
}
 