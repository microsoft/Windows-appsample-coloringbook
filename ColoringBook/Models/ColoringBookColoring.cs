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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ColoringBook.Common;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;

namespace ColoringBook.Models
{
    public class ColoringBookColoring
    {
        private uint CurrentCell { get; set; }

        public ColoringBookColoringSettings Settings { get; private set; }

        public ColoringBookLibraryImage TemplateImage { get; private set; }

        private ColoringError Error { get; set; }

        public bool IsNewAndUnchanged { get; set; }

        public string ColoringName => Settings.ColoringName;

        public DateTime LastOpenedTime => Settings.LastOpened;

        public InkStrokeContainer InkStrokeContainer { get; private set; } = new InkStrokeContainer();

        public static async Task<ColoringBookColoring> CreateFromTemplateAsync(string templateImageName)
        {
            var coloring = new ColoringBookColoring() { IsNewAndUnchanged = true };
            var coloringName = Path.GetRandomFileName();
            await coloring.InitializeAsync(coloringName, templateImageName);
            return coloring;
        }

        private async Task InitializeAsync(string coloringName, string templateImageName)
        {
            Settings = new ColoringBookColoringSettings(coloringName, templateImageName, Tools.ResolutionType);
            await Settings.SaveSettingsToFileAsync();
            var imageName = Settings.ColoringImageName +
                Tools.GetResolutionTypeAsString(Settings.ColoringResolutionType);
            TemplateImage = await ColoringBookBitmapLibraryImage.LoadImageFromFileAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                imageName + Tools.GetResourceString("FileType/png"),
                imageName + Tools.GetResourceString("FileType/preprocessing"));
            InkStrokeContainer = new InkStrokeContainer();
        }

        public void SetOpenedTimeToNow() => Settings.LastOpened = DateTime.Now;

        public void SetCurrentCell(Point location)
        {
            if (!TemplateImage.IsOnBoundary(location))
            {
                CurrentCell = TemplateImage.GetCorrespondingCellId(location);
            }
        }

        public bool IsWithinUserCurrentCell(Point location) => CurrentCell == 0 ?
            false : CurrentCell == TemplateImage.GetCorrespondingCellId(location);

        public bool HasFilledCells() =>
            (TemplateImage as ColoringBookBitmapLibraryImage)?.CellColorCache.Count > 0;

        public Dictionary<uint, Color> ClearAllCells()
        {
            var oldCache = (TemplateImage as ColoringBookBitmapLibraryImage).CellColorCache;
            TemplateImage.EraseAllCells();
            return oldCache;
        }

        public byte[] ApplyFilledCells(Dictionary<uint, Color> dict) =>
            (TemplateImage as ColoringBookBitmapLibraryImage).ApplyFilledCells(dict);

        public void ClearAllInk() => InkStrokeContainer.Clear();

        public static async Task<ColoringBookColoring> LoadThumbnailDetailsAsync(string coloringName)
        {
            var coloring = new ColoringBookColoring();
            await coloring.LoadSettingsAsync(coloringName);
            return coloring;
        }

