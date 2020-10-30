//------------------------------------------------------------------------------
//
// <copyright file="InputPane.cs" company="Microsoft">
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
//
//------------------------------------------------------------------------------

using MS.Internal.PresentationCore.WindowsRuntime;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MS.Internal.WindowsRuntime
{
    namespace Windows.UI.ViewManagement
    {
        /// <summary>
        /// DevDiv:1193138
        /// This class wraps the corresponding WinRT APIs for InputPane.  This is used to show the touch keyboard.
        /// Note that WinRT events belonging to this class are not included for simplicity.
        /// 
        /// This class uses RCWs for WinRT objects in order to properly cast from a WinRT IActivationFactory and
        /// therefore implements IDisposable in order to allow for fast RCW cleanup.
        /// </summary>
        internal class InputPane : IDisposable
        {
            #region Fields

            /// <summary>
            /// The name of the InputPane WinRT runtime class
            /// </summary>
            private static readonly string s_TypeName = "Windows.UI.ViewManagement.InputPane, Windows, ContentType=WindowsRuntime";

            /// <summary>
            /// The InputPane Type
            /// </summary>
            private static Type s_WinRTType;

            /// <summary>
            /// Activation factory to instantiate InputPane RCWs
            /// </summary>
            [SecurityCritical]
            private static IActivationFactory _winRtActivationFactory;

            /// <summary>
            /// The appropriate RCW for calling TryShow/Hide
            /// </summary>
            [SecurityCritical]
            private InputPaneRcw.IInputPane2 _inputPane;

            #endregion

            #region Constructors

            /// <summary>
            /// Acquires the InputPane type from the winmd
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            static InputPane()
            {
                // We don't want to throw here - so wrap in try..catch
                try
                {
                    s_WinRTType = Type.GetType(s_TypeName);
                }
                catch
                {
                    s_WinRTType = null;
                }
            }

            /// <summary>
            /// Checks that the InputPane type was loaded and gets a new InputPane for the parent window.
            /// </summary>
            /// <exception cref="PlatformNotSupportedException"></exception>
            /// <SecurityNote>
            ///     Critical
            ///         Accesses COM RCWs IActivationFactory, IInputPaneInterop, IInputPane2.
            ///         Calls GetWinRtActivationFactory and IInputPaneInterop.GetForWindow.
            /// </SecurityNote>
            [SecurityCritical]
            private InputPane(IntPtr? hwnd)
            {
                if (s_WinRTType == null)
                {
                    throw new PlatformNotSupportedException();
                }

                try
                {
                    if (hwnd.HasValue)
                    {
                        InputPaneRcw.IInputPaneInterop inputPaneInterop;

                        try
                        {
                            // Get the IActivationFactory and cast to IInputPaneInterop.  The WinRT pattern for
                            // static factory types implements an activation factory that allows casting to one
                            // or more COM interfaces containing "static" methods to use as initialization.  In 
                            // the case of InputPane this is an interop class that contains an init function
                            // designed to take an HWND.  This interface is cloaked and not part of the WinRT
                            // projections and therefore cannot be seen by reflecting on the type.
                            inputPaneInterop = GetWinRtActivationFactory() as InputPaneRcw.IInputPaneInterop;
                        }
                        catch (COMException)
                        {
                            // Do a fine grained catch here to detect the activation factory going stale.
                            // If this happens, we retry the cast querying a new factory.  If this retry fails
                            // something else is going wrong, allow the error to be handled by the outer block.
                            inputPaneInterop = GetWinRtActivationFactory(forceInitialization: true) as InputPaneRcw.IInputPaneInterop;
                        }

                        _inputPane = inputPaneInterop?.GetForWindow(hwnd.Value, typeof(InputPaneRcw.IInputPane2).GUID);
                    }
                }
                catch (COMException)
                {
                    // Something went wrong in acquiring/using one of the COM RCWs above.
                    // This is not a fatal error and just means that this particular attempt to initialize InputPane
                    // has failed or the platform does not support this access (this can be caught at the Type init
                    // but it is possible that is not the case).
                }

                if (_inputPane == null)
                {
                    throw new PlatformNotSupportedException();
                }
            }

            #endregion

            #region Member Functions

            /// <summary>
            /// Wraps creation in a manner analagous to the WinRT interface to this class.
            /// </summary>
            /// <returns>A new InputPane</returns>
            /// <exception cref="PlatformNotSupportedException"></exception>
            /// <SecurityNote>
            ///     Critical
            ///         Calls InputPane constructor
            ///         Accesses CriticalHandle from HwndSource
            /// </SecurityNote>
            [SecurityCritical]
            internal static InputPane GetForWindow(HwndSource source)
            {
                return new InputPane(source?.CriticalHandle ?? null);
            }

            /// <summary>
            /// Attempts to show the touch keyboard
            /// </summary>
            /// <returns>True if successful, false otherwise</returns>
            /// <SecurityNote>
            ///     Critical
            ///         Accesses COM RCW function IInputPane2.TryShow
            /// </SecurityNote>
            [SecurityCritical]
            internal bool TryShow()
            {
                bool result = false;

                try
                {
                    result = _inputPane?.TryShow() ?? false;
                }
                catch (COMException)
                {
                    // It's possible that the IInputPane2 has gone stale for some reason
                    // in that case we should catch the exception here and simply return
                    // false indicating that the KB did not show.
                }

                return result;
            }

            /// <summary>
            /// Attempts to hide the touch keyboard
            /// </summary>
            /// <returns>True if successful, false otherwise</returns>
            /// <SecurityNote>
            ///     Critical
            ///         Accesses COM RCW function IInputPane2.TryHide
            /// </SecurityNote>
            [SecurityCritical]
            internal bool TryHide()
            {
                bool result = false;

                try
                {
                    result = _inputPane?.TryHide() ?? false;
                }
                catch (COMException)
                {
                    // It's possible that the IInputPane2 has gone stale for some reason
                    // in that case we should catch the exception here and simply return
                    // false indicating that the KB did not show.
                }

                return result;
            }

            /// <summary>
            /// Creates, caches, and returns a WinRT activation factory for use with the InputPane runtime type.
            /// </summary>
            /// <param name="forceInitialization">If true, will create a new IActivationFactory.  If false will
            /// only create a new IActivationFactory if there is no valid cached instance available.</param>
            /// <returns>An IActivationFactory of InputPane</returns>
            /// <SecurityNote>
            ///     Critical
            ///         Accesses COM RCW IActivationFactory and function WindowsRuntimeMarshal.GetActivationFactory
            /// </SecurityNote>
            [SecurityCritical]
            private static IActivationFactory GetWinRtActivationFactory(bool forceInitialization = false)
            {
                if (forceInitialization || _winRtActivationFactory == null)
                {
                    _winRtActivationFactory = WindowsRuntimeMarshal.GetActivationFactory(s_WinRTType);
                }

                return _winRtActivationFactory;
            }

            #endregion

            #region IDisposable

            bool _disposed = false;

            ~InputPane()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Releases the _inputPane RCW
            /// </summary>
            /// <param name="disposing">True if called from a Dispose() call, false when called from the finalizer</param>
            /// <SecurityNote>
            ///     Critical
            ///         Calls Marshal.ReleaseCOMObject
            ///         Accesses critical COM RCWs
            ///     SafeCritical
            ///         Does not expose critical COM RCWs to callers
            /// </SecurityNote>
            [SecuritySafeCritical]
            private void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (_inputPane != null)
                    {
                        try
                        {
                            // Release the input pane here
                            Marshal.ReleaseComObject(_inputPane);
                        }
                        catch
                        {
                            // Don't want to raise any exceptions in a finalizer, eat them here
                        }

                        _inputPane = null;
                    }

                    _disposed = true;
                }
            }

            #endregion
        }
    }
}
