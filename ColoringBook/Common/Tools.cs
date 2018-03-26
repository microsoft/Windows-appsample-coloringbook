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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace ColoringBook.Common
{
    public static class Tools
    {
        public static bool Not(bool value) => !value;

        /// <summary>
        /// Awaits the running task but discards the result so that execution can continue
        /// without blocking the caller (enabling fire-and-forget semantics), but also without
        /// ignoring any exceptions that might occur, which will instead be thrown here.
        /// </summary>
        /// <param name="task">The task to await.</param>
        public static async void ContinueWithoutWaiting(this Task task)
        {
            if (task.IsFaulted)
            {
                await Task.CompletedTask;
            }
            else
            {
                await task.ConfigureAwait(false);
            }
        }

        public static Visibility VisibleIfFalse(bool isNotVisible) =>
            isNotVisible ? Visibility.Collapsed : Visibility.Visible;

        public static Brush BrushFromColor(Color color) => new SolidColorBrush(color);

        public static DeviceResolutionType ResolutionType
        {
            get
            {
                if (_resolutionType == DeviceResolutionType.Uncalculated)
                {
                    _resolutionType = CalculateDeviceResolutionType();
                }

                return _resolutionType;
            }
        }

        private static DeviceResolutionType _resolutionType = DeviceResolutionType.Uncalculated;

        public static string GetResolutionTypeAsString(DeviceResolutionType resolutionType) =>
            resolutionType == DeviceResolutionType.High ? "HighResolution" : string.Empty;

        public static DeviceResolutionType CalculateDeviceResolutionType()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            var deviceWidth = displayInformation.ScreenWidthInRawPixels;
            var deviceHeight = displayInformation.ScreenHeightInRawPixels;

            var noOfPixels = deviceWidth * deviceHeight;

            // 4096 * 2160 = 8847360
            return noOfPixels > 8847360 ? DeviceResolutionType.High : DeviceResolutionType.Regular;
        }

        public static string GetResourceString(string resourceName)
        {
            var loader = new ResourceLoader();
            var resourceValue = loader.GetString(resourceName);

            return resourceValue;
        }

        public static async Task<StorageFolder> GetSubDirectoryAsync(StorageFolder dir, string sub)
        {
            try
            {
                return await dir.TryGetItemAsync(sub) as StorageFolder;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<StorageFolder> CreateSubDirectoryAsync(StorageFolder dir, string sub) =>
            await dir.CreateFolderAsync(sub, CreationCollisionOption.OpenIfExists);

        public static async Task<StorageFile> GetFileAsync(StorageFolder dir, string file)
        {
            try
            {
                return await dir.TryGetItemAsync(file) as StorageFile;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<StorageFile> CreateFileAsync(StorageFolder dir, string file) =>
            await dir.CreateFileAsync(file, CreationCollisionOption.OpenIfExists);

        public static async Task<StorageFolder> GetAssetsFolderAsync() =>
            await Package.Current.InstalledLocation.GetFolderAsync("Assets");

        public static async Task<StorageFolder> GetColoringsDirectoryAsync() =>
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                "Colorings", CreationCollisionOption.OpenIfExists);

        public static InkStroke CloneInkStroke(InkStroke stroke)
        {
            var inkBuilder = new InkStrokeBuilder();
            var newStroke = inkBuilder.CreateStrokeFromInkPoints(stroke.GetInkPoints(), stroke.PointTransform);
            newStroke.DrawingAttributes = stroke.DrawingAttributes;

            return newStroke;
        }

        public static Color ColorFromHexInteger(int val) => new Color
        {
            A = (byte)(((val >> 24) & 0xff) == 0 ? 0xff : (val >> 24) & 0xff),
            R = (byte)((val >> 16) & 0xff),
            G = (byte)((val >> 8) & 0xff),
            B = (byte)(val & 0xff)
        };
    }
}
