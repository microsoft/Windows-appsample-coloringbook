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
using ColoringBook.Common;
using ColoringBook.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace ColoringBook.Views
{
    public sealed partial class ColorPalette : UserControl
    {
        public ColorPaletteViewModel ViewModel { get; } = new ColorPaletteViewModel();

        public ColorPalette()
        {
            InitializeComponent();
            ViewModel.ColorChangedProgrammatically += OnColorChangedProgramatically;
            Loaded += (s, e) => ViewModel.SetPaletteSize(new Size(ActualWidth, ActualHeight));
        }

        public event EventHandler<PaletteColorChangedEventArgs> ColorChanged;

        private int PalettePageIndex { get; set; } = -1;

        private GridViewItem SelectedGrid { get; set; }

        private PaletteSwitchingDirectionWithKeyboard ColorPaletteChangeWithKeyboard { get; set; }

        private void ColorPalette_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
            ViewModel.SetPaletteSize(e.NewSize);

        private void ColorPaletteItemGrid_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                GridViewItem gridViewItem = GetGridViewItemForGrid(grid);
                if (gridViewItem != SelectedGrid)
                {
                    UpdateGridViewItemSelectionState(gridViewItem);
                }

                SelectAndOpenColorPicker(grid);
            }
        }

        private void ColorPaletteItemGrid_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                SelectAndOpenColorPicker(grid);
            }
        }

        private void SelectAndOpenColorPicker(FrameworkElement colorPaletteItemGrid)
        {
            var colorPaletteItem = colorPaletteItemGrid.DataContext as ColorPaletteItem;
            ViewModel.SelectItem(colorPaletteItem);
            ColorChanged?.Invoke(this,
                new PaletteColorChangedEventArgs { NewColor = colorPaletteItem.ColorBrush.Color });

            ColorPicker.Color = colorPaletteItem.ColorBrush.Color;
            ColorPicker.PreviousColor = colorPaletteItem.ColorBrush.Color;
            FlyoutBase.SetAttachedFlyout(colorPaletteItemGrid, ColorPickerFlyout);
            FlyoutBase.ShowAttachedFlyout(colorPaletteItemGrid);
        }

        private void ColorPickerOkButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ColorPickerConfirm(ColorPicker.Color);
            ColorChanged?.Invoke(this, new PaletteColorChangedEventArgs { NewColor = ColorPicker.Color });
            ColorPickerFlyout.Hide();
        }

        private void ColorPickerCancelButton_Click(object sender, RoutedEventArgs e) =>
            ColorPickerFlyout.Hide();

        private void OnColorChangedProgramatically(object sender, PaletteColorChangedEventArgs e)
        {
            var flipViewItems = ColorPaletteFlipView.ItemsPanelRoot?.Children;

            if (flipViewItems != null)
            {
                foreach (FlipViewItem flipViewItem in flipViewItems)
                {
                    if ((flipViewItem.Content as ColorPalettePageViewModel) == ColorPaletteFlipView.SelectedItem)
                    {
                        if (!(flipViewItem.ContentTemplateRoot is GridView currentGridView))
                        {
                            continue;
                        }

                        int index = currentGridView.SelectedIndex;

                        if (!(currentGridView.ItemsPanelRoot?.Children[index] is GridViewItem item))
                        {
                            continue;
                        }

                        UpdateGridViewItemSelectionState(item);
                    }
                }
            }

            ColorChanged?.Invoke(this, e);
        }

        private void ColorPalettePage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var gridView = sender as GridView;
            if (e.OriginalKey == Windows.System.VirtualKey.Right && gridView.SelectedIndex ==
                gridView.ItemsPanelRoot.Children.Count - 1)
            {
                ColorPaletteChangeWithKeyboard = PaletteSwitchingDirectionWithKeyboard.Next;
            }
            else if (e.OriginalKey == Windows.System.VirtualKey.Left && gridView.SelectedIndex == 0)
            {
                ColorPaletteChangeWithKeyboard = PaletteSwitchingDirectionWithKeyboard.Prev;
            }
        }

        private void ColorPalettePage_ItemClick(object sender, ItemClickEventArgs e)
        {
            var colorPaletteItem = e.ClickedItem as ColorPaletteItem;

            // The sender's GridView.SelectedIndex hasn't been updated yet,
            // so get the value through the clicked item.
            int selectedIndex = (sender as GridView).Items.IndexOf(colorPaletteItem);

            if ((sender as GridView).ItemsPanelRoot == null)
            {
                return;
            }

            if ((sender as GridView).ItemsPanelRoot.Children[selectedIndex]
                is GridViewItem clickedGridViewItem)
            {
                if (clickedGridViewItem != SelectedGrid)
                {
                    UpdateGridViewItemSelectionState(clickedGridViewItem);
                }

                if (colorPaletteItem.IsColorSet)
                {
                    ViewModel.SelectItem(colorPaletteItem);
                    ColorChanged?.Invoke(this,
                        new PaletteColorChangedEventArgs { NewColor = colorPaletteItem.ColorBrush.Color });
                }
                else
                {
                    SelectAndOpenColorPicker(clickedGridViewItem.ContentTemplateRoot as Grid);
                }
            }
        }

        // Update visual state of clicked item and previously selected item.
        private void UpdateGridViewItemSelectionState(GridViewItem clickedGridViewItem)
        {
            if (clickedGridViewItem == null)
            {
                return;
            }

            VisualStateManager.GoToState(clickedGridViewItem, "Normal", false);
            VisualStateManager.GoToState(clickedGridViewItem, "SelectedState", false);
            if (SelectedGrid != null)
            {
                VisualStateManager.GoToState(SelectedGrid, "UnselectedState", false);
                SelectedGrid.KeyDown -= ColorPaletteItem_KeyDown;
            }

            clickedGridViewItem.KeyDown += ColorPaletteItem_KeyDown;
            SelectedGrid = clickedGridViewItem;
        }

        private void ColorPaletteItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.OriginalKey == Windows.System.VirtualKey.Enter)
            {
                SelectAndOpenColorPicker((sender as GridViewItem)?.ContentTemplateRoot as Grid);
            }
        }

        private void ColorPaletteItemGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var gridViewItem = GetGridViewItemForGrid(sender as Grid);
            if (gridViewItem != SelectedGrid)
            {
                VisualStateManager.GoToState(gridViewItem, "PointerOverProg", false);
            }
        }

        private void ColorPaletteItemGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var gridViewItem = GetGridViewItemForGrid(sender as Grid);
            if (gridViewItem != SelectedGrid)
            {
                VisualStateManager.GoToState(gridViewItem, "Normal", false);
            }
        }

        private void ColorPaletteItemGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if ((sender as Grid)?.DataContext == ViewModel.CurrentColorPaletteItem)
            {
                var gridViewItem = GetGridViewItemForGrid(sender as Grid);
                SelectedGrid = gridViewItem;
                VisualStateManager.GoToState(gridViewItem, "SelectedState", false);
            }
        }

        // If FlipView selection has been changed with keyboard, set focus on appropriate colorItem.
        private void ColorPaletteFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Because sometimes, Focus shifts to FlipViewItem when arrow key is spammed.
            var x = FocusManager.GetFocusedElement();
            if (x is FlipViewItem)
            {
                ColorPaletteChangeWithKeyboard = (PalettePageIndex + 1 == ColorPaletteFlipView.SelectedIndex) ?
                    PaletteSwitchingDirectionWithKeyboard.Next : PaletteSwitchingDirectionWithKeyboard.Prev;
            }

            PalettePageIndex = ColorPaletteFlipView.SelectedIndex;

            var flipView = sender as FlipView;
            if (ColorPaletteChangeWithKeyboard == PaletteSwitchingDirectionWithKeyboard.None)
            {
                return;
            }

            var flipViewItems = flipView.ItemsPanelRoot.Children;
            GridView currentGridView = null;
            foreach (FlipViewItem flipViewItem in flipViewItems)
            {
                if ((flipViewItem.Content as ColorPalettePageViewModel) == flipView.SelectedItem)
                {
                    currentGridView = flipViewItem.ContentTemplateRoot as GridView;
                    int index = (ColorPaletteChangeWithKeyboard == PaletteSwitchingDirectionWithKeyboard.Next) ?
                        0 : currentGridView.ItemsPanelRoot.Children.Count - 1;
                    var item = currentGridView.ItemsPanelRoot.Children[index] as GridViewItem;
                    item.Focus(FocusState.Keyboard);
                }
            }

            ColorPaletteChangeWithKeyboard = PaletteSwitchingDirectionWithKeyboard.None;
        }

        private static GridViewItem GetGridViewItemForGrid(Grid grid)
        {
            var parent = VisualTreeHelper.GetParent(grid);
            return VisualTreeHelper.GetParent((parent as ContentPresenter).Parent) as GridViewItem;
        }

        private enum PaletteSwitchingDirectionWithKeyboard
        {
            None,
            Next,
            Prev
        }
    }
}
