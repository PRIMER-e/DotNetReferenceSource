//---------------------------------------------------------------------------
// File: RequestStatusBarUpdateEventArgs.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
// 
//---------------------------------------------------------------------------

using System;
using System.Net;
using System.Windows;
using MS.Internal.Utility;
using System.Security;

namespace MS.Internal.AppModel
{
    internal sealed class RequestSetStatusBarEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Text that will be set on the status bar.
        /// </summary>
        /// <SecurityNote>
        ///     CriticalDataForSet - Arbitrary changes to the status bar text could open up for spoofing attacks.
        /// </SecurityNote>
        private SecurityCriticalDataForSet<string> _text;

        /// <summary>
        /// Creates a RequestSetStatusBarEventArgs based on a specified string.
        /// </summary>
        /// <param name="text">Text that will be set on the status bar.</param>
        /// <SecurityNote>
        ///     Critical - Sets the status bar text; could open up for spoofing attacks.
        /// </SecurityNote>
        [SecurityCritical]
        internal RequestSetStatusBarEventArgs(string text)
            : base()
        {
            _text.Value = text;
            base.RoutedEvent = System.Windows.Documents.Hyperlink.RequestSetStatusBarEvent;
        }

        /// <summary>
        /// Creates a RequestSetStatusBarEventArgs based on a specified URI.
        /// </summary>
        /// <param name="targetUri">URI that will be set on the status bar after appropriate conversion to text. If null, the status bar will be cleared.</param>
        /// <SecurityNote>
        ///     Critical - Sets the status bar text; could open up for spoofing attacks.
        /// </SecurityNote>
        [SecurityCritical]
        internal RequestSetStatusBarEventArgs(Uri targetUri)
            : base()
        {
            if (targetUri == null)
                _text.Value = String.Empty;
            else
                _text.Value = BindUriHelper.UriToString(targetUri);

            base.RoutedEvent = System.Windows.Documents.Hyperlink.RequestSetStatusBarEvent;
        }

        /// <summary>
        /// Text that will be set on the status bar.
        /// </summary>
        internal string Text
        {
            get
            {
                return _text.Value;
            }
        }

        /// <summary>
        /// Request object for clearing the status bar.
        /// </summary>
        /// <SecurityNote>
        ///     Critical - Calls the critical ctor that allows setting the status bar text.
        ///     TreatAsSafe - We control the input to the status bar (String.Empty).
        ///                   The critical stuff is setting the status bar to a URI; we consider clearing the status bar safe.
        /// </SecurityNote>
        internal static RequestSetStatusBarEventArgs Clear
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                return new RequestSetStatusBarEventArgs(String.Empty);
            }
        }
    }
}

