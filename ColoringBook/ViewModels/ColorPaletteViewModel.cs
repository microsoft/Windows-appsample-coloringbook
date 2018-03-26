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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.FileIO;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace ColoringBook.ViewModels
{
    public class ColorPaletteViewModel : Observable
    {
        public ColorPaletteViewModel()
        {
            LoadDefaultColors();
            BuildPaletteItems();

            SetPaletteSize(new Size(750, 80));

            LoadColorPaletteStateAsync().ContinueWithoutWaiting();
        }

        private static string ColorPaletteStateFileName { get; }
            = Tools.GetResourceString("FileName/ColorPaletteFileName");

        private const int MaxColorCount = 144;

        private int?[] Colors { get; } = new int?[MaxColorCount];

        private ColorPaletteItem[] ColorPaletteItems { get; } = new ColorPaletteItem[MaxColorCount];

        private int SelectedColorIndex { get; set; }

        private int SelectedColorPageIndex { get; set; }

        private int ColorsPerPage { get; set; }

        public event EventHandler<PaletteColorChangedEventArgs> ColorChangedProgrammatically;

        public ObservableCollection<ColorPalettePageViewModel> PalettePages { get; }
            = new ObservableCollection<ColorPalettePageViewModel>();

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage == value)
                {
                    return;
                }

                if (PalettePages.Count > 0)
                {
                    if (_currentPage >= 0)
                    {
                        PalettePages[_currentPage].IsSelected = false;
                    }

                    if (value >= 0)
                    {
                        PalettePages[value].IsSelected = true;
                    }
                }

                Set(ref _currentPage, value);
            }
        }

        private int _currentPage;

        public Color CurrentColor
        {
            get
            {
                if (ColorPaletteItems[SelectedColorIndex] != null)
                {
                    return ColorPaletteItems[SelectedColorIndex].ColorBrush.Color;
                }

                return Windows.UI.Colors.LightGray;
            }
        }

        public ColorPaletteItem CurrentColorPaletteItem => ColorPaletteItems[SelectedColorIndex];

        public void GoToNextPage()
        {
            if (CurrentPage < PalettePages.Count - 1)
            {
                CurrentPage++;
            }
        }

        public void GoToPreviousPage()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
            }
        }

        private void LoadDefaultColors()
        {
            int[] defaultColors =
            {
                // Spring colors.
                0x0abac1, 0x0acb9c, 0x067a1c, 0x60b24c, 0xfff970, 0xbaffa8, 0xff9002, 0xff540e, 0xf83d5c, 0xb25a9f, 0xd92588, 0xa6123a,

                // Summer colors.
                0xff554d, 0xe8b746, 0x93ff59, 0x46dbe8, 0x9659ff, 0x32ffbc, 0xff7972, 0xcc134d, 0x7bcdd1, 0xeff28d, 0xf2bf80, 0xf29580,

                // Autumn colors.
                0x36472a, 0x994124, 0xac5926, 0xff8500, 0xffb400, 0xf25c05, 0xbd2803, 0xa64029, 0x732a10, 0x736b43, 0x868c56, 0x2e594a,

                // Winter colors.
                0x273f5a, 0x4b81a5, 0x74a0bf, 0x98c4da, 0xbddbe8, 0xdcfeff, 0xbde8dc, 0xcfffe3, 0xdeebd8, 0xc3bbb2, 0x8e7f78, 0x706662,

                // Primary colors.
                0xff006c, 0xfff800, 0x0088ff, 0x0ef4f7, 0xffffff, 0xfffc42, 0xff7f00, 0xff0000, 0x00ff00, 0x0000ff, 0x808080, 0,

                // Neutral colors.
                0x4c5154, 0x818c89, 0xbcbfb6, 0xf0f1eb, 0xdbdfde, 0xd7d8d2, 0xb3babd, 0xaeaeaf, 0xc3bbb2, 0xa89d98, 0x8e7f78, 0x5f4640,

                // Petal colors.
                0x9dff9b, 0xe8d58f, 0xffb7aa, 0xbb90e8, 0x9eefff, 0xeff28d, 0xf2ae80, 0xf28080, 0x9c9475, 0x7ecbbd, 0xffcbbd, 0xbd9dca,

                // Landscape colors.
                0xf28080, 0x7a601e, 0x436228, 0x0a5f35, 0x197826, 0x758c65, 0xd9d5a7, 0xa68729, 0xbecbb1, 0x30513c, 0xebcd5b, 0xa8b22f,

                // Sea colors.
                0x001c36, 0x63245, 0x055ba1, 0x0d8fac, 0x00cdb4, 0x00dfae, 0x01efad, 0xa5f2ed, 0x97bdd7, 0xcae9f4, 0x629aa3, 0x9f998c
            };

            for (int i = 0; i < defaultColors.Length; i++)
            {
                if (i >= MaxColorCount)
                {
                    break;
                }

                Colors[i] = defaultColors[i];
            }

            SelectedColorIndex = 0;
        }

        private async Task LoadColorPaletteStateAsync()
        {
            var coloringDirectory = await Tools.GetColoringsDirectoryAsync();
            try
            {
                var colorReader = new ColorSettingsReader();
                var state = await colorReader.ReadAsync(coloringDirectory, ColorPaletteStateFileName);
                for (int i = 0; i < MaxColorCount; i++)
                {
                    if (i >= state.Colors.Length)
                    {
                        Colors[i] = null;
                    }
                    else
                    {
                        Colors[i] = state.Colors[i];
                    }
                }

                SelectedColorIndex = state.SelectedIndex;
                if (SelectedColorIndex < 0)
                {
                    SelectedColorIndex = 0;
                }
            }
            catch
            {
                // Save the default colors we have already loaded.
                SaveColorPaletteStateAsync().ContinueWithoutWaiting();
            }

            BuildPaletteItems();
            RebuildPalettePages(PalettePages.Count, ColorsPerPage);
            SetPaletteToSelectedColor();

            ColorChangedProgrammatically?.Invoke(this, new PaletteColorChangedEventArgs
                { NewColor = ColorPaletteItems[SelectedColorIndex].ColorBrush.Color });
        }

        public async Task SaveColorPaletteStateAsync()
        {
            var coloringDirectory = await Tools.GetColoringsDirectoryAsync();
            var colorWriter = new ColorSettingsWriter();
            var state = new ColorPaletteState
            {
                Colors = Colors,
                SelectedIndex = SelectedColorIndex
            };
            await colorWriter.WriteAsync(coloringDirectory, ColorPaletteStateFileName, state);
        }

        public static async Task DeleteColorFileAsync()
        {
            var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
            var coloringFile = await Tools.GetFileAsync(coloringsDirectory, ColorPaletteStateFileName);
            if (coloringFile != null)
            {
                await coloringFile.DeleteAsync();
            }
        }

        public void SetPaletteSize(Size newSize)
        {
            var colorsPerPage = PageColorCountForWidth(newSize.Width);
            int pageCount = (int)Math.Ceiling((double)MaxColorCount / colorsPerPage);

            // Don't rebuild palette when it's not necessary.
            if ((PalettePages.Count > 0) &&
                (colorsPerPage == PalettePages.ElementAt(0).PageColors.Count))
            {
                return;
            }

            ColorsPerPage = colorsPerPage;
            RebuildPalettePages(pageCount, colorsPerPage);
            SetPaletteToSelectedColor();
        }

        private void RebuildPalettePages(int pageCount, int colorsPerPage)
        {
            PalettePages.Clear();

            // Add preset colors.
            var colorIndex = 0;
            for (int i = 0; i < pageCount; i++)
            {
                var newPage = new ColorPalettePageViewModel();
                newPage.Selected += (s, e) => CurrentPage =
                    PalettePages.IndexOf(s as ColorPalettePageViewModel);

                for (int j = 0; j < colorsPerPage; j++)
                {
                    if (colorIndex >= MaxColorCount)
                    {
                        break;
                    }

                    var item = ColorPaletteItems[colorIndex];
                    newPage.PageColors.Add(item);
                    colorIndex++;
                }

                PalettePages.Add(newPage);
            }
        }

        public void SetPaletteToSelectedColor()
        {
            var page = SelectedColorIndex / ColorsPerPage;
            var pagePosition = SelectedColorIndex % ColorsPerPage;

            CurrentPage = page;
            PalettePages[page].SelectedIndex = pagePosition;
            SelectedColorPageIndex = page;
        }

        private void BuildPaletteItems()
        {
            for (int i = 0; i < MaxColorCount; i++)
            {
                var color = Colors[i];
                ColorPaletteItem item;
                if (color == null)
                {
                    item = new ColorPaletteItem();
                }
                else
                {
                    var brush = new SolidColorBrush(Tools.ColorFromHexInteger(color.Value));
                    item = new ColorPaletteItem(brush);
                }

                ColorPaletteItems[i] = item;
            }
        }

        private void SelectionChanged(ColorPaletteItem item)
        {
            var colorIndex = Array.IndexOf(ColorPaletteItems, item);

            // Unselect old color.
            if (SelectedColorPageIndex != CurrentPage)
            {
                PalettePages[SelectedColorPageIndex].SelectedIndex = -1;
            }

            SelectedColorPageIndex = CurrentPage;
            SelectedColorIndex = colorIndex;
        }

        public void SelectItem(ColorPaletteItem item)
        {
            SelectionChanged(item);
            var currentPage = PalettePages[CurrentPage];
            var newIndex = currentPage.PageColors.IndexOf(item);
            if (newIndex >= 0)
            {
                currentPage.SelectedIndex = newIndex;
            }
        }

        public void SelectNextItem()
        {
            // Find next available item (do not set to an unset item).
            var nextItemIndex = SelectedColorIndex;

            while (nextItemIndex < ColorPaletteItems.Length - 1)
            {
                nextItemIndex++;

                if (!ColorPaletteItems[nextItemIndex].IsColorSet)
                {
                    continue;
                }

                SelectionChanged(ColorPaletteItems[nextItemIndex]);
                SetPaletteToSelectedColor();
                ColorChangedProgrammatically?.Invoke(this, new PaletteColorChangedEventArgs
                    { NewColor = ColorPaletteItems[SelectedColorIndex].ColorBrush.Color });
                break;
            }
        }

        public void SelectPreviousItem()
        {
            // Find next available item (do not set to an unset item).
            var nextItemIndex = SelectedColorIndex;

            while (nextItemIndex > 0)
            {
                nextItemIndex--;

                if (!ColorPaletteItems[nextItemIndex].IsColorSet)
                {
                    continue;
                }

                SelectionChanged(ColorPaletteItems[nextItemIndex]);
                SetPaletteToSelectedColor();
                ColorChangedProgrammatically?.Invoke(this, new PaletteColorChangedEventArgs
                    { NewColor = ColorPaletteItems[SelectedColorIndex].ColorBrush.Color });
                break;
            }
        }

        public void ColorPickerConfirm(Color color)
        {
            ColorPaletteItems[SelectedColorIndex].ColorBrush = new SolidColorBrush(color);

            // Set into the array to save.
            var colorStr = color.ToString();
            var colorInt = int.Parse(colorStr.Substring(3), NumberStyles.HexNumber);
            Colors[SelectedColorIndex] = colorInt;

            SaveColorPaletteStateAsync().ContinueWithoutWaiting();
        }

        private static int PageColorCountForWidth(double width)
        {
            var colorAreaWidth = width - 78;
            var colorWidth = 56;
            var count = (int)(colorAreaWidth / colorWidth);
            return count;
        }
    }
}