        public async Task LoadColoringPageFilesAsync()
        {
            var imageName = Settings.ColoringImageName +
                Tools.GetResolutionTypeAsString(Settings.ColoringResolutionType);
            TemplateImage = await ColoringBookBitmapLibraryImage.LoadImageFromFileAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                imageName + Tools.GetResourceString("FileType/png"),
                imageName + Tools.GetResourceString("FileType/preprocessing"));
            await LoadInkAsync(Settings.ColoringDirectory);
        }

        private async Task LoadInkAsync(StorageFolder coloringDirectory)
        {
            InkStrokeContainer = new InkStrokeContainer();
            var fileName = ColoringName + Tools.GetResourceString("FileType/inkFileType");
            var file = await Tools.GetFileAsync(coloringDirectory, fileName);
            if (file != null)
            {
                using (var stream = await file.OpenSequentialReadAsync())
                {
                    await InkStrokeContainer.LoadAsync(stream);
                }
            }
        }

        private async Task LoadSettingsAsync(string coloringName)
        {
            var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
            var coloringDirectory = await Tools.GetSubDirectoryAsync(coloringsDirectory, coloringName);
            if (coloringDirectory == null)
            {
                Error = ColoringError.ColoringNotFound;
                return;
            }

            try
            {
                Settings = await ColoringBookColoringSettings.LoadSettingsAsync(coloringName);
            }
            catch
            {
                // Set known defaults and mark as error
                Settings = new ColoringBookColoringSettings(coloringName, null, DeviceResolutionType.Uncalculated)
                {
                    ColoringDirectory = coloringDirectory
                };

                Error = ColoringError.SettingsParsing;
                return;
            }

            var doesImageExist = await ColoringBookLibraryImage.CheckImageExistsAsync(
                await Settings.GetLibraryImageLocationAsync(),
                await Settings.GetLibraryImagePreprocessingLocationAsync(),
                Settings.ColoringImageName +
                    Tools.GetResolutionTypeAsString(Settings.ColoringResolutionType) +
                    Tools.GetResourceString("FileType/png"),
                Settings.ColoringImageName +
                    Tools.GetResolutionTypeAsString(Settings.ColoringResolutionType) +
                    Tools.GetResourceString("FileType/preprocessing"));

            if (!doesImageExist)
            {
                Error = ColoringError.TemplateImage;
            }
        }

        public async Task<bool> DeleteIfNewAndUnchangedAsync()
        {
            if (IsNewAndUnchanged)
            {
                await DeleteColoringAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task DeleteIfNewAndUnchangedElseSaveAsync() =>
            await (await DeleteIfNewAndUnchangedAsync() ? Task.CompletedTask : SaveToFileAsync(true));

        public async Task SaveAsync() => await SaveToFileAsync();

        private async Task SaveToFileAsync(bool isQuickSave = false)
        {
            await SaveFilledCellsToFileAsync(Settings.ColoringDirectory);
            await SaveInkToFileAsync(Settings.ColoringDirectory);

            if (!isQuickSave)
            {
                await Settings.SaveSettingsToFileAsync();
            }
        }

        private async Task SaveInkToFileAsync(StorageFolder coloringDirectory)
        {
            var fileName = ColoringName + Tools.GetResourceString("FileType/inkFileType");
            var inkFile = await Tools.CreateFileAsync(coloringDirectory, fileName);

            if (InkStrokeContainer.GetStrokes().Count == 0)
            {
                if (inkFile != null)
                {
                    await inkFile.DeleteAsync();
                }

                return;
            }

            using (var inkFileStream = (await inkFile.OpenStreamForWriteAsync()).AsOutputStream())
            {
                if (inkFileStream == null)
                {
                    throw new Exception("Cannot save to ink file");
                }

                await InkStrokeContainer.SaveAsync(inkFileStream);
            }
        }

        private async Task SaveFilledCellsToFileAsync(StorageFolder coloringDirectory) =>
            await (TemplateImage as ColoringBookBitmapLibraryImage)
                .SaveFilledCellsToFileAsync(ColoringName, coloringDirectory);

        public async Task DeleteColoringAsync()
        {
            try
            {
                await Settings.ColoringDirectory.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                Debug.WriteLine("Coloring.DeleteColoringAsync() - Exception, Could not delete Coloring");
            }
        }

        public static async Task<bool> DisplayErrorDialogAsync(string coloringName)
        {
            var deleteColoringDialog = new ContentDialog
            {
                Title = Tools.GetResourceString("ErrorDialog/Title"),
                Content = Tools.GetResourceString("ErrorDialog/Content"),
                PrimaryButtonText = Tools.GetResourceString("ErrorDialog/IgnoreButtonText"),
                SecondaryButtonText = Tools.GetResourceString("ErrorDialog/DeleteButtonText")
            };

            var result = await deleteColoringDialog.ShowAsync();

            return result == ContentDialogResult.Secondary;
        }

        public bool ColoringHasError() => Error != ColoringError.None;

        private enum ColoringError
        {
            None,
            ColoringNotFound,
            SettingsParsing,
            TemplateImage,
            Ink,
            Ttf
        }
    }
}