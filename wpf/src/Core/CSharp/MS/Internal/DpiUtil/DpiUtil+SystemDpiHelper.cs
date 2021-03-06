// <copyright file="DpiUtil+SystemDpiHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace MS.Internal
{
    using MS.Win32;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Security;

    using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

    /// <content>
    /// Contains definition of <see cref="SystemDpiHelper"/>
    /// </content>
    internal static partial class DpiUtil
    {
        /// <summary>
        /// Gets the System DPI
        /// </summary>
        /// <remarks>
        /// If the system DPI cannot be obtained directly, then
        /// it falls back to obtaining the system DPI indirectly
        /// by querying through the desktop.
        /// </remarks>
        private static class SystemDpiHelper
        {
            /// <summary>
            /// True when user32!GetDpiForSystem function is available in the currently
            /// running platform, False otherwise
            /// </summary>
            private static bool IsGetDpiForSystemFunctionAvailable { get; set; } = true;

            /// <summary>
            /// Gets the System DPI
            /// </summary>
            /// <returns>The system DPI</returns>
            /// <remarks>
            /// If the system DPI cannot be obtained directly, then
            /// it falls back to obtaining the system DPI indirectly
            /// by querying through the desktop.
            /// 
            /// If querying via the desktop fails for some reason, then
            /// it returns null.
            /// </remarks>
            internal static DpiScale2 GetSystemDpi()
            {
                if (IsGetDpiForSystemFunctionAvailable)
                {
                    try
                    {
                        return GetDpiForSystem();
                    }
                    catch (Exception e) when (e is EntryPointNotFoundException || e is MissingMethodException || e is DllNotFoundException)
                    {
                        IsGetDpiForSystemFunctionAvailable = false;
                    }
                }

                return GetSystemDpiFromDeviceCaps();
            }

            /// <summary>
            /// Gets the System DPI from the values stored in the UIElement static cache
            /// </summary>
            /// <returns>The system DPI value</returns>
            internal static DpiScale2 GetSystemDpiFromUIElementCache()
            {
                lock (UIElement.DpiLock)
                {
                    return new DpiScale2(UIElement.DpiScaleXValues[0], UIElement.DpiScaleYValues[0]);
                }
            }

            /// <summary>
            /// Updates the UIElement static cache containing System DPI value
            /// </summary>
            /// <param name="systemDpiScale">Updated System DPI scale value</param>
            internal static void UpdateUIElementCacheForSystemDpi(DpiScale2 systemDpiScale)
            {
                lock(UIElement.DpiLock)
                {
                    UIElement.DpiScaleXValues.Insert(0, systemDpiScale.DpiScaleX);
                    UIElement.DpiScaleYValues.Insert(0, systemDpiScale.DpiScaleY);
                }
            }

            /// <summary>
            /// Returns the System DPI by querying the value directly.
            /// </summary>
            /// <returns>The system DPI</returns>
            private static DpiScale2 GetDpiForSystem()
            {
                uint dpi = SafeNativeMethods.GetDpiForSystem();
                return DpiScale2.FromPixelsPerInch(dpi, dpi);
            }

            /// <summary>
            /// Returns System DPI by querying the value indirectly
            /// from the desktop device context
            /// </summary>
            /// <returns>The system DPI</returns>
            /// <SecurityNote>
            ///     Critical: Calls native methods
            ///     Safe: Returns only non-critical information to the caller
            /// </SecurityNote>
            [SecuritySafeCritical]
            private static DpiScale2 GetSystemDpiFromDeviceCaps()
            {
                HandleRef hWndDesktop = new HandleRef(IntPtr.Zero, IntPtr.Zero);
                HandleRef hDC = new HandleRef(IntPtr.Zero, UnsafeNativeMethods.GetDC(hWndDesktop));
                if (hDC.Handle == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    int ppiX = UnsafeNativeMethods.GetDeviceCaps(hDC, NativeMethods.LOGPIXELSX);
                    int ppiY = UnsafeNativeMethods.GetDeviceCaps(hDC, NativeMethods.LOGPIXELSY);

                    return DpiScale2.FromPixelsPerInch(ppiX, ppiY);
                }
                finally
                {
                    UnsafeNativeMethods.ReleaseDC(hWndDesktop, hDC);
                }
            }
        }
    }
}
