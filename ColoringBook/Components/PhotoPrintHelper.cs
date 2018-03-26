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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Views;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Printing;

namespace ColoringBook.Components
{
    public class PhotoPrintHelper : PrintHelper
    {
        public PhotoPrintHelper(Page scenarioPage)
            : base(scenarioPage)
        {
        }

        /// <summary>
        /// Constant for 96 DPI.
        /// </summary>
        private const int DPI96 = 96;

        /// <summary>
        /// The app's number of photos.
        /// </summary>
        private const int NumberOfPhotos = 1;

        /// <summary>
        /// Gets or sets the current size setting for the image.
        /// </summary>
        /// <value>
        /// The current size setting for the image.
        /// </value>
        private PhotoSize PhotoSizeSetting { get; set; } = PhotoSize.SizeFullPage;

        /// <summary>
        /// Gets or sets the current scale settings for the image.
        /// </summary>
        /// <value>
        /// The current scale settings for the image.
        /// </value>
        private Scaling PhotoScale { get; set; } = Scaling.ShrinkToFit;

        /// <summary>
        /// Gets a map of UIElements used to store the print preview pages.
        /// </summary>
        /// <value>
        /// A map of UIElements used to store the print preview pages.
        /// </value>
        private Dictionary<int, UIElement> PageCollection { get; } = new Dictionary<int, UIElement>();

        /// <summary>
        /// Gets the synchronization object used to sync access to pageCollection and the visual root(PrintingRoot).
        /// </summary>
        /// <value>
        /// The synchronization object.</placeholder>
        /// </value>
        private static object PrintSync { get; } = new object();

        /// <summary>
        /// Gets or sets the current printer's page description used to create the content (size, margins, printable area).
        /// </summary>
        /// <value>
        /// The current printer's page description.
        /// </value>
        private PageDescription CurrentPageDescription { get; set; }

        /// <summary>
        /// Gets a request number used to describe a Paginate - GetPreviewPage session.
        /// </summary>
        /// <remarks>
        /// This value is used by GetPreviewPage to determine, before calling SetPreviewPage, if the page content is out of date.
        /// Flow:
        /// Paginate will increment the request count and all subsequent GetPreviewPage calls will store a local copy
        /// and verify it before calling SetPreviewPage.
        /// If another Paginate event is triggered while some GetPreviewPage workers are still executing asynchronously
        /// their results will be discarded (ignored) because their request number is expired (the photo page description changed).
        /// </remarks>
        /// <value>
        /// The request number.
        /// </value>
        private ref long RequestCount => ref _requestCount;

        private long _requestCount;

        /// <summary>
        /// This is the event handler for PrintManager.PrintTaskRequested.
        /// In order to ensure a good user experience, the system requires that the app handle
        /// the PrintTaskRequested event within the time specified
        /// by PrintTaskRequestedEventArgs->Request->Deadline.
        /// Therefore, we use this handler to only create the print task.
        /// The print settings customization can be done when the print document source is requested.
        /// </summary>
        /// <param name="sender">The print manager for which a print task request was made.</param>
        /// <param name="e">The print taks request associated arguments.</param>
        protected override void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;

