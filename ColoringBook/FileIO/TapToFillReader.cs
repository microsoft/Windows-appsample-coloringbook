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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

namespace ColoringBook.FileIO
{
    public class TapToFillReader : ColoringBookFileReader<Dictionary<uint, Color>, BinaryReader>
    {
        protected override async Task<BinaryReader> GetStreamAsync(StorageFile file)
        {
            try
            {
                return new BinaryReader(await file.OpenStreamForReadAsync());
            }
            catch
            {
                return null;
            }
        }

        protected override Dictionary<uint, Color> ReadFromFile(BinaryReader reader)
        {
            var ttfCache = new Dictionary<uint, Color>();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var cellId = reader.ReadUInt32();

                var color = new Color
                {
                    B = reader.ReadByte(),
                    G = reader.ReadByte(),
                    R = reader.ReadByte(),
                    A = reader.ReadByte()
                };

                ttfCache.Add(cellId, color);
            }

            return ttfCache;
        }
    }
}