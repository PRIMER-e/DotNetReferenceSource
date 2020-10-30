//------------------------------------------------------------------------------
//
// <copyright file="TipTsfHelper.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
//------------------------------------------------------------------------------

using System;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal.WindowsRuntime.Windows.UI.ViewManagement;

namespace MS.Internal.Interop
{
    /// <summary>
    /// DevDiv:1193138
    /// 
    /// This class provides functionality to show/hide the touch keyboard.
    /// This is necessary in Win8+ as touch keyboard invocation was changed 
    /// to use WM_POINTER message tracking.  Since Wisp does not allow these
    /// to propogate to WPF, we have to forcibly show the keyboard.
    /// 
    /// As of creation of this class, we never call Hide, only Show.  This is
    /// because the touch keyboard is smart about when to show or hide.  As long
    /// as we attempt to show it on each focus, it will determine whether this is
    /// a valid scenario or whether it should close.
    /// 
    /// If we are run under a platform that does not support the proper calls into
    /// the InputPane WinRT API we will catch an exception on the first call to
    /// TryShow/Hide and then cache the fact that we no longer need to make these calls.
    /// </summary>
    internal static class TipTsfHelper
    {
        /// <summary>
        /// Cache if the API call is supported on the current platform
        /// </summary>
        private static bool s_PlatformSupported = true;

        /// <summary>
        /// Attempts to show the touch keyboard.
        /// We rely on the Windows API to determine if showing the keyboard aligns with the 
        /// current state (no physical KB, touch enabled, focused edit control).
        /// </summary>
        /// <SecurityNote>
        ///     Critical
        ///         Calls critical functions InputPane.GetForWindow and InputPane.TryShow
        ///     SafeCritical
        ///         Does not expose critical data to callers (InputPane)
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static void Show(DependencyObject focusedObject)
        {
            // DDVSO:222625
            // We need to only show if applicable to this focused object
            // so guard the calls to TryShow here.
            if (s_PlatformSupported && ShouldShow(focusedObject))
            {
                InputPane ip;

                try
                {
                    using (ip = InputPane.GetForWindow(GetHwndSource(focusedObject)))
                    {
                        ip?.TryShow();
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    s_PlatformSupported = false;
                }
            }
        }

        /// <summary>
        /// Attempts to hide the touch keyboard.
        /// </summary>
        /// <SecurityNote>
        ///     Critical
        ///         Calls critical functions InputPane.GetForWindow and InputPane.TryHide
        ///     SafeCritical
        ///         Does not expose critical data to callers (InputPane)
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static void Hide(DependencyObject focusedObject)
        {
            if (s_PlatformSupported)
            {
                InputPane ip;

                try
                {
                    using (ip = InputPane.GetForWindow(GetHwndSource(focusedObject)))
                    {
                        ip?.TryHide();
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    s_PlatformSupported = false;
                }
            }
        }

        /// <summary>
        /// The KB should only show when we have an object that implements the
        /// UIAutomation Text pattern.  Therefore, we should test for this 
        /// pattern on any focused object that we get.
        /// </summary>
        /// <param name="focusedObject">The object being focused</param>
        /// <returns>True if the touch KB should show, false otherwise.</returns>
        private static bool ShouldShow(DependencyObject focusedObject)
        {
            UIElement uiElement;
            UIElement3D uiElement3D;
            ContentElement contentElement;
            AutomationPeer peer = null;

            if ((uiElement = focusedObject as UIElement) != null)
            {
                peer = uiElement.GetAutomationPeer();
            }
            else if ((uiElement3D = focusedObject as UIElement3D) != null)
            {
                peer = uiElement3D.GetAutomationPeer();
            }
            else if ((contentElement = focusedObject as ContentElement) != null)
            {
                peer = contentElement.GetAutomationPeer();
            }

            return peer?.GetPattern(PatternInterface.Text) != null;
        }

        /// <summary>
        /// Returns an HwndSource from the Visual
        /// </summary>
        /// <param name="focusedVisual">The visual to get the HwndSource for</param>
        /// <returns>The HwndSource associated with the Visual</returns>
        [SecurityCritical]
        private static HwndSource GetHwndSource(DependencyObject focusedObject)
        {
            return PresentationSource.CriticalFromVisual(focusedObject) as HwndSource;
        }
    }
}
