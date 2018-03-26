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
using ColoringBook.FileIO;
using Windows.Storage;

namespace ColoringBook.Models
{
    public class ColoringBookColoringSettings
    {
        public ColoringBookColoringSettings()
        {
            LastOpened = DateTime.Now;
        }

        public ColoringBookColoringSettings(string coloringName, string templateImageName,
            DeviceResolutionType coloringResolutionType)
            : base()
        {
            ColoringImageName = templateImageName;
            ColoringName = coloringName;
            ColoringResolutionType = coloringResolutionType;
        }

        private static ColoringBookColoringSettingsReader Reader { get; } = new ColoringBookColoringSettingsReader();

        private ColoringBookColoringSettingsWriter Writer { get; } = new ColoringBookColoringSettingsWriter();

        public StorageFolder ColoringDirectory { get; set; }

        public string ColoringImageName { get; set; }

        public string ColoringName { get; set; }

        public DeviceResolutionType ColoringResolutionType { get; set; }

        public DateTime LastOpened { get; set; }

        public static async Task<ColoringBookColoringSettings> LoadSettingsAsync(string coloringName)
        {
            var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
            var coloringDirectory = await coloringsDirectory.CreateFolderAsync(coloringName, CreationCollisionOption.OpenIfExists);

            var settings = await Reader.ReadAsync(coloringDirectory, coloringName + Tools.GetResourceString("FileType/settings"));
            settings.ColoringDirectory = coloringDirectory;
            return settings;
        }

        public async Task<StorageFolder> GetLibraryImageLocationAsync()
        {
            var assetsDir = await Tools.GetAssetsFolderAsync();
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(assetsDir, "LibraryImages");
            var libraryImageDir = await Tools.GetSubDirectoryAsync(libraryImagesDir, ColoringImageName);

            return libraryImageDir;
        }

        public async Task<StorageFolder> GetLibraryImagePreprocessingLocationAsync()
        {
            var assetsDir = await Tools.GetAssetsFolderAsync();
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(assetsDir, "LibraryImages");
            var libraryImageDir = await Tools.GetSubDirectoryAsync(libraryImagesDir, ColoringImageName);

            return libraryImageDir;
        }

        public async Task SaveSettingsToFileAsync()
        {
            if (ColoringDirectory == null)
            {
                var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
                ColoringDirectory = await Tools.CreateSubDirectoryAsync(coloringsDirectory, ColoringName);
            }

            var fname = ColoringName + Tools.GetResourceString("FileType/settings");
            await Writer.WriteAsync(ColoringDirectory, fname, this);
        }
    }
}