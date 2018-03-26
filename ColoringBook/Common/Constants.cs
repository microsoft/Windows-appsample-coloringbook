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

using Windows.Foundation;
using Windows.UI;

namespace ColoringBook.Common
{
    public static class Constants
    {
        public const float InitialDisplayDpi = 96f;
        public const int ShortDelay = 100; // in milliseconds
        public const int MinScreenWidthBeforeToolbarOverlap = 530;
        public const double IconDisabledOpacity = 0.3;
        public const double MinimumManufacturedStrokeLength = 2.0;
        public static readonly Color ClearColor = Colors.Transparent;
        public static readonly Size ThumbnailSize = new Size(223, 126);
        public static readonly Size HeroThumbnailSize = new Size(446, 252);
        public const int UndoRedoTransactionCapacity = 100;
        public const int DefaultPenStrokeWidth = 3;
        public const int DefaultPencilStrokeWidth = 3;
        public static readonly double[] PreviewStrokeCoordinates =
        {
            24.0, 28.5, 26.175271, 26.452467, 28.468156, 24.57762, 30.860594, 22.862812, 33.253032, 21.148004,
            35.745021, 19.593234, 38.3185, 18.185855, 40.891979, 16.778476, 43.546946, 15.518487, 46.26534, 14.39324,
            48.983733, 13.267993, 51.765552, 12.277489, 54.592735, 11.409078, 57.419917, 10.540668, 60.292463, 9.794352,
            63.192308, 9.1574822, 66.092153, 8.5206123, 69.019299, 7.9931887, 71.955681, 7.5625631, 74.892063, 7.1319376,
            77.837682, 6.7981102, 80.774476, 6.5484328, 83.711269, 6.2987555, 86.639236, 6.1332282, 89.540314, 6.0392029,
            92.12168, 5.9271775, 94.691318, 5.9313799, 97.248453, 6.040089, 99.805587, 6.1487981, 102.35022, 6.3620141,
            104.88157, 6.668016, 107.41293, 6.974018, 109.931, 7.3728059, 112.43502, 7.852659, 114.93904, 8.332512,
            117.42901, 8.8934302, 119.90414, 9.5236926, 122.37928, 10.153955, 124.83959, 10.853562, 127.28429, 11.610792,
            129.72899, 12.368022, 132.15809, 13.182876, 134.57081, 14.043632, 136.98352, 14.904388, 139.37986, 15.811047,
            141.75904, 16.751888, 144.13822, 17.692728, 146.50025, 18.667751, 148.84434, 19.665234, 150.92055, 20.558561,
            153.02009, 21.393996, 155.13997, 22.171709, 157.25986, 22.949423, 159.40009, 23.669414, 161.55771, 24.331854,
            163.71533, 24.994294, 165.89033, 25.599182, 168.07973, 26.146689, 170.26914, 26.694195, 172.47295, 27.184321,
            174.6882, 27.617235, 176.90346, 28.050149, 179.13015, 28.425851, 181.3653, 28.744513, 183.60046, 29.063175,
            185.84408, 29.324795, 188.0932, 29.529545, 190.34231, 29.734295, 192.59692, 29.882174, 194.85406, 29.973352,
            197.11119, 30.06453, 199.37085, 30.099008, 201.63006, 30.076955, 203.88927, 30.054901, 206.14803, 29.976318,
            208.40338, 29.841374, 210.65872, 29.70643, 212.91064, 29.515126, 215.15617, 29.267631, 219.64724, 28.772643,
            224.11274, 28.052893, 228.52891, 27.109745, 230.73699, 26.638171, 232.93274, 26.110747, 235.11319, 25.527643,
            237.29363, 24.94454, 239.45877, 24.305756, 241.60564, 23.611464, 243.7525, 22.917171, 245.88109, 22.167369,
            247.98843, 21.362228, 250.09577, 20.557087, 252.18187, 19.696606, 254.24374, 18.780957, 257.44071, 17.278777,
            260.5682, 15.598548, 263.55223, 13.698859, 265.04425, 12.749015, 266.50041, 11.744305, 267.91145, 10.679554,
            269.32249, 9.6148034, 270.68842, 8.4900108, 272.0, 7.3
        };
    }
}
