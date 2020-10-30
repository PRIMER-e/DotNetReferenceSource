//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
// 
// Description: Utility class for handling various Per Monitor DPI related
// functions
//---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Interop;

namespace MS.Internal.PresentationCore
{
    internal static class DpiUtil
    {
        /// <summary>
        /// flag1 and flag2 correspond to MonitorDpiScaleIndex1 and MonitorDpiScaleIndex2 of VisualFlags respectively
        /// UpdateDpiScales sets flag1 and flag2 based on the correct index of the DPI in the static DPI array stored in
        /// UIElement class and inserts an entry into the array if necessary.
        /// </summary>
        internal static DpiFlags UpdateDpiScalesAndGetIndex(double pixelsPerInchX, double pixelsPerInchY)
        {
            lock (UIElement.DpiLock)
            {
                bool dpiScaleFlag1, dpiScaleFlag2;
                int index = 0;
                int sizeOfList = UIElement.DpiScaleXValues.Count;
                for (index = 0; index < sizeOfList; index++)
                {
                    // We have found a match.
                    if (UIElement.DpiScaleXValues[index] == pixelsPerInchX / DpiUtil.DefaultPixelsPerInch &&
                        UIElement.DpiScaleYValues[index] == pixelsPerInchY / DpiUtil.DefaultPixelsPerInch)
                    {
                        break;
                    }
                }

                // Didn't find a match, add to the end of the list
                if (index == sizeOfList)
                {
                    UIElement.DpiScaleXValues.Add(pixelsPerInchX / DpiUtil.DefaultPixelsPerInch);
                    UIElement.DpiScaleYValues.Add(pixelsPerInchY / DpiUtil.DefaultPixelsPerInch);
                }

                // Since we only have 2 bits to spare in VisualFlags, we use the first 3 possible
                // values to directly map the index to the array. If the index is >= 3, we set both flags
                // to true, and store the actual index in an UncommonField on the Visual

                if (index < 3)
                {
                    dpiScaleFlag1 = (index & 0x1) != 0;
                    dpiScaleFlag2 = (index & 0x2) != 0;
                }
                else
                {
                    dpiScaleFlag1 = dpiScaleFlag2 = true;
                }

                return new DpiFlags(dpiScaleFlag1, dpiScaleFlag2, index);
            }
        }

        internal const double DefaultPixelsPerInch = 96.0;
    }
}
