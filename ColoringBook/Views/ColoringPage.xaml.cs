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
using ColoringBook.Components;
using ColoringBook.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ColoringBook.Views
{
    public sealed partial class ColoringPage : Page
    {
        public ColoringPageController Controller { get; }

        public ColoringPage()
        {
            InitializeComponent();
            var canvasElements = new CanvasElements
            {
                CanvasScrollViewer = CanvasScrollViewer,
                InkCanvas = InkCanvas,
                DryInkCanvas = DryCanvas,
                ColoringGrid = ColoringGrid
            };

            Controller = new ColoringPageController(canvasElements, ColorPalette.ViewModel);
            Controller.ColoringLoadCompleted += ViewModelOnColoringLoadCompleted;

            Loaded += ColoringPage_Loaded;
            SizeChanged += ColoringPage_SizeChanged;
        }

        // Dial Support.
        private RadialControlHelper RadialControlHelper { get; } = new RadialControlHelper();

        private PhotoPrintHelper PrintHelper { get; set; }

        private double? MaxDpiScale { get; set; }

        // For inktoolbar.
        public SolidColorBrush CurrentColorBrush { get; set; } =
            new SolidColorBrush { Color = Colors.MediumOrchid };

        private async void ColoringPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CanvasViewbox.MaxHeight = ColoringHolderGrid.ActualHeight;
            CanvasViewbox.MaxWidth = ColoringHolderGrid.ActualWidth;

            // Zoom out fully as the viewbox causes the context from the zoom to be lost.
            // Add delay to allow viewbox UI operations to complete.
            await Task.Delay(1);
            CanvasScrollViewer.ChangeView(null, null, 1, true);
        }

        public static async Task NotifyUserAsync(string message) =>
            await new ContentDialog() { Content = message, CloseButtonText = "Close" }.ShowAsync();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Controller.LoadColoringAsync(e.Parameter as ColoringBookColoring).ContinueWithoutWaiting();

            // Enable Back button.
            SystemNavigationManager.GetForCurrentView().BackRequested += ColoringPage_BackRequested;

            PrintHelper = new PhotoPrintHelper(this);
            PrintHelper.RegisterForPrinting();

            // Initialize Dial Options.
            RadialControlHelper.InitializeForColoringPage();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Disable Back button.
            SystemNavigationManager.GetForCurrentView().BackRequested -= ColoringPage_BackRequested;

            // Deregister printer helper.
            PrintHelper?.UnregisterForPrinting();

            InkToolbar.UnregisterRadialController();

            var display = DisplayInformation.GetForCurrentView();
            display.DpiChanged -= Display_DpiChanged;

            // Remove custom dial options.
            RadialControlHelper?.Unregister();

            Controller.CleanUp();
        }

        private bool backRequested;

        private async void ColoringPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            // Do nothing if the back button was clicked repeatedly before navigation could occur.
            if (backRequested)
            {
                return;
            }

            backRequested = true;
            e.Handled = true;

            if (await Controller.DeleteNewColoringIfUnchangedAsync())
            {
                NavigationController.LoadHome(typeof(LibraryPivotItem));
            }
            else
            {
                // Ensure that we're not waiting to save.
                Controller.ResetAutosaveTimer();

                await Controller.SaveAsync();
                NavigationController.LoadHome(typeof(MyArtworkPivotItem));
            }
        }

        private void ColoringPage_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentColorBrush = new SolidColorBrush(ColorPalette.ViewModel.CurrentColor);
            InkToolbar.TargetCurrentColorBrush = CurrentColorBrush;

            // Pass these references to radial controller.
            RadialControlHelper.ColorPaletteViewModel = ColorPalette.ViewModel;
            RadialControlHelper.UndoOperation += (s, e2) => Controller.OnUndo();
            RadialControlHelper.RedoOperation += (s, e2) => Controller.OnRedo();

            var display = DisplayInformation.GetForCurrentView();
            display.DpiChanged += Display_DpiChanged;
            Display_DpiChanged(display, null);
        }

        private void ViewModelOnColoringLoadCompleted(object sender, EventArgs eventArgs)
        {
            CanvasViewbox.MaxHeight = ColoringHolderGrid.ActualHeight;
            CanvasViewbox.MaxWidth = ColoringHolderGrid.ActualWidth;
            Controller.InkScale = Controller.TemplateImageWidth / ColoringHolderGrid.ActualWidth;
            InkToolbar.SetInkScaling(Controller.InkScale);
        }

        private void DryCanvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args) =>
            Controller.DrawDryInk(args.DrawingSession);

        private void Display_DpiChanged(DisplayInformation sender, object args)
        {
            Controller.DisplayDpi = sender.LogicalDpi;
            ScrollViewer_ViewChanged(null, null);
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var dpiAdjustment = 96 / Controller.DisplayDpi;

            // Adjust DPI to match the current zoom level.
            var dpiScale = dpiAdjustment * CanvasScrollViewer.ZoomFactor;

            // To boost performance during pinch-zoom manipulations, we only update DPI when it has
            // changed by more than 20%, or at the end of the zoom (when e.IsIntermediate reports false).
            var ratio = DryCanvas.DpiScale / dpiScale;

            if (e == null || !e.IsIntermediate || ratio <= 0.8 || ratio >= 1.25)
            {
                if (DryCanvas.ReadyToDraw)
                {
                    // We can't calculate MaxDpiScale before DryCanvas.ReadyToDraw.
                    // DryCanvas is only ReadyToDraw some arbitrary time after
                    // ViewModelOnColoringLoadCompleted completes,
                    // so we have to calculate the scale on the first zoom event.
                    if (MaxDpiScale == null)
                    {
                        CalculateMaxDpiScale();
                    }

                    DryCanvas.DpiScale = Math.Min((float)MaxDpiScale.Value, dpiScale);
                }
                else
                {
                    DryCanvas.DpiScale = dpiScale;
                }
            }
        }

        private void CalculateMaxDpiScale()
        {
            var canvasSize = DryCanvas.Size;
            var canvasWidth = DryCanvas.ConvertDipsToPixels((float)canvasSize.Width,
                CanvasDpiRounding.Floor);
            var canvasHeight = DryCanvas.ConvertDipsToPixels((float)canvasSize.Height,
                CanvasDpiRounding.Floor);
            var maxDimension = Math.Max(canvasWidth, canvasHeight);
            var maxDimensionScaleAdjusted = maxDimension / DryCanvas.DpiScale;
            var maxBitmapSize = DryCanvas.Device.MaximumBitmapSizeInPixels;

            MaxDpiScale = (double)maxBitmapSize / maxDimensionScaleAdjusted;
        }

        private void ColorPalette_OnColorChanged(object sender, PaletteColorChangedEventArgs e)
        {
            var inkDrawingAttributes = InkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            inkDrawingAttributes.Color = e.NewColor;
            InkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(inkDrawingAttributes);
            CurrentColorBrush.Color = e.NewColor;
        }

        private void InkToolbar_OnDrawingToolChanged(object sender, DrawingToolChangedEventArgs e) =>
            Controller.DrawingTool = e.NewDrawingTool;

        private async void PrintButton_OnClick(object sender, RoutedEventArgs e)
        {
            var export = await ColoringExporter.CreateExportFromUiElementAsync(ColoringGrid,
                (int)ColoringGrid.ActualWidth, (int)ColoringGrid.ActualHeight);

            PrintHelper.Export = export;
            await Components.PrintHelper.ShowPrintUIAsync();
        }
    }
}