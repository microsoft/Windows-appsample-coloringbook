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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ColoringBook.ViewModels;
using Windows.Storage;

namespace ColoringBook.FileIO
{
    public class ColorSettingsReader : ColoringBookFileReader<ColorPaletteState, Stream>
    {
        protected override async Task<Stream> GetStreamAsync(StorageFile file)
        {
            try
            {
                var readStream = await file.OpenAsync(FileAccessMode.Read);
                var stream = readStream.AsStreamForRead();
                return stream;
            }
            catch
            {
                return null;
            }
        }

        protected override ColorPaletteState ReadFromFile(Stream stream)
        {
            var settings = new XmlReaderSettings();

            using (var reader = XmlReader.Create(stream, settings))
            {
                try
                {
                    var paletteState = default(ColorPaletteState);

                    reader.Read();
                    reader.ReadStartElement("ColorSettings");

                    reader.ReadStartElement("ColorsCount");
                    var colorsCount = Convert.ToInt32(reader.ReadContentAsString());
                    reader.ReadEndElement();

                    paletteState.Colors = new int?[colorsCount];

                    reader.ReadStartElement("Colors");
                    for (int i = 0; i < colorsCount; i++)
                    {
                        reader.ReadStartElement("Color");
                        var colorStr = reader.ReadContentAsString();
                        if (colorStr == "null")
                        {
                            paletteState.Colors[i] = null;
                        }
                        else
                        {
                            paletteState.Colors[i] = Convert.ToInt32(colorStr);
                        }

                        reader.ReadEndElement();
                    }

                    reader.ReadEndElement();

                    reader.ReadStartElement("SelectedIndex");
                    paletteState.SelectedIndex = Convert.ToInt32(reader.ReadContentAsString());
                    reader.ReadEndElement();

                    return paletteState;
                }
                catch (XmlException)
                {
                    Debug.WriteLine("ColorPaletteState.ReadFromFile() XmlException thrown");

                    return default(ColorPaletteState);
                }
            }
        }
    }
}