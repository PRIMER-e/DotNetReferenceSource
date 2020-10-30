//---------------------------------------------------------------------------
//
// File: DpiChangedEventArgs.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
// 
// Description: This class is used to pass necessary information to any listener
//              of DpiChanged Event - when a window is moved to a monitor with
//              different DPI, or the dpi of the current monitor changes.
//---------------------------------------------------------------------------

using System.ComponentModel;
using System.Security;
using System.Threading;
using System.Windows.Interop;
using MS.Internal.PresentationCore;
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    /// The HwndDpiChangedEventArgs class represents a type of HandledEventArgs that
    /// is relevant to a DpiChanged event.
    /// </summary>
    public sealed class HwndDpiChangedEventArgs : HandledEventArgs
    {
        /// <summary>
        /// Constructs a HwndDpiChangedEventArgs instance.
        /// </summary>
        /// <param name="oldDpiX">
        /// The X-Axis DPI value before DPI changed.
        /// </param>    
        /// <param name="oldDpiY">
        /// The Y-Axis DPI value before DPI changed.
        /// </param>
        /// <param name="newDpiX">
        /// The X-Axis DPI value after DPI changed.
        /// </param>    
        /// <param name="newDpiY">
        /// The Y-Axis DPI value after DPI changed.
        /// </param>    
        /// <param name="lParam">
        /// A pointer to a RECT structure that provides the size and position of the suggested window, scaled for the new DPI.
        /// </param>    
        /// <SecurityNote>
        ///     Critical: This code accesses critical function *PtrToStructure*
        /// </SecurityNote>
        [SecurityCritical]
        internal HwndDpiChangedEventArgs(double oldDpiX, double oldDpiY, double newDpiX, double newDpiY, IntPtr lParam) : base(false)
        {
            OldDpi = new DpiScale(oldDpiX / DpiUtil.DefaultPixelsPerInch, oldDpiY / DpiUtil.DefaultPixelsPerInch);
            NewDpi = new DpiScale(newDpiX / DpiUtil.DefaultPixelsPerInch, newDpiY / DpiUtil.DefaultPixelsPerInch);
            NativeMethods.RECT suggestedRect = (NativeMethods.RECT)UnsafeNativeMethods.PtrToStructure(lParam, typeof(NativeMethods.RECT));
            this.SuggestedRect = new Rect((double)suggestedRect.left, (double)suggestedRect.top, (double)suggestedRect.Width, (double)suggestedRect.Height);
        }

        /// <summary>
        /// DPI Scale information before change.
        /// </summary>
        public DpiScale OldDpi { get; private set; }

        /// <summary>
        /// DPI Scale information after change.
        /// </summary>
        public DpiScale NewDpi { get; private set; }

        /// <summary>
        /// Provides the size and position of the suggested window, scaled for the new DPI.
        /// </summary>
        public Rect SuggestedRect { get; private set; }
    }

    /// <summary>
    /// The delegate to use for handlers that receive DpiChangedEventArgs.
    /// </summary>
    public delegate void DpiChangedEventHandler(object sender, DpiChangedEventArgs e);

    /// <summary>
    /// The delegate to use for handlers that receive DPI change notification.
    /// </summary>
    public delegate void HwndDpiChangedEventHandler(object sender, HwndDpiChangedEventArgs e);

    /// <summary>
    /// The DpiChangedventArgs class represents a type of RoutedEventArgs that
    /// is relevant to a DpiChanged event.
    /// </summary>
    public sealed class DpiChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Constructs a DpiChangedEventArgs instance.
        /// </summary>
        internal DpiChangedEventArgs(DpiScale oldDpi, DpiScale newDpi, RoutedEvent routedEvent, object source) : base(routedEvent, source )
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
        }

        /// <summary>
        /// DPI Scale information before change.
        /// </summary>
        public DpiScale OldDpi { get; private set; }

        /// <summary>
        /// DPI Scale information after change.
        /// </summary>
        public DpiScale NewDpi { get; private set; }
    }

    /// <summary>
    /// Wrapper class to pass all the DPI related information down the Visual tree on DPI change.
    /// </summary>
    internal class DpiRecursiveChangeArgs
    {
        internal DpiRecursiveChangeArgs(DpiFlags dpiFlags, DpiScale oldDpiScale,
            DpiScale newDpiScale)
        {
            DpiScaleFlag1 = dpiFlags.DpiScaleFlag1;
            DpiScaleFlag2 = dpiFlags.DpiScaleFlag2;
            Index = dpiFlags.Index;
            OldDpiScale = oldDpiScale;
            NewDpiScale = newDpiScale;
        }

        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
        internal DpiScale OldDpiScale { get; set; }
        internal DpiScale NewDpiScale { get; set; }
    }

    /// <summary>
    /// Wrapper class to pass DPI flags and index of the UIElement.DpiScale array
    /// </summary>
    internal class DpiFlags
    {
        internal DpiFlags(bool dpiScaleFlag1, bool dpiScaleFlag2, int index)
        {
            DpiScaleFlag1 = dpiScaleFlag1;
            DpiScaleFlag2 = dpiScaleFlag2;
            Index = index;
        }
        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
    }
}
