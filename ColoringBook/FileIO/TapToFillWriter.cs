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
    public class TapToFillWriter : ColoringBookFileWriter<Dictionary<uint, Color>, BinaryWriter>
    {
        protected override async Task<BinaryWriter> GetStreamAsync(StorageFile file)
        {
            try
            {
                return new BinaryWriter(await file.OpenStreamForWriteAsync());
            }
            catch
            {
                return null;
            }
        }

        protected override Task WriteToFile(BinaryWriter bw, Dictionary<uint, Color> data)
        {
            foreach (var item in data)
            {
                bw.Write(item.Key);
                bw.Write(item.Value.B);
                bw.Write(item.Value.G);
                bw.Write(item.Value.R);
                bw.Write(item.Value.A);
            }

            bw.Flush();
            bw.Dispose();

            return Task.CompletedTask;
        }
    }
}