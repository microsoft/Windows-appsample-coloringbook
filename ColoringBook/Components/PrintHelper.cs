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
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Models;
using ColoringBook.Views;
using Windows.Graphics.Printing;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Printing;

namespace ColoringBook.Components
{
    public class PrintHelper
    {
        public PrintHelper(Page scenarioPage)
        {
            ScenarioPage = scenarioPage;
        }

        public ColoringBookExport Export { get; set; }

        /// <summary>
        /// Gets or sets a PrintDocument that is used to prepare the pages for printing.
        /// Prepare the pages to print in the handlers for the Paginate, GetPreviewPage, and AddPages events.
        /// </summary>
        /// <value>
        /// A PrintDocument that is used to prepare the pages for printing.
        /// </value>
        protected PrintDocument PrintDocument { get; set; }

        /// <summary>
        /// Gets or sets a marker interface for the document source.
        /// </summary>
        /// <value>
        /// The marker interface for the document source.
        /// </value>
        protected IPrintDocumentSource PrintDocumentSource { get; set; }

        /// <summary>
        /// Gets a list of the UIElements used to store the print preview pages.  This gives easy access to any desired preview page.
        /// </summary>
        /// <value>
        /// The list of UIElements.
        /// </value>
        private List<UIElement> PrintPreviewPages { get; } = new List<UIElement>();

        // Event callback which is called after print preview pages are generated.
        // Photos scenario uses this to do filtering of preview pages
        protected event EventHandler PreviewPagesCreated;

        /// <summary>
        /// Gets or sets the first page in the printing-content series.
        /// From this "virtual sized" paged content is split(text is flowing) to "printing pages".
        /// </summary>
        /// <value>
        /// The first page in the printing-content series.
        /// </value>
        private FrameworkElement FirstPage { get; set; }

        /// <summary>
        /// Gets or sets a reference back to the scenario page used to access XAML elements on the scenario page.
        /// </summary>
        /// <value>
        /// A reference back to the scenario page used to access XAML elements on the scenario page.
        /// </value>
        protected Page ScenarioPage { get; set; }

        /// <summary>
        /// Gets a hidden canvas used to hold pages we wish to print.
        /// </summary>
        /// <value>
        /// The hidden canvas.
        /// </value>
        protected Canvas PrintCanvas => ScenarioPage.FindName("PrintCanvas") as Canvas;

        /// <summary>
        /// This function registers the app for printing with Windows and
        /// sets up the necessary event handlers for the print process.
        /// </summary>
        public virtual void RegisterForPrinting()
        {
            PrintDocument = new PrintDocument();
            PrintDocumentSource = PrintDocument.DocumentSource;
            PrintDocument.Paginate += CreatePrintPreviewPages;
            PrintDocument.GetPreviewPage += GetPrintPreviewPage;
            PrintDocument.AddPages += OnAddPrintPages;

            PrintManager printManager = PrintManager.GetForCurrentView();
            printManager.PrintTaskRequested += PrintTaskRequested;
        }

        /// <summary>
        /// This function unregisters the app for printing with Windows.
        /// </summary>
        public virtual void UnregisterForPrinting()
        {
            if (PrintDocument == null)
            {
                return;
            }

            PrintDocument.Paginate -= CreatePrintPreviewPages;
            PrintDocument.GetPreviewPage -= GetPrintPreviewPage;
            PrintDocument.AddPages -= OnAddPrintPages;

            // Remove the handler for printing initialization.
            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested -= PrintTaskRequested;

            PrintCanvas.Children.Clear();
            ScenarioPage = null;
        }

        public static async Task ShowPrintUIAsync()
        {
            // Catch and print out any errors reported.
            try
            {
                await PrintManager.ShowPrintUIAsync();
            }
            catch (Exception e)
            {
                ColoringPage.NotifyUserAsync(Tools.GetResourceString("Printer/ErrorMessage:" + e)).ContinueWithoutWaiting();
            }
        }

        /// <summary>
        /// Method that will generate print content for the scenario.
        /// For scenarios 1-4: it will create the first page from which content will flow.
        /// Scenario 5 uses a different approach.
        /// </summary>
        /// <param name="page">The page to print</param>
        public virtual void PreparePrintContent(Page page)
        {
            if (FirstPage == null)
            {
                FirstPage = page;
                (FirstPage.FindName("Header") as StackPanel).Visibility = Visibility.Visible;
            }

            // Add the (newly created) page to the print canvas which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            PrintCanvas.Children.Add(FirstPage);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();
        }

        /// <summary>
        /// This is the event handler for PrintManager.PrintTaskRequested.
        /// </summary>
        /// <param name="sender">PrintManager</param>
        /// <param name="e">PrintTaskRequestedEventArgs</param>
        protected virtual void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;
            printTask = e.Request.CreatePrintTask("C# Printing SDK Sample", sourceRequested =>
            {
                // Print Task event handler is invoked when the print job is completed.
                printTask.Completed += async (s, args) =>
                {
                    // Notify the user when the print operation fails.
                    if (args.Completion == PrintTaskCompletion.Failed)
                    {
                        await ScenarioPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ColoringPage.NotifyUserAsync(Tools.GetResourceString("Printer/ErrorMessage")).ContinueWithoutWaiting();
                        });
                    }
                };

