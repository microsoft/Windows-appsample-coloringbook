// ---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBookPreprocessingGenerator
{
    public class MainPageViewModel : Observable
    {
        private string ImageName { get; set; }

        private uint ImageWidth { get; set; }

        private uint ImageHeight { get; set; }

        private byte[] ImageBytes { get; set; }

        private StorageFolder OutputDirectory { get; set; }

        public BitmapImage ImageToProcess
        {
            get => _imageToProcess;
            set => Set(ref _imageToProcess, value);
        }

        private BitmapImage _imageToProcess;

        public string ImagePath
        {
            get => _imagePath;
            set => Set(ref _imagePath, value);
        }

        private string _imagePath;

        public bool ShouldConvertImage
        {
            get => _shouldConvertImage;
            set => Set(ref _shouldConvertImage, value);
        }

        private bool _shouldConvertImage = true;

        public bool ShouldGeneratePreprocessing
        {
            get => _shouldGeneratePreprocessing;
            set => Set(ref _shouldGeneratePreprocessing, value);
        }

        private bool _shouldGeneratePreprocessing = true;

        public bool ShouldOverwrite
        {
            get => _shouldOverwrite;
            set => Set(ref _shouldOverwrite, value);
        }

        private bool _shouldOverwrite;

        public string OutputPath
        {
            get => _outputPath;
            set => Set(ref _outputPath, value);
        }

        private string _outputPath;

        public async void LoadImage()
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".png");

            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            await LoadImageAsync(file);
        }

        public async void ChooseOutputDirectory()
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            folderPicker.FileTypeFilter.Add("*");

            OutputDirectory = await folderPicker.PickSingleFolderAsync();
            if (OutputDirectory != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", OutputDirectory);
                OutputPath = OutputDirectory.Path;
            }
            else
            {
                OutputPath = "";
            }
        }

        public async void BeginProcessing()
        {
            if (ImageToProcess == null)
            {
                await NotifyUserAsync("Please choose an image to process.");
                return;
            }

            if (OutputDirectory == null)
            {
                await NotifyUserAsync("Please choose an output directory.");
                return;
            }

            if (!ShouldConvertImage && !ShouldGeneratePreprocessing)
            {
                await NotifyUserAsync("Please choose a processing action to take.");
                return;
            }

            byte[] imageBytes = ShouldOverwrite ? ImageBytes : (byte[])ImageBytes.Clone();
            Processor processor = new Processor(imageBytes, ImageHeight, ImageWidth);

            var collisionSettings = ShouldOverwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.FailIfExists;

            if (ShouldConvertImage)
            {
                StorageFile convertedImageFile = null;
                try
                {
                    convertedImageFile = await OutputDirectory.CreateFileAsync(ImageName, collisionSettings);
                }
                catch { }

                if (convertedImageFile == null)
                {
                    await NotifyUserAsync("Converting file error: File creation error.");
                    return;
                }

                await processor.ConvertWhitesToTransparentAsync(convertedImageFile);
            }

            if (ShouldGeneratePreprocessing)
            {
                var fileName = Path.GetFileNameWithoutExtension(ImageName) + ".preprocessing";

                StorageFile preprocessingFile = null;
                try
                {
                    preprocessingFile = await OutputDirectory.CreateFileAsync(fileName, collisionSettings);
                }
                catch { }

                if (preprocessingFile == null)
                {
                    await NotifyUserAsync("Generating preprocessing error: File creation error.");
                    return;
                }

                var loadingDialog = new LoadingDialog();
                var ignoreResult = loadingDialog.ShowAsync();

                await processor.GeneratePreprocessingAsync(preprocessingFile);

                loadingDialog.Hide();
            }

            await NotifyUserAsync("Processing Complete");
        }


        public static async Task NotifyUserAsync(string message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }

        private async Task LoadImageAsync(StorageFile file)
        {
            ImagePath = file.Path;
            ImageName = Path.GetFileName(file.Path);

            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                ImageToProcess = new BitmapImage();

                var dec = await BitmapDecoder.CreateAsync(stream);

                var data = await dec.GetPixelDataAsync();
                ImageBytes = data.DetachPixelData();
                ImageWidth = dec.PixelWidth;
                ImageHeight = dec.PixelHeight;

                await ImageToProcess.SetSourceAsync(stream);
            }
        }
    }
}
