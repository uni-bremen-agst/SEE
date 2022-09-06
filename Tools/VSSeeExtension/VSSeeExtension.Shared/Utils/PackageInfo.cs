// Copyright © 2022 Jan-Philipp Schramm
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace VSSeeExtension.Utils
{
    /// <summary>
    /// Stores all configuration options.
    /// </summary>
    public static partial class PackageInfo
    {
        public const string MainPackageString = "d4d88e38-8a1b-49b1-a4fd-202a542a8afc";
        public const string VSSeePackageCommandString = "f6a899b2-aab0-46ef-8980-f0e38ccc0dbf";

        public static readonly Guid MainPackage = new(MainPackageString);
        public static readonly Guid VSSeePackageCommandSet = new(VSSeePackageCommandString);

        public static readonly int HighlightClassCommandId = 0x0100;
        public static readonly int HighlightClassesCommandId = 0x0101;
        public static readonly int HighlightMethodCommandId = 0x0102;
        public static readonly int HighlightMethodReferencesCommandId = 0x0103;

        public static readonly int ConnectToSeeCommandId = 0x0110;
        public static readonly int DisconnectFromSeeCommandId = 0x0111;

        public static readonly int AboutCommandId = 0x0120;
    }
}