                sourceRequested.SetSource(PrintDocumentSource);
            });
        }

        /// <summary>
        /// This is the event handler for PrintDocument.Paginate. It creates print preview pages for the app.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Paginate Event Arguments</param>
        protected virtual void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            // Clear the cache of preview pages.
            PrintPreviewPages.Clear();

            // Clear the print canvas of preview pages.
            PrintCanvas.Children.Clear();

            // This variable keeps track of the last RichTextBlockOverflow element that was added to a page which will be printed.
            RichTextBlockOverflow lastRTBOOnPage;

            // Get the PrintTaskOptions.
            PrintTaskOptions printingOptions = e.PrintTaskOptions;

            // Get the page description to deterimine how big the page is.
            PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);

            // We know there is at least one page to be printed. passing null as the first parameter to
            // AddOnePrintPreviewPage tells the function to add the first page.
            lastRTBOOnPage = AddOnePrintPreviewPage(null, pageDescription);

            // We know there are more pages to be added as long as the last RichTextBoxOverflow added to a print preview
            // page has extra content.
            while (lastRTBOOnPage.HasOverflowContent && lastRTBOOnPage.Visibility == Visibility.Visible)
            {
                lastRTBOOnPage = AddOnePrintPreviewPage(lastRTBOOnPage, pageDescription);
            }

            PreviewPagesCreated?.Invoke(PrintPreviewPages, EventArgs.Empty);

            PrintDocument printDoc = sender as PrintDocument;

            // Report the number of preview pages created.
            printDoc.SetPreviewPageCount(PrintPreviewPages.Count, PreviewPageCountType.Intermediate);
        }

        /// <summary>
        /// This is the event handler for PrintDocument.GetPrintPreviewPage. It provides a specific print preview page,
        /// in the form of an UIElement, to an instance of PrintDocument. PrintDocument subsequently converts the UIElement
        /// into a page that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Arguments containing the preview requested page.</param>
        protected virtual void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e) =>
            (sender as PrintDocument).SetPreviewPage(e.PageNumber, PrintPreviewPages[e.PageNumber - 1]);

        /// <summary>
        /// This is the event handler for PrintDocument.AddPages. It provides all pages to be printed, in the form of
        /// UIElements, to an instance of PrintDocument. PrintDocument subsequently converts the UIElements
        /// into a pages that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Add page event arguments containing a print task options reference.</param>
        protected virtual void OnAddPrintPages(object sender, AddPagesEventArgs e)
        {
            // Loop over all of the preview pages and add each one to  add each page to be printed.
            for (int i = 0; i < PrintPreviewPages.Count; i++)
            {
                // We should have all pages ready at this point.
                PrintDocument.AddPage(PrintPreviewPages[i]);
            }

            // Indicate that all of the print pages have been provided.
            (sender as PrintDocument).AddPagesComplete();
        }

        /// <summary>
        /// This function creates and adds one print preview page to the internal cache of print preview
        /// pages stored in printPreviewPages.
        /// </summary>
        /// <param name="lastRTBOAdded">Last RichTextBlockOverflow element added in the current content.</param>
        /// <param name="printPageDescription">Printer's page description</param>
        /// <returns>The link container for text overflowing in this page.</returns>
        protected virtual RichTextBlockOverflow AddOnePrintPreviewPage(RichTextBlockOverflow lastRTBOAdded, PrintPageDescription printPageDescription)
        {
            // XAML element that is used to represent to "printing page".
            FrameworkElement page = FirstPage;

            // The link container for text overflowing in this page.
            RichTextBlockOverflow textLink;

            // Check if this is the first page ( no previous RichTextBlockOverflow).
            if (lastRTBOAdded == null)
            {
                // Hide footer since we don't know yet if it will be displayed (this might not be the last page) - wait for layout.
                (page.FindName("Footer") as StackPanel).Visibility = Visibility.Collapsed;
            }

            // Set "paper" width.
            page.Width = printPageDescription.PageSize.Width;
            page.Height = printPageDescription.PageSize.Height;

            var printableArea = page.FindName("PrintableArea") as Grid;

            // Get the margins size.
            // If the ImageableRect is smaller than the app provided margins use the ImageableRect.
            double marginWidth = 0;
            double marginHeight = 0;

            // Set-up "printable area" on the "paper".
            printableArea.Width = FirstPage.Width - marginWidth;
            printableArea.Height = FirstPage.Height - marginHeight;

            // Add the (newley created) page to the print canvas which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            PrintCanvas.Children.Add(page);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();

            // Find the last text container and see if the content is overflowing.
            textLink = page.FindName("ContinuationPageLinkedContainer") as RichTextBlockOverflow;

            // Check if this is the last page.
            if (!textLink.HasOverflowContent && textLink.Visibility == Visibility.Visible)
            {
                (page.FindName("Footer") as StackPanel).Visibility = Visibility.Visible;
            }

            // Add the page to the page preview collection.
            PrintPreviewPages.Add(page);

            return textLink;
        }
    }
}