            printTask = e.Request.CreatePrintTask(Tools.GetResourceString("Printer/PrintingTask"), sourceRequestedArgs =>
            {
                PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);

                // Choose the printer options to be shown.
                // The order in which the options are appended determines the order in which they appear in the UI.
                printDetailedOptions.DisplayedOptions.Clear();
                printDetailedOptions.DisplayedOptions.Add(StandardPrintTaskOptions.MediaSize);
                printDetailedOptions.DisplayedOptions.Add(StandardPrintTaskOptions.Copies);

                // Create a new list option.
                PrintCustomItemListOptionDetails photoSize = printDetailedOptions.CreateItemListOption(
                    "photoSize", Tools.GetResourceString("Printer/SizeHeading"));
                photoSize.AddItem("SizeFullPage", Tools.GetResourceString("Printer/SizeFullPage"));
                photoSize.AddItem("Size4x6", Tools.GetResourceString("Printer/Size4x6"));
                photoSize.AddItem("Size5x7", Tools.GetResourceString("Printer/Size5x7"));
                photoSize.AddItem("Size8x10", Tools.GetResourceString("Printer/Size8x10"));

                // Add the custom option to the option list.
                printDetailedOptions.DisplayedOptions.Add("photoSize");

                PrintCustomItemListOptionDetails scaling = printDetailedOptions.CreateItemListOption(
                    "scaling", Tools.GetResourceString("Printer/ScalingHeading"));
                scaling.AddItem("ShrinkToFit", Tools.GetResourceString("Printer/Shrink"));
                scaling.AddItem("Crop", Tools.GetResourceString("Printer/Crop"));

                // Add the custom option to the option list.
                printDetailedOptions.DisplayedOptions.Add("scaling");

                // Set default orientation to landscape.
                printTask.Options.Orientation = PrintOrientation.Landscape;

                // Register for print task option changed notifications.
                printDetailedOptions.OptionChanged += OnPrintDetailedOptionsOptionChanged;

                // Register for print task Completed notification.
                // Print Task event handler is invoked when the print job is completed.
                printTask.Completed += async (s, args) =>
                {
                    await ScenarioPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        ClearPageCollection();

                        // Reset image options to default values.
                        PhotoScale = Scaling.ShrinkToFit;
                        PhotoSizeSetting = PhotoSize.SizeFullPage;

                        // Reset the current page description.
                        CurrentPageDescription = null;

                        // Notify the user when the print operation fails.
                        if (args.Completion == PrintTaskCompletion.Failed)
                        {
                            ColoringPage.NotifyUserAsync(Tools.GetResourceString("Printer/ErrorMessage")).ContinueWithoutWaiting();
                        }
                    });
                };

