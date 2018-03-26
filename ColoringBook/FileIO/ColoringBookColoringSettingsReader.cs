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
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using ColoringBook.Common;
using ColoringBook.Models;
using Windows.Storage;

namespace ColoringBook.FileIO
{
    public class ColoringBookColoringSettingsReader : ColoringBookFileReader<ColoringBookColoringSettings, Stream>
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

        protected override ColoringBookColoringSettings ReadFromFile(Stream stream)
        {
            var settings = new ColoringBookColoringSettings();

            var xmlreadersettings = new XmlReaderSettings { Async = true };

            using (var reader = XmlReader.Create(stream, xmlreadersettings))
            {
                // Read xml beginning.
                reader.Read();

                reader.ReadStartElement("ColoringBookColoring");

                reader.ReadStartElement("Name");
                settings.ColoringName = reader.ReadContentAsString();
                reader.ReadEndElement();

                // Read last opened; upon error, set to default.
                reader.ReadStartElement("LastOpened");
                try
                {
                    settings.LastOpened = DateTime.Parse(reader.ReadContentAsString());
                }
                catch
                {
                    settings.LastOpened = DateTime.Now;
                }

                reader.ReadEndElement();

                // Read image name and location.
                reader.ReadStartElement("ImageName");
                settings.ColoringImageName = reader.ReadContentAsString();
                reader.ReadEndElement();

                try
                {
                    reader.ReadStartElement("ColoringResolutionType");
                    settings.ColoringResolutionType =
                        (DeviceResolutionType)Enum.Parse(typeof(DeviceResolutionType), reader.ReadContentAsString());
                    reader.ReadEndElement();
                }
                catch
                {
                    settings.ColoringResolutionType = DeviceResolutionType.Regular;
                }
            }

            return settings;
        }
    }
}