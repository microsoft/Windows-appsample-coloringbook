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
using Windows.UI.Input;

namespace ColoringBook.Components
{
    public class RadialControlHelper
    {
        public ColorPaletteViewModel ColorPaletteViewModel { get; set; }

        public event EventHandler UndoOperation;

        public event EventHandler RedoOperation;

        private RadialController RadialController { get; set; }

        private ColoringBookRadialController CurrentSelection { get; set; }
            = ColoringBookRadialController.None;

        public void InitializeForColoringPage()
        {
            InitializeRadialController();

            // Set new system defaults.
            var config = RadialControllerConfiguration.GetForCurrentView();
            config.SetDefaultMenuItems(new[]
            {
                RadialControllerSystemMenuItemKind.Volume,
            });

            RadialControllerMenuItem colorRadialMenuItem = RadialControllerMenuItem.CreateFromKnownIcon(
                Tools.GetResourceString("RadialDial/ColorPicker"), RadialControllerMenuKnownIcon.InkColor);
            RadialControllerMenuItem undoRedoRadialMenuItem = RadialControllerMenuItem.CreateFromKnownIcon(
                "UndoRedo", RadialControllerMenuKnownIcon.UndoRedo);

            colorRadialMenuItem.Invoked += (s, e) => CurrentSelection = ColoringBookRadialController.Color;
            undoRedoRadialMenuItem.Invoked += (s, e) => CurrentSelection =
                ColoringBookRadialController.UndoRedo;

            AddMenuItem(colorRadialMenuItem);
            AddMenuItem(undoRedoRadialMenuItem);
        }

        private void InitializeRadialController()
        {
            RadialController = RadialController.CreateForCurrentView();
            RadialController.RotationResolutionInDegrees = 15;

            // Clear any custom items.
            RadialController.Menu.Items.Clear();

            RadialController.RotationChanged += RadialController_RotationChanged;
        }

        private void RadialController_RotationChanged(RadialController sender,
            RadialControllerRotationChangedEventArgs args)
        {
            if (CurrentSelection == ColoringBookRadialController.Color)
            {
                if (args.RotationDeltaInDegrees > 0)
                {
                    ColorPaletteViewModel.SelectNextItem();
                }
                else
                {
                    ColorPaletteViewModel.SelectPreviousItem();
                }
            }
            else if (CurrentSelection == ColoringBookRadialController.UndoRedo)
            {
                if (args.RotationDeltaInDegrees > 0)
                {
                    RedoOperation?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    UndoOperation?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Unregister()
        {
            if (RadialController != null)
            {
                RadialController.Menu.Items.Clear();
                RadialController.RotationChanged -= RadialController_RotationChanged;
            }

            RadialControllerConfiguration.GetForCurrentView().ResetToDefaultMenuItems();
            UndoOperation = null;
            RedoOperation = null;
        }

        public void AddMenuItem(RadialControllerMenuItem item)
        {
            if (RadialController != null && !RadialController.Menu.Items.Contains(item))
            {
                RadialController.Menu.Items.Add(item);
            }
        }

        public void RemoveMenuItem(RadialControllerMenuItem item)
        {
            if (RadialController != null && RadialController.Menu.Items.Contains(item))
            {
                RadialController.Menu.Items.Remove(item);
            }
        }

        private enum ColoringBookRadialController
        {
            None,
            InkingTool,
            Color,
            StrokeSize,
            UndoRedo
        }
    }
}
