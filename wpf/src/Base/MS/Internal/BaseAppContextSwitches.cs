//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
// 
// File: BaseAppContextSwitches.cs
//---------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal
{
    /// <summary>
    /// Appcompat switches used by WindowsBase. See comments at the start of each switch. 
    /// Also see AppContextDefaultValues which initializes default values for each of 
    /// these switches.
    /// </summary>
    internal static class BaseAppContextSwitches
    {
        /// <summary>
        /// Starting .NET 4.6, ExecutionContext tracks Thread.CurrentCulture and Thread.CurrentUICulture, which would be restored 
        /// to their respective previous values after a call to ExecutionContext.Run. This behavior is undesirable within the 
        /// Dispatcher - various dispatcher operations can run user code that can in turn set Thread.CurrentCulture or 
        /// Thread.CurrentUICulture, and we do not want those values to be overwritten with their respective previous values. 
        /// To work around the new ExecutionContext behavior, we introduce CulturePreservingExecutionContext for use within 
        /// Dispatcher and DispatcherOperation. WPF in .NET 4.6 & 4.6.1 shipped with buggy behavior - each DispatcherOperation 
        /// ends with all modificaitons to culture infos being reverted.Though unlikely, if some applications targeting 4.6 or 
        /// above might have taken a dependence on this 

        #region UseCulturePreservingDispatcherOperations

        internal const string SwitchDoNotUseCulturePreservingDispatcherOperations = "Switch.MS.Internal.DoNotUseCulturePreservingDispatcherOperations";
        private static int _doNotUseCulturePreservingDispatcherOperations;

        public static bool DoNotUseCulturePreservingDispatcherOperations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchDoNotUseCulturePreservingDispatcherOperations, ref _doNotUseCulturePreservingDispatcherOperations);
            }
        }

        #endregion
    }
}
