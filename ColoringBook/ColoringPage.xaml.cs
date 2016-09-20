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
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Input.Inking;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Core;
using PrintSample;

namespace ColoringBook
{
    /// <summary>
    /// ColoringPage is a page for coloring (inking on images).
    /// </summary>
    public sealed partial class ColoringPage : Page
    {
        // The name of the coloring image in the app's Images folder
        private string _Imgsrc;

        // The name of the folder where the coloring page is saved in the app's Local folder
        private string _Foldername; 

        private PhotosPrintHelper _PrintHelper;

        public ColoringPage()
        {
            this.InitializeComponent();
        }
        

        /// Open coloring page with coloring image and, if applicable, ink.
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // NavigationEvent parameter is the source of the coloring page, either a blank coloring image or a previously inked coloring page.
            String parameter = e.Parameter.ToString(); 
            
            // If navigating from ContinueColoring, the image is a previously inked coloring page.
            if (parameter.EndsWith(Constants.inkedImageFile))
            {
                // Get folder in local storage containing ink file and coloring image source.
                string folderpath = parameter.Substring(0, parameter.IndexOf(Constants.inkedImageFile));
                _Foldername = folderpath.Substring(parameter.IndexOf(Constants.inkImageFolder));
                
                try
                {
                    StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(_Foldername);
                    // Get coloring image source.
                    StorageFile imgsrcFile = await folder.GetFileAsync(Constants.imgsrcFile);
                    _Imgsrc = await FileIO.ReadTextAsync(imgsrcFile);
                    // Load ink.
                    StorageFile inkFile = await folder.GetFileAsync(Constants.inkFile);
                    if (inkFile != null)
                    {
                        IRandomAccessStream stream = await inkFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                        using (var inputStream = stream.GetInputStreamAt(0))
                        {
                            await myInkCanvas.InkPresenter.StrokeContainer.LoadAsync(stream);
                        }
                        stream.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

            }

            // If navigating from PickColoringPage, the image is a blank coloring image.
            else
            {
                Uri abspath = new Uri($"{Windows.ApplicationModel.Package.Current.InstalledLocation.Path}\\Images");
                Uri relpath = abspath.MakeRelativeUri(new Uri(parameter));
                _Imgsrc = relpath.ToString();
            }

            // Set up printing.
            _PrintHelper = new PhotosPrintHelper(this);
            _PrintHelper.RegisterForPrinting();

            // Adjust ScrollViewer size to Window.
            var bounds = Window.Current.Bounds;
            myScrollViewer.Height = bounds.Height - myCommandBar.Height;
            myScrollViewer.Width = bounds.Width;

            myImage.Source = new BitmapImage(new Uri("ms-appx:///" + _Imgsrc));
        }

        // Must unregister for printing upon leaving the page.
        protected override void OnNavigatedFrom(NavigationEventArgs e)=> _PrintHelper.UnregisterForPrinting();
        
        /// Match InkCanvas size to coloring image size.
        private void MyImage_Opened(object sender, RoutedEventArgs e)
        {
            myInkCanvas.Height = myImage.ActualHeight;
            myInkCanvas.Width = myImage.ActualWidth;
        }

        /// Customize inktool bar.
        private void InkToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            myInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

            markerButton.Palette = penButton.Palette;
            myInkToolbar.ActiveTool = markerButton;

            myInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
        }


        /// Save coloring image source, ink, and inked image to local folder.
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Find exisiting folder, or create new folder.
            StorageFolder folder = null;
            if (_Foldername == null)
            {
                try
                {
                    folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(Constants.inkImageFolder, CreationCollisionOption.GenerateUniqueName);
                    _Foldername = folder.Name;
                    // Save coloring image source.
                    StorageFile imgsrcfile = await folder.CreateFileAsync(Constants.imgsrcFile);
                    await FileIO.WriteTextAsync(imgsrcfile, _Imgsrc);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            else
            {
                folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(_Foldername);
            }

            // Save ink.
            StorageFile myInkFile = await folder.CreateFileAsync(Constants.inkFile, CreationCollisionOption.ReplaceExisting);
            IReadOnlyList<InkStroke> currentStrokes = myInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (myInkFile != null && currentStrokes.Count > 0)
            {
                Windows.Storage.CachedFileManager.DeferUpdates(myInkFile);
                IRandomAccessStream stream = await myInkFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    await myInkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                    
                    await outputStream.FlushAsync();
                }
                stream.Dispose();

                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(myInkFile);
            }

            // Save inked image.
            StorageFile myInkedImageFile = await folder.CreateFileAsync(Constants.inkedImageFile, CreationCollisionOption.ReplaceExisting);
            await Save_InkedImagetoFile(myInkedImageFile);

        }
        
