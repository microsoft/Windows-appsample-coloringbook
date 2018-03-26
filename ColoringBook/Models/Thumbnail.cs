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
using System.Threading.Tasks;
using ColoringBook.Common;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBook.Models
{
    public abstract class Thumbnail : Observable
    {
        protected Thumbnail(bool isHero) => IsHero = isHero;

        public BitmapImage ThumbnailImage { get; set; }

        public bool IsHero
        {
            get => _isHero;
            set => Set(ref _isHero, value);
        }

        private bool _isHero;

        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        private bool _isLoading = true;

        public bool HadError
        {
            get => _hadError;
            set => Set(ref _hadError, value);
        }

        private bool _hadError;

        public bool MarkForDelete
        {
            get => _markForDelete;
            set => Set(ref _markForDelete, value);
        }

        private bool _markForDelete;

        protected string ThumbnailBackgroundImageName { get; set; }

        public abstract Task LoadThumbnailAsync();

        public void SetErrorThumbnail()
        {
            IsLoading = false;
            HadError = true;
        }

        public void OnItemClick()
        {
            if (!IsLoading)
            {
                LoadColoringAsync().ContinueWithoutWaiting();
            }
        }

        protected abstract Task LoadColoringAsync();

        protected async Task LoadThumbnailBackgroundImageAsync(StorageFile imageFile)
        {
            ThumbnailImage = new BitmapImage();
            try
            {
                using (var fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(fileStream);

                    // Use encoder to set interpolation mode and scaling for visually better rendering.
                    var inMemoryRandomAccessStream = new InMemoryRandomAccessStream();
                    var encoder = await BitmapEncoder
                        .CreateForTranscodingAsync(inMemoryRandomAccessStream, decoder);
                    encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    encoder.BitmapTransform.ScaledWidth =
                        (uint)Constants.HeroThumbnailSize.Width * (uint)(IsHero ? 2 : 1);
                    encoder.BitmapTransform.ScaledHeight =
                        (uint)Constants.HeroThumbnailSize.Height * (uint)(IsHero ? 2 : 1);
                    await encoder.FlushAsync();

                    await ThumbnailImage.SetSourceAsync(inMemoryRandomAccessStream);
                }
            }
            catch
            {
                var defaultThumbnailFile = await Tools.GetFileAsync(await Tools.GetAssetsFolderAsync(),
                    Tools.GetResourceString("FileName/DefaultThumbnailFileName"));

                if (defaultThumbnailFile == null)
                {
                    throw new Exception("Could not load default Thumbnail File.");
                }

                using (var fileStream = await defaultThumbnailFile.OpenAsync(FileAccessMode.Read))
                {
                    await ThumbnailImage.SetSourceAsync(fileStream);
                }
            }
        }
    }
}