                // Set the document source.
                sourceRequestedArgs.SetSource(PrintDocumentSource);
            });
        }

        /// <summary>
        /// Option change event handler
        /// </summary>
        /// <param name="sender">The print task option details for which an option changed.</param>
        /// <param name="args">The event arguments containing the id of the changed option.</param>
        private async void OnPrintDetailedOptionsOptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        {
            bool invalidatePreview = false;

            // For this scenario we are interested only when the 2 custom options change (photoSize & scaling)
            // in order to trigger a preview refresh.
            // Default options that change page aspect will trigger preview invalidation (refresh) automatically.
            // It is safe to ignore verifying other options and(or) combinations here because during Paginate event
            // (CreatePrintPreviewPages) we check if the PageDescription changed.
            if (args.OptionId == null)
            {
                return;
            }

            string optionId = args.OptionId.ToString();

            if (optionId == "photoSize")
            {
                IPrintOptionDetails photoSizeOption = sender.Options[optionId];
                string photoSizeValue = photoSizeOption.Value as string;

                if (!string.IsNullOrEmpty(photoSizeValue))
                {
                    switch (photoSizeValue)
                    {
                        case "SizeFullPage":
                            PhotoSizeSetting = PhotoSize.SizeFullPage;
                            break;
                        case "Size4x6":
                            PhotoSizeSetting = PhotoSize.Size4x6;
                            break;
                        case "Size5x7":
                            PhotoSizeSetting = PhotoSize.Size5x7;
                            break;
                        case "Size8x10":
                            PhotoSizeSetting = PhotoSize.Size8x10;
                            break;
                        default:
                            break;
                    }

                    invalidatePreview = true;
                }
            }

            if (optionId == "scaling")
            {
                IPrintOptionDetails scalingOption = sender.Options[optionId];
                string scalingValue = scalingOption.Value as string;

                if (!string.IsNullOrEmpty(scalingValue))
                {
                    switch (scalingValue)
                    {
                        case "Crop":
                            PhotoScale = Scaling.Crop;
                            break;
                        case "ShrinkToFit":
                            PhotoScale = Scaling.ShrinkToFit;
                            break;
                        default:
                            break;
                    }

                    invalidatePreview = true;
                }
            }

            // Invalidate preview if one of the 2 options (photoSize, scaling) changed.
            if (invalidatePreview)
            {
                await ScenarioPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    PrintDocument.InvalidatePreview);
            }
        }

        /// <summary>
        /// This is the event handler for Pagination.
        /// </summary>
        /// <param name="sender">The document for which pagination occurs.</param>
        /// <param name="e">The pagination event arguments containing the print options.</param>
        protected override void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            var printDoc = sender as PrintDocument;

            // A new "session" starts with each paginate event.
            Interlocked.Increment(ref RequestCount);

            PageDescription pageDescription = new PageDescription();

            // Get printer's page description.
            PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(e.PrintTaskOptions);
            PrintPageDescription printPageDescription = e.PrintTaskOptions.GetPageDescription(0);

            // Reset the error state.
            printDetailedOptions.Options["photoSize"].ErrorText = string.Empty;

            // Compute the printing page description (page size & center printable area).
            pageDescription.PageSize = printPageDescription.PageSize;

            pageDescription.Margin = new Size(
                Math.Max(printPageDescription.ImageableRect.Left,
                    printPageDescription.ImageableRect.Right - printPageDescription.PageSize.Width),
                Math.Max(printPageDescription.ImageableRect.Top,
                    printPageDescription.ImageableRect.Bottom - printPageDescription.PageSize.Height));

            pageDescription.ViewablePageSize = new Size(
                printPageDescription.PageSize.Width - (pageDescription.Margin.Width * 2),
                printPageDescription.PageSize.Height - (pageDescription.Margin.Height * 2));

            // Compute print photo area.
            switch (PhotoSizeSetting)
            {
                case PhotoSize.Size4x6:
                    pageDescription.PictureViewSize = new Size(4 * DPI96, 6 * DPI96);
                    break;
                case PhotoSize.Size5x7:
                    pageDescription.PictureViewSize = new Size(5 * DPI96, 7 * DPI96);
                    break;
                case PhotoSize.Size8x10:
                    pageDescription.PictureViewSize = new Size(8 * DPI96, 10 * DPI96);
                    break;
                case PhotoSize.SizeFullPage:
                    pageDescription.PictureViewSize = new Size(
                        pageDescription.ViewablePageSize.Width,
                        pageDescription.ViewablePageSize.Height);
                    break;
                default:
                    break;
            }

            // Try to maximize photo-size based on it's aspect-ratio.
            if ((pageDescription.ViewablePageSize.Width > pageDescription.ViewablePageSize.Height) && (PhotoSizeSetting != PhotoSize.SizeFullPage))
            {
                // Swap the Height and Width values.
                pageDescription.PictureViewSize = new Size(pageDescription.PictureViewSize.Height, pageDescription.PictureViewSize.Width);
            }

            pageDescription.IsContentCropped = PhotoScale == Scaling.Crop;

            // Recreate content only when:
            // - There is no current page description.
            // - The current page description doesn't match the new one.
            if (CurrentPageDescription == null || !CurrentPageDescription.Equals(pageDescription))
            {
                ClearPageCollection();

                if (pageDescription.PictureViewSize.Width > pageDescription.ViewablePageSize.Width ||
                    pageDescription.PictureViewSize.Height > pageDescription.ViewablePageSize.Height)
                {
                    printDetailedOptions.Options["photoSize"].ErrorText = Tools.GetResourceString("Printer/PhotoSizeError");

                    // Inform preview that it has only 1 page to show.
                    printDoc.SetPreviewPageCount(1, PreviewPageCountType.Intermediate);

                    // Add a custom "preview" unavailable page.
                    lock (PrintSync)
                    {
                        PageCollection[0] = new PreviewUnavailablePage(pageDescription.PageSize, pageDescription.ViewablePageSize);
                    }
                }
                else
                {
                    // Inform preview that is has #NumberOfPhotos pages to show.
                    printDoc.SetPreviewPageCount(NumberOfPhotos, PreviewPageCountType.Intermediate);
                }

                CurrentPageDescription = pageDescription;
            }
        }

        /// <summary>
        /// This is the event handler for PrintDocument.GetPrintPreviewPage. It provides a specific print page preview,
        /// in the form of an UIElement, to an instance of PrintDocument.
        /// PrintDocument subsequently converts the UIElement into a page that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">The print document.</param>
        /// <param name="e">Arguments containing the requested page preview.</param>
        protected async override void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            // Store a local copy of the request count to use later to determine if the computed page is out of date.
            // If the page preview is unavailable an async operation will generate the content.
            // When the operation completes there is a chance that a pagination request was already made
            // therefore making this page obsolete.
            // If the page is obsolete throw away the result (don't call SetPreviewPage) since a new
            // GetPrintPreviewPage will server that request.
            long requestNumber = 0;
            Interlocked.Exchange(ref requestNumber, RequestCount);
            int pageNumber = e.PageNumber;

            UIElement page;
            bool pageReady = false;

            // Try to get the page if it was previously generated.
            lock (PrintSync)
            {
                pageReady = PageCollection.TryGetValue(pageNumber - 1, out page);
            }

            if (!pageReady)
            {
                // The page is not available yet.
                page = await GeneratePageAsync(pageNumber, CurrentPageDescription);

                // If the ticket changed discard the result since the content is outdated.
                if (Interlocked.CompareExchange(ref requestNumber, requestNumber, RequestCount) != RequestCount)
                {
                    return;
                }

                // Store the page in the list in case an invalidate happens but the content doesn't need to be regenerated.
                lock (PrintSync)
                {
                    PageCollection[pageNumber - 1] = page;

                    // Add the newly created page to the printing root which is part of the visual tree and force it to go
                    // through layout so that the linked containers correctly distribute the content inside them.
                    PrintCanvas.Children.Add(page);
                    PrintCanvas.InvalidateMeasure();
                    PrintCanvas.UpdateLayout();
                }
            }

            // Send the page to preview.
            (sender as PrintDocument).SetPreviewPage(pageNumber, page);
        }

        /// <summary>
        /// This is the event handler for PrintDocument.AddPages. It provides all pages to be printed, in the form of
        /// UIElements, to an instance of PrintDocument. PrintDocument subsequently converts the UIElements
        /// into a pages that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">The print document.</param>
        /// <param name="e">Arguments containing the print task options.</param>
        protected override async void OnAddPrintPages(object sender, AddPagesEventArgs e)
        {
            var printDoc = sender as PrintDocument;

            // Loop over all of the preview pages.
            for (int i = 0; i < NumberOfPhotos; i++)
            {
                UIElement page = null;
                bool pageReady = false;

                lock (PrintSync)
                {
                    pageReady = PageCollection.TryGetValue(i, out page);
                }

                if (!pageReady)
                {
                    // If the page is not ready create a task that will generate its content.
                    page = await GeneratePageAsync(i + 1, CurrentPageDescription);
                }

                printDoc.AddPage(page);
            }

            // Indicate that all of the print pages have been provided.
            printDoc.AddPagesComplete();

            // Reset the current page description as soon as possible since the
            // PrintTask.Completed event might fire later (long running job).
            CurrentPageDescription = null;
        }

        /// <summary>
        /// Helper function that clears the page collection and also the pages attached to the "visual root".
        /// </summary>
        private void ClearPageCollection()
        {
            lock (PrintSync)
            {
                PageCollection.Clear();
                PrintCanvas.Children.Clear();
            }
        }

        /// <summary>
        /// Generic swap of 2 values.
        /// </summary>
        /// <typeparam name="T">typename</typeparam>
        /// <param name="v1">Value 1</param>
        /// <param name="v2">Value 2</param>
        private static void Swap<T>(ref T v1, ref T v2)
        {
            T swap = v1;
            v1 = v2;
            v2 = swap;
        }

        /// <summary>
        /// Generates a page containing a photo.
        /// The image will be rotated if detected that there is a gain from that regarding size (try to maximize photo size).
        /// </summary>
        /// <param name="photoNumber">The photo number.</param>
        /// <param name="pageDescription">The description of the printer page.</param>
        /// <returns>A task that will return the page.</returns>
        private async Task<UIElement> GeneratePageAsync(int photoNumber, PageDescription pageDescription)
        {
            Canvas page = new Canvas
            {
                Width = pageDescription.PageSize.Width,
                Height = pageDescription.PageSize.Height
            };

            Canvas viewablePage = new Canvas()
            {
                Width = pageDescription.ViewablePageSize.Width,
                Height = pageDescription.ViewablePageSize.Height
            };

            viewablePage.SetValue(Canvas.LeftProperty, pageDescription.Margin.Width);
            viewablePage.SetValue(Canvas.TopProperty, pageDescription.Margin.Height);

            // The image "frame" which also acts as a viewport.
            Grid photoView = new Grid
            {
                Width = pageDescription.PictureViewSize.Width,
                Height = pageDescription.PictureViewSize.Height
            };

            // Center the frame.
            photoView.SetValue(Canvas.LeftProperty, (viewablePage.Width - photoView.Width) / 2);
            photoView.SetValue(Canvas.TopProperty, (viewablePage.Height - photoView.Height) / 2);

            // Return an async task that will complete when the image is fully loaded.
            WriteableBitmap bitmap = new WriteableBitmap(Export.ExportWidth, Export.ExportHeight);
            System.IO.Stream bitmapStream = null;
            bitmapStream = bitmap.PixelBuffer.AsStream();
            bitmapStream.Position = 0;

            await bitmapStream.WriteAsync(Export.Pixels.ToArray(), 0, 4 * Export.ExportHeight * Export.ExportWidth);
            bitmapStream.Flush();

            if (bitmap != null)
            {
                Image image = new Image
                {
                    Source = bitmap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Use the real image size when croping or if the image is smaller then the target area (prevent a scale-up).
                if (PhotoScale == Scaling.Crop ||
                    (bitmap.PixelWidth <= pageDescription.PictureViewSize.Width &&
                    bitmap.PixelHeight <= pageDescription.PictureViewSize.Height))
                {
                    image.Stretch = Stretch.None;
                    image.Width = bitmap.PixelWidth;
                    image.Height = bitmap.PixelHeight;
                }

                // Add the newly created image to the visual root.
                photoView.Children.Add(image);
                viewablePage.Children.Add(photoView);
                page.Children.Add(viewablePage);
            }

            // Return the page with the image centered.
            return page;
        }

        /// <summary>
        /// Loads an image from an uri source and performs a rotation based on the print target aspect.
        /// </summary>
        /// <param name="source">The location of the image.</param>
        /// <param name="landscape">A flag that indicates if the target (printer page) is in landscape mode.</param>
        /// <returns>A task that will return the loaded bitmap.</returns>
        private async Task<WriteableBitmap> LoadBitmapAsync(Uri source, bool landscape)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(source);
            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                BitmapTransform transform = new BitmapTransform
                {
                    Rotation = BitmapRotation.None
                };
                uint width = decoder.PixelWidth;
                uint height = decoder.PixelHeight;

                if (landscape && width < height)
                {
                    transform.Rotation = BitmapRotation.Clockwise270Degrees;
                    Swap(ref width, ref height);
                }
                else if (!landscape && width > height)
                {
                    transform.Rotation = BitmapRotation.Clockwise90Degrees;
                    Swap(ref width, ref height);
                }

                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,    // WriteableBitmap uses BGRA format.
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,    // This sample ignores Exif orientation.
                    ColorManagementMode.DoNotColorManage);

                WriteableBitmap bitmap = new WriteableBitmap((int)width, (int)height);
                var pixelBuffer = pixelData.DetachPixelData();
                using (var pixelStream = bitmap.PixelBuffer.AsStream())
                {
                    pixelStream.Write(pixelBuffer, 0, (int)pixelStream.Length);
                }

                return bitmap;
            }
        }

        /// <summary>
        /// Photo size options.
        /// </summary>
        private enum PhotoSize : byte
        {
            SizeFullPage,
            Size4x6,
            Size5x7,
            Size8x10
        }

        /// <summary>
        /// Photo scaling options.
        /// </summary>
        private enum Scaling : byte
        {
            ShrinkToFit,
            Crop
        }

        private class PageDescription : IEquatable<PageDescription>
        {
            public Size Margin { get; set; }

            public Size PageSize { get; set; }

            public Size ViewablePageSize { get; set; }

            public Size PictureViewSize { get; set; }

            public bool IsContentCropped { get; set; }

            public bool Equals(PageDescription other)
            {
                // Detect whether PageSize changed.
                bool equal = (Math.Abs(PageSize.Width - other.PageSize.Width) < double.Epsilon) &&
                    (Math.Abs(PageSize.Height - other.PageSize.Height) < double.Epsilon);

                // Detect whether ViewablePageSize changed.
                if (equal)
                {
                    equal = (Math.Abs(ViewablePageSize.Width - other.ViewablePageSize.Width) < double.Epsilon) &&
                        (Math.Abs(ViewablePageSize.Height - other.ViewablePageSize.Height) < double.Epsilon);
                }

                // Detect whether PictureViewSize changed.
                if (equal)
                {
                    equal = (Math.Abs(PictureViewSize.Width - other.PictureViewSize.Width) < double.Epsilon) &&
                        (Math.Abs(PictureViewSize.Height - other.PictureViewSize.Height) < double.Epsilon);
                }

                // Detect whether cropping changed.
                if (equal)
                {
                    equal = IsContentCropped == other.IsContentCropped;
                }

                return equal;
            }
        }
    }
}
