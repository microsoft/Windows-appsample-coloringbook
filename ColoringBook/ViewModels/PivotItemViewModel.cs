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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Models;
using ColoringBook.Views;
using Windows.UI.Xaml.Controls;

namespace ColoringBook.ViewModels
{
    public class PivotItemViewModel : Observable
    {
        public PivotItemViewModel(PageIndexEnum currentPivot)
        {
            CurrentPivot = currentPivot;
            ThumbnailsSource.CollectionChanged += ThumbnailsSource_CollectionChanged;
        }

        public event EventHandler UpdateHomeCommandBar;

        public event EventHandler<PageIndexChangedEventArgs> UpdateHomePivotIndex;

        public void OnUpdateHomePivotIndex(PageIndexEnum index) => UpdateHomePivotIndex?.Invoke(this,
            new PageIndexChangedEventArgs { NewIndex = index });

        private PageIndexEnum CurrentPivot { get; }

        public bool DisplayEmptyArtworkScreen => (CurrentPivot == PageIndexEnum.MyArtworkIndex) && (ThumbnailsSource?.Count <= 0);

        public ObservableCollection<Thumbnail> ThumbnailsSource { get; }
            = new ObservableCollection<Thumbnail>();

        public IList<Thumbnail> SelectedThumbnails
        {
            get => _selectedThumbnails;
            set => Set(ref _selectedThumbnails, value);
        }

        private IList<Thumbnail> _selectedThumbnails;

        public async Task DeleteThumbnailAsync(MyArtworkThumbnail thumbnail)
        {
            await thumbnail.Coloring.DeleteColoringAsync();
            ThumbnailsSource.Remove(thumbnail);
        }

        public async Task DeleteSelectedItemsAsync()
        {
            foreach (var item in SelectedThumbnails)
            {
                if (!(item is MyArtworkThumbnail thumbnail))
                {
                    continue;
                }

                await DeleteThumbnailAsync(thumbnail);
            }

            UpdateHeroThumbnail();
        }

        public virtual void ThumbnailGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentPivot == PageIndexEnum.MyArtworkIndex)
            {
                UpdateHomeCommandBar?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ThumbnailsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Thumbnail item in e.NewItems)
                {
                    item.PropertyChanged += OnThumbnailPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (Thumbnail item in e.OldItems)
                {
                    item.PropertyChanged -= OnThumbnailPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(DisplayEmptyArtworkScreen));
            UpdateHomeCommandBar?.Invoke(this, EventArgs.Empty);
        }

        private async void OnThumbnailPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var thumbnail = sender as Thumbnail;

            if (thumbnail.MarkForDelete)
            {
                if (thumbnail is MyArtworkThumbnail artworkThumbnail)
                {
                    await DeleteThumbnailAsync(artworkThumbnail);
                    UpdateHeroThumbnail();
                }

                return;
            }

            // Removing and re-adding the thumbnail to force the Observable Collection to visually reflect changes.
            var index = ThumbnailsSource.IndexOf(thumbnail);
            ThumbnailsSource.Remove(thumbnail);

            // Only show Library thumbnail if there was no error.
            if (thumbnail != null && !(CurrentPivot == PageIndexEnum.LibraryIndex && thumbnail.HadError))
            {
                ThumbnailsSource.Insert(index, thumbnail);
            }

            UpdateHeroThumbnail();
        }

        private void UpdateHeroThumbnail()
        {
            // Update thumbnail as hero if necessary.
            if (CurrentPivot == PageIndexEnum.MyArtworkIndex && ThumbnailsSource?.Count > 0)
            {
                var first = ThumbnailsSource.First();

                if (first != null && !first.IsHero)
                {
                    first.IsHero = true;
                }
            }
        }
    }
}
