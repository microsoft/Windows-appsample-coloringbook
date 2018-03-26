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
using System.Numerics;
using ColoringBook.Common;
using ColoringBook.Models;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBook.Views
{
    public sealed partial class ColoringBookInkToolbar : UserControl
    {
        public ColoringBookInkToolbar()
        {
            InitializeComponent();

            PenWidthChangedHandler = PenStrokeWidthSlider_PointerReleased;
            PenStrokeWidthSlider.AddHandler(PointerReleasedEvent, PenWidthChangedHandler, true);

            PencilWidthChangedHandler = PencilStrokeWidthSlider_PointerReleased;
            PencilStrokeWidthSlider.AddHandler(PointerReleasedEvent, PencilWidthChangedHandler, true);

            Initialize();
            Loaded += ColoringBookInkToolbar_Loaded;

            AccessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;
            InitializeHighContrastFillButtonHover();

            DisplayInformation = DisplayInformation.GetForCurrentView();
            DisplayInformation.DpiChanged += DisplayInformation_DpiChanged;
            UpdateUiScaling();
        }

        public event EventHandler<DrawingToolChangedEventArgs> DrawingToolChanged;

        public InkCanvas TargetInkCanvas { get; set; }

        public SolidColorBrush TargetCurrentColorBrush { get; set; }

        private void DisplayInformation_DpiChanged(DisplayInformation sender, object args)
        {
            DisplayInformation = sender;
            UpdateUiScaling();
        }

        private void UpdateUiScaling()
        {
            int currentScaling = (int)(DisplayInformation.RawPixelsPerViewPixel * 100);
            if (currentScaling >= 400)
            {
                CurrentUiScaling = 400;
            }
            else if (currentScaling >= 200)
            {
                CurrentUiScaling = 200;
            }
            else if (currentScaling >= 150)
            {
                CurrentUiScaling = 150;
            }
            else if (currentScaling >= 125)
            {
                CurrentUiScaling = 125;
            }
            else
            {
                CurrentUiScaling = 100;
            }
        }

        private BitmapImage GetBitmapImageForFillIcon(string contrastName, int scale)
        {
            BitmapImage bitmapImage = new BitmapImage();
            string addressOfImage = string.Concat("../Assets/Icons/Bucket/BucketColor.scale-",
                scale.ToString(), "_contrast-", contrastName, ".png");
            bitmapImage.UriSource = new Uri((this as FrameworkElement).BaseUri, addressOfImage);
            return bitmapImage;
        }

        private void InitializeHighContrastFillButtonHover()
        {
            if (!AccessibilitySettings.HighContrast)
            {
                ContrastHandlerAttached = ContrastHandler.None;
                return;
            }

            string highContrastScheme = AccessibilitySettings.HighContrastScheme;
            if (highContrastScheme == "High Contrast Black")
            {
                FillCellButton.PointerEntered += SetFillCellButtonToWhite;
                FillCellButton.PointerExited += SetFillCellButtonToBlack;
                ContrastHandlerAttached = ContrastHandler.Black;

                // On changing HC mode while app is running, it doesn't update icon. Do it explicitly.
                FillCellButtonImage.Source = GetBitmapImageForFillIcon("black", CurrentUiScaling);
            }
            else if (highContrastScheme == "High Contrast White")
            {
                FillCellButton.PointerEntered += SetFillCellButtonToBlack;
                FillCellButton.PointerExited += SetFillCellButtonToWhite;
                ContrastHandlerAttached = ContrastHandler.White;

                // On changing HC mode while app is running, it doesn't update icon. Do it explicitly.
                FillCellButtonImage.Source = GetBitmapImageForFillIcon("white", CurrentUiScaling);
            }
            else
            {
                // On changing HC mode while app is running, it doesn't update icon. Do it explicitly.
                FillCellButtonImage.Source = GetBitmapImageForFillIcon("black", CurrentUiScaling);
                ContrastHandlerAttached = ContrastHandler.None;
            }
        }

        private void SetFillCellButtonToWhite(object sender, PointerRoutedEventArgs e) =>
            FillCellButtonImage.Source = GetBitmapImageForFillIcon("white", CurrentUiScaling);

        private void SetFillCellButtonToBlack(object sender, PointerRoutedEventArgs e) =>
            FillCellButtonImage.Source = GetBitmapImageForFillIcon("black", CurrentUiScaling);

        private void AccessibilitySettings_HighContrastChanged(AccessibilitySettings sender, object args)
        {
            if (ContrastHandlerAttached == ContrastHandler.Black)
            {
                FillCellButton.PointerEntered -= SetFillCellButtonToWhite;
                FillCellButton.PointerExited -= SetFillCellButtonToBlack;
            }
            else if (ContrastHandlerAttached == ContrastHandler.White)
            {
                FillCellButton.PointerEntered -= SetFillCellButtonToBlack;
                FillCellButton.PointerExited -= SetFillCellButtonToWhite;
            }

            InitializeHighContrastFillButtonHover();
        }

        private void ColoringBookInkToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial button state
            PenButton.IsChecked = true;
            VisualStateManager.GoToState(PenButton, "ShowExtensionGlyph", false);
        }

        private void Initialize()
        {
            CreatePenPreviewStroke();
            UpdatePenPreviewInkStroke();

            CreatePencilPreviewStroke();
            UpdatePencilPreviewInkStroke();

            InitializeRadialController();
        }

        private void OnDrawingToolChanged(DrawingTool newTool)
        {
            DrawingToolChanged?.Invoke(this,
                new DrawingToolChangedEventArgs { NewDrawingTool = newTool });

            if (newTool == DrawingTool.FillCell)
            {
                SelectedToolIndex = 0;
            }
            else if (newTool == DrawingTool.Pen)
            {
                SelectedToolIndex = 1;
            }
            else if (newTool == DrawingTool.Pencil)
            {
                SelectedToolIndex = 2;
            }
            else
            {
                // Eraser
                SelectedToolIndex = 3;
            }
        }

        private void InitializeRadialController()
        {
            RadialController = RadialController.CreateForCurrentView();
            RadialController.RotationResolutionInDegrees = 15;
            RadialController.Menu.Items.Clear();
            RadialController.RotationChanged += RadialController_RotationChanged;
            RadialController.ButtonClicked += RadialController_ButtonClicked;

            RadialControllerMenuItem inkingToolRadialMenuItem =
                RadialControllerMenuItem.CreateFromKnownIcon("Ink Tool",
                    RadialControllerMenuKnownIcon.PenType);
            RadialControllerMenuItem strokeSizeRadialMenuItem =
                RadialControllerMenuItem.CreateFromKnownIcon("Size",
                    RadialControllerMenuKnownIcon.InkThickness);

            inkingToolRadialMenuItem.Invoked += InkingToolRadialMenuItem_Invoked;
            strokeSizeRadialMenuItem.Invoked += StrokeSizeRadialMenuItem_Invoked;

            RadialController.Menu.Items.Add(inkingToolRadialMenuItem);
            RadialController.Menu.Items.Add(strokeSizeRadialMenuItem);
        }

        public void UnregisterRadialController()
        {
            if (RadialController != null)
            {
                RadialController.Menu.Items.Clear();
                RadialController.RotationChanged -= RadialController_RotationChanged;
                RadialController.ButtonClicked -= RadialController_ButtonClicked;
            }

            RadialControllerConfiguration.GetForCurrentView().ResetToDefaultMenuItems();
        }

        private void StrokeSizeRadialMenuItem_Invoked(RadialControllerMenuItem sender, object args)
        {
            CurrentRadialSelection = ColoringBookRadialController.StrokeSize;

            // Open flyout and bring focus to slider.
            if (SelectedToolIndex == 1 || SelectedToolIndex == 2)
            {
                RadioButton currentTool = (SelectedToolIndex == 1) ? PenButton : PencilButton;
                var flyout = FlyoutBase.GetAttachedFlyout(currentTool);
                flyout.ShowAt(currentTool as FrameworkElement);
                Slider slider = (SelectedToolIndex == 1) ? PenStrokeWidthSlider : PencilStrokeWidthSlider;
                slider.Focus(FocusState.Keyboard);
            }
        }

        private void InkingToolRadialMenuItem_Invoked(RadialControllerMenuItem sender, object args) =>
            CurrentRadialSelection = ColoringBookRadialController.InkingTool;

        private void RadialController_ButtonClicked(RadialController sender,
            RadialControllerButtonClickedEventArgs args)
        {
            if (SelectedToolIndex == 1 || SelectedToolIndex == 2)
            {
                // Pen or pencil.
                RadioButton currentTool = (SelectedToolIndex == 1) ? PenButton : PencilButton;
                var flyout = FlyoutBase.GetAttachedFlyout(currentTool);
                Slider slider = (SelectedToolIndex == 1) ? PenStrokeWidthSlider : PencilStrokeWidthSlider;

                // Flyout is opened and focus is on slider.
                if (slider.FocusState == FocusState.Keyboard)
                {
                    flyout.Hide();
                    currentTool.Focus(FocusState.Keyboard);
                }
                else
                {
                    // Flyout is not open, and dial click is on pen/pencil.
                    CurrentRadialSelection = ColoringBookRadialController.StrokeSizeDialClick;
                    flyout.ShowAt(currentTool as FrameworkElement);
                    flyout.Closed += DialInvokedPenPencilFlyoutClosed;
                    slider.Focus(FocusState.Keyboard);
                }
            }
            else if (SelectedToolIndex == 3)
            {
                if (CurrentRadialSelection == ColoringBookRadialController.InkingTool)
                {
                    // Eraser, open flyout and now rotation goes to selection of erasing tool.
                    CurrentRadialSelection = ColoringBookRadialController.EraserTool;
                    var flyout = FlyoutBase.GetAttachedFlyout(EraserButton);
                    flyout.ShowAt(EraserButton as FrameworkElement);
                    flyout.Opened += DialInvokedEraserFlyout_Opened;
                    flyout.Closed += EraserButtonFlyoutClosed;
                }
                else if (CurrentRadialSelection == ColoringBookRadialController.EraserTool)
                {
                    // If dial has been clicked while selecting erase tool, we collapse the flyout.
                    var flyout = FlyoutBase.GetAttachedFlyout(EraserButton);
                    flyout.Hide();
                }
            }
        }

        private void DialInvokedPenPencilFlyoutClosed(object sender, object e)
        {
            CurrentRadialSelection = ColoringBookRadialController.InkingTool;
            (sender as FlyoutBase).Closed -= DialInvokedPenPencilFlyoutClosed;
        }

        private void DialInvokedEraserFlyout_Opened(object sender, object e)
        {
            // Now set focus on selected erasing tool.
            if (DrawingTool == DrawingTool.Eraser)
            {
                StrokeEraseListItem.Focus(FocusState.Keyboard);
            }
            else
            {
                CellEraseListItem.Focus(FocusState.Keyboard);
            }

            (sender as FlyoutBase).Opened -= DialInvokedEraserFlyout_Opened;
        }

        private void RadialController_RotationChanged(RadialController sender,
            RadialControllerRotationChangedEventArgs args)
        {
            RadioButton[] inkingTools = { FillCellButton, PenButton, PencilButton, EraserButton };
            Action[] inkingActions =
            {
                OnFillCellButtonClicked,
                OnPenButtonClicked,
                OnPencilButtonClicked,
                OnEraserButtonClicked
            };
            if (CurrentRadialSelection == ColoringBookRadialController.InkingTool)
            {
                SelectedToolIndex = (4 + SelectedToolIndex +
                    (args.RotationDeltaInDegrees > 0 ? 1 : -1)) % 4;
                inkingActions[SelectedToolIndex]();
                inkingTools[SelectedToolIndex].IsChecked = true;
                inkingTools[SelectedToolIndex].Focus(FocusState.Keyboard);
            }
            else if (CurrentRadialSelection == ColoringBookRadialController.StrokeSize)
            {
                if (SelectedToolIndex == 1 || SelectedToolIndex == 2)
                {
                    var flyout = FlyoutBase.GetAttachedFlyout(inkingTools[SelectedToolIndex]);
                    flyout.ShowAt(inkingTools[SelectedToolIndex] as FrameworkElement);
                    Slider slider = (SelectedToolIndex == 1) ?
                        PenStrokeWidthSlider : PencilStrokeWidthSlider;
                    slider.Focus(FocusState.Keyboard);
                    int val = (int)args.RotationDeltaInDegrees;
                    int newVal = ((int)slider.Value) + (val > 0 ? 1 : -1);
                    newVal = (newVal > 24) ? 24 : newVal;
                    newVal = (newVal < 0) ? 0 : newVal;
                    slider.Value = newVal;
                }
            }
            else if (CurrentRadialSelection == ColoringBookRadialController.StrokeSizeDialClick)
            {
                Slider slider = (SelectedToolIndex == 1) ? PenStrokeWidthSlider : PencilStrokeWidthSlider;
                slider.Focus(FocusState.Keyboard);
                int val = (int)args.RotationDeltaInDegrees;
                int newVal = ((int)slider.Value) + (val > 0 ? 1 : -1);
                newVal = (newVal > 24) ? 24 : newVal;
                newVal = (newVal < 0) ? 0 : newVal;
                slider.Value = newVal;
            }
            else if (CurrentRadialSelection == ColoringBookRadialController.EraserTool)
            {
                if ((args.RotationDeltaInDegrees > 0) && (DrawingTool == DrawingTool.Eraser))
                {
                    EraserFlyoutList.SelectedIndex = 1;
                    CellEraseListItem.Focus(FocusState.Keyboard);
                }
                else if ((args.RotationDeltaInDegrees < 0) && (DrawingTool == DrawingTool.EraseCell))
                {
                    EraserFlyoutList.SelectedIndex = 0;
                    StrokeEraseListItem.Focus(FocusState.Keyboard);
                }
            }
        }

        private void PenButton_Checked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(PenButton, "ShowExtensionGlyph", false);

        private void PenButton_Unchecked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(PenButton, "HideExtensionGlyph", false);

        private void OnOpenPenButtonFlyout()
        {
            FlyoutBase.ShowAttachedFlyout(PenButton);
            VisualStateManager.GoToState(PenButton, "HideExtensionGlyph", false);
            var flyout = FlyoutBase.GetAttachedFlyout(PenButton);
            flyout.Closed += PenButtonFlyoutClosed;
        }

        private void PenButtonFlyoutClosed(object sender, object e)
        {
            VisualStateManager.GoToState(PenButton, "ShowExtensionGlyph", false);
            if (sender is FlyoutBase flyoutBase)
            {
                flyoutBase.Closed -= PenButtonFlyoutClosed;
            }
        }

        private void CreatePenPreviewStroke()
        {
            var strokePreviewInkPoints = new List<InkPoint>();
            for (var i = 0; i < PreviewStrokeCoordinates.Length; i += 2)
            {
                var newPoint = new Point(PreviewStrokeCoordinates[i],
                    PreviewStrokeCoordinates[i + 1] + 10);
                var inkPoint = new InkPoint(newPoint, 0.5f);
                strokePreviewInkPoints.Add(inkPoint);
            }

            var inkStrokeBuilder = new InkStrokeBuilder();
            var newStroke = inkStrokeBuilder.CreateStrokeFromInkPoints(
                strokePreviewInkPoints, Matrix3x2.Identity);
            PenPreviewInkStroke = newStroke;
        }

        private void UpdatePenPreviewInkStroke()
        {
            var currentStrokeSize = PenStrokeWidthSlider.Value;
            var inkDrawingAttributes = PenStrokePreviewInkCanvas.InkPresenter
                .CopyDefaultDrawingAttributes();
            inkDrawingAttributes.Color = StrokePreviewColor;
            inkDrawingAttributes.Size = new Size(currentStrokeSize, currentStrokeSize);

            if (PenPreviewInkStroke == null)
            {
                CreatePenPreviewStroke();
            }

            PenPreviewInkStroke.DrawingAttributes = inkDrawingAttributes;

            var strokeContainer = PenStrokePreviewInkCanvas.InkPresenter.StrokeContainer;
            foreach (var stroke in strokeContainer.GetStrokes())
            {
                stroke.Selected = true;
            }

            strokeContainer.AddStroke(PenPreviewInkStroke.Clone());
            strokeContainer.DeleteSelected();
        }

        private void PenStrokeWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) =>
            UpdatePenPreviewInkStroke();

        private void PenStrokeWidthSlider_PointerReleased(object sender, RoutedEventArgs e) =>
            FlyoutBase.GetAttachedFlyout(PenButton)?.Hide();

        private void PenStrokeWidthSlider_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                var y = FlyoutBase.GetAttachedFlyout(PenButton);
                y?.Hide();
            }
        }

        private void PencilButton_Checked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(PencilButton, "ShowExtensionGlyph", false);

        private void PencilButton_Unchecked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(PencilButton, "HideExtensionGlyph", false);

        private void OnOpenPencilButtonFlyout()
        {
            FlyoutBase.ShowAttachedFlyout(PencilButton);
            VisualStateManager.GoToState(PencilButton, "HideExtensionGlyph", false);
            var flyout = FlyoutBase.GetAttachedFlyout(PencilButton);
            flyout.Closed += PencilButtonFlyoutClosed;
        }

        private void PencilButtonFlyoutClosed(object sender, object e)
        {
            VisualStateManager.GoToState(PencilButton, "ShowExtensionGlyph", false);
            if (sender is FlyoutBase flyoutBase)
            {
                flyoutBase.Closed -= PencilButtonFlyoutClosed;
            }
        }

        private void CreatePencilPreviewStroke()
        {
            var strokePreviewInkPoints = new List<InkPoint>();
            for (var i = 0; i < PreviewStrokeCoordinates.Length; i += 2)
            {
                var newPoint = new Point(PreviewStrokeCoordinates[i],
                    PreviewStrokeCoordinates[i + 1] + 10);
                var inkPoint = new InkPoint(newPoint, 1f);
                strokePreviewInkPoints.Add(inkPoint);
            }

            var inkStrokeBuilder = new InkStrokeBuilder();
            var newStroke = inkStrokeBuilder.CreateStrokeFromInkPoints(
                strokePreviewInkPoints, Matrix3x2.Identity);
            PencilPreviewInkStroke = newStroke;
        }

        private void UpdatePencilPreviewInkStroke()
        {
            var currentStrokeSize = PencilStrokeWidthSlider.Value;
            var inkDrawingAttributes = InkDrawingAttributes.CreateForPencil();
            inkDrawingAttributes.Color = StrokePreviewColor;
            inkDrawingAttributes.Size = new Size(currentStrokeSize, currentStrokeSize);

            if (PencilPreviewInkStroke == null)
            {
                CreatePencilPreviewStroke();
            }

            PencilPreviewInkStroke.DrawingAttributes = inkDrawingAttributes;

            var strokeContainer = PencilStrokePreviewInkCanvas.InkPresenter.StrokeContainer;
            foreach (var stroke in strokeContainer.GetStrokes())
            {
                stroke.Selected = true;
            }

            strokeContainer.AddStroke(PencilPreviewInkStroke.Clone());
            strokeContainer.DeleteSelected();
        }

        private void PencilStrokeWidthSlider_PointerReleased(object sender, RoutedEventArgs e) =>
            FlyoutBase.GetAttachedFlyout(PencilButton)?.Hide();

        private void PencilStrokeWidthSlider_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                FlyoutBase.GetAttachedFlyout(PencilButton)?.Hide();
            }
        }

        private void EraserButton_Checked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(EraserButton, "ShowExtensionGlyph", false);

        private void EraserButton_Unchecked(object sender, RoutedEventArgs e) =>
            VisualStateManager.GoToState(EraserButton, "HideExtensionGlyph", false);

        private void OnOpenEraserButtonFlyout()
        {
            FlyoutBase.ShowAttachedFlyout(EraserButton);
            VisualStateManager.GoToState(EraserButton, "HideExtensionGlyph", false);
            var flyout = FlyoutBase.GetAttachedFlyout(EraserButton);
            flyout.Closed += EraserButtonFlyoutClosed;
        }

        private void EraserButtonFlyoutClosed(object sender, object e)
        {
            VisualStateManager.GoToState(EraserButton, "ShowExtensionGlyph", false);
            if (sender is FlyoutBase flyoutBase)
            {
                flyoutBase.Closed -= EraserButtonFlyoutClosed;
            }

            if (CurrentRadialSelection == ColoringBookRadialController.EraserTool)
            {
                CurrentRadialSelection = ColoringBookRadialController.InkingTool;
                EraserButton.Focus(FocusState.Keyboard);
            }
        }

        private void EraserFlyoutList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawingToolChanged?.Invoke(this,
                new DrawingToolChangedEventArgs { NewDrawingTool = DrawingTool });
            if (StrokeEraseListItem.FocusState != FocusState.Keyboard &&
                CellEraseListItem.FocusState != FocusState.Keyboard)
            {
                var flyout = FlyoutBase.GetAttachedFlyout(EraserButton);
                flyout?.Hide();
            }
        }

        private void EraserFlyoutList_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // Hide flyout when enter pressed on an eraser list item.
            if (e.OriginalKey == VirtualKey.Enter || e.OriginalKey == VirtualKey.Space)
            {
                FlyoutBase.GetAttachedFlyout(EraserButton)?.Hide();
            }
        }

        private static double[] PreviewStrokeCoordinates => Constants.PreviewStrokeCoordinates;

        private static Color StrokePreviewColor => Colors.Gray;

        private InkStroke PenPreviewInkStroke { get; set; }

        private InkStroke PencilPreviewInkStroke { get; set; }

        private PointerEventHandler PenWidthChangedHandler { get; set; }

        private PointerEventHandler PencilWidthChangedHandler { get; set; }

        private RadialController RadialController { get; set; }

        private ColoringBookRadialController CurrentRadialSelection { get; set; } =
            ColoringBookRadialController.None;

        private int SelectedToolIndex { get; set; } = 1;

        private AccessibilitySettings AccessibilitySettings { get; set; }
            = new AccessibilitySettings();

        private DisplayInformation DisplayInformation { get; set; }

        private int CurrentUiScaling { get; set; }

        private ContrastHandler ContrastHandlerAttached { get; set; } = ContrastHandler.None;

        private double InkScaling { get; set; } = 1.0;

        public DrawingTool DrawingTool
        {
            get => _drawingTool;
            private set
            {
                if (_drawingTool != value)
                {
                    _drawingTool = value;
                    OnDrawingToolChanged(value);
                }
            }
        }

        private DrawingTool _drawingTool = DrawingTool.Pen;

        public void OnFillCellButtonClicked()
        {
            DrawingTool = DrawingTool.FillCell;

            if (TargetInkCanvas != null)
            {
                TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                    InkInputProcessingMode.None;
            }
        }

        public void OnPenButtonClicked()
        {
            if (DrawingTool == DrawingTool.Pen)
            {
                OnOpenPenButtonFlyout();
            }
            else
            {
                DrawingTool = DrawingTool.Pen;

                if (TargetInkCanvas != null)
                {
                    var newAttributes = new InkDrawingAttributes();
                    var oldAttributes = TargetInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                    newAttributes.Color = oldAttributes.Color;
                    var strokeWidth = PenWidth * InkScaling;
                    newAttributes.Size = new Size(strokeWidth, strokeWidth);
                    TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(newAttributes);
                    TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                        InkInputProcessingMode.Inking;
                }
            }
        }

        public void OnPencilButtonClicked()
        {
            if (DrawingTool == DrawingTool.Pencil)
            {
                OnOpenPencilButtonFlyout();
            }
            else
            {
                DrawingTool = DrawingTool.Pencil;

                if (TargetInkCanvas != null)
                {
                    var newAttributes = InkDrawingAttributes.CreateForPencil();
                    var oldAttributes = TargetInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                    newAttributes.Color = oldAttributes.Color;
                    var strokeWidth = PencilWidth * InkScaling;
                    newAttributes.Size = new Size(strokeWidth, strokeWidth);
                    TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(newAttributes);
                    TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                        InkInputProcessingMode.Inking;
                }
            }
        }

        public void OnEraserButtonClicked()
        {
            if ((DrawingTool == DrawingTool.Eraser) ||
                (DrawingTool == DrawingTool.EraseCell))
            {
                OnOpenEraserButtonFlyout();
            }
            else
            {
                DrawingTool = IsStrokeEraseChecked ? DrawingTool.Eraser : DrawingTool.EraseCell;

                if (TargetInkCanvas != null)
                {
                    TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                        InkInputProcessingMode.None;
                }
            }
        }

        public int PenWidth
        {
            get => _penWidth;
            set
            {
                if (_penWidth != value)
                {
                    _penWidth = value;
                    UpdateDrawingWidth(value);
                }
            }
        }

        private int _penWidth = Constants.DefaultPenStrokeWidth;

        public int PencilWidth
        {
            get => _pencilWidth;
            set
            {
                if (_pencilWidth != value)
                {
                    _pencilWidth = value;
                    UpdateDrawingWidth(value);
                }
            }
        }

        private int _pencilWidth = Constants.DefaultPencilStrokeWidth;

        private void UpdateDrawingWidth(int width)
        {
            var currentDrawingAttributes = TargetInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            var newWidth = width * InkScaling;
            currentDrawingAttributes.Size = new Size(newWidth, newWidth);
            TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(currentDrawingAttributes);
        }

        public bool IsStrokeEraseChecked
        {
            get => _isStrokeEraseChecked;
            set
            {
                _isStrokeEraseChecked = value;
                if (value)
                {
                    DrawingTool = DrawingTool.Eraser;
                }
            }
        }

        private bool _isStrokeEraseChecked = true;

        public bool IsEraseCellChecked
        {
            get => _isEraseCellChecked;
            set
            {
                _isEraseCellChecked = value;
                if (value)
                {
                    DrawingTool = DrawingTool.EraseCell;
                }
            }
        }

        private bool _isEraseCellChecked;

        public void SetInkScaling(double inkScaling)
        {
            InkScaling = inkScaling;

            // Set initial drawing attributes.
            var newAttributes = new InkDrawingAttributes();
            var oldAttributes = TargetInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            newAttributes.Color = oldAttributes.Color;
            var strokeWidth = PenWidth * InkScaling;
            newAttributes.Size = new Size(strokeWidth, strokeWidth);
            TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(newAttributes);
            TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                InkInputProcessingMode.Inking;
        }

        private enum ContrastHandler
        {
            None,
            Black,
            White
        }

        private enum ColoringBookRadialController
        {
            None,
            InkingTool,
            Color,
            StrokeSize,
            UndoRedo,
            EraserTool,
            StrokeSizeDialClick
        }
    }
}