        /// Share coloring page.
        private void Share_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            DataTransferManager.ShowShareUI();
        }

        /// Prepare data package with coloring page for sharing.
        private async void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            DataRequestDeferral deferral = request.GetDeferral();
            request.Data.Properties.Title = "A Coloring Page";
            request.Data.Properties.ApplicationName = "Coloring Book";
            request.Data.Properties.Description = "A coloring page sent from my Coloring Book app!";
            using (InMemoryRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream())
            {
                await Save_InkedImagetoStream(inMemoryStream);
                request.Data.SetBitmap(RandomAccessStreamReference.CreateFromStream(inMemoryStream));
            }
            deferral.Complete();
        }

        /// Export inked image.
        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            savePicker.SuggestedFileName = "Coloring Page";
            StorageFile saveFile = await savePicker.PickSaveFileAsync();
            await Save_InkedImagetoFile(saveFile);
        }

        /// Save coloring page as PNG to specified stream.
        private async Task Save_InkedImagetoStream(IRandomAccessStream stream)
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(((BitmapImage)myImage.Source).UriSource);

            CanvasDevice device = CanvasDevice.GetSharedDevice();

            var image = await CanvasBitmap.LoadAsync(device, file.Path);

            using (var renderTarget = new CanvasRenderTarget(device, (int)myInkCanvas.ActualWidth, (int)myInkCanvas.ActualHeight, image.Dpi))
            {
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    
                    ds.DrawImage(image, new Rect(0, 0, (int)myInkCanvas.ActualWidth, (int)myInkCanvas.ActualHeight));
                    ds.DrawInk(myInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }

                await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
            }
        }

        /// Saves coloring page to specified file.
        private async Task Save_InkedImagetoFile(StorageFile saveFile)
        {
            if (saveFile != null)
            {
                Windows.Storage.CachedFileManager.DeferUpdates(saveFile);
                
                using (var outStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await Save_InkedImagetoStream(outStream);
                }

                Windows.Storage.Provider.FileUpdateStatus status =
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(saveFile);
            }
        }

        // Print coloring page.
        public async void Print_Click(object sender, RoutedEventArgs e)
        {
            using (InMemoryRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream())
            {
                await Save_InkedImagetoStream(inMemoryStream);
                _PrintHelper.setStream(inMemoryStream);
                await _PrintHelper.ShowPrintUIAsync();
            }

        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => myScrollViewer.ChangeView(myScrollViewer.HorizontalOffset, myScrollViewer.VerticalOffset, myScrollViewer.ZoomFactor + 0.2f);

        private void ZoomOut_Click(object sender, RoutedEventArgs e) => myScrollViewer.ChangeView(myScrollViewer.HorizontalOffset, myScrollViewer.VerticalOffset, myScrollViewer.ZoomFactor - 0.2f);

        /// Delete coloring page.
        private  async void Delete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Delete file permanently?",
                Content = "If you delete this file, you won't be able to recover it. Do you want to delete it?",
                PrimaryButtonText = "Cancel",
                SecondaryButtonText = "Delete file permanently"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            // Delete the file if the user clicked the second button. 
            // Otherwise, do nothing. 
            if (result == ContentDialogResult.Secondary)
            {
                // Delete the file. 
                if (_Foldername != null)
                {
                    StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(_Foldername);
                    await folder.DeleteAsync();
                }


                if (await ApplicationData.Current.LocalFolder.GetFoldersAsync() == null)
                {
                    this.Frame.Navigate(typeof(Welcome));
                }
                else
                {
                    this.Frame.GoBack();
                }
                
            }

        }

        // Remove last Ink stroke drawn on InkCanvas.
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<InkStroke> strokes = myInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count > 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                myInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
        }
    }



}
