//-----------------------------------------------------------------------------
//
// <copyright file="ResourceDictionaryDiagnostics.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//
// Description:
//  ResourceDictionary diagnostics API
//      i.   Enables enumeration of generic and themed ResourceDictionary instances.
//      ii.  Notifies listeners when generic or themed ResourceDictionary instances 
//           get loaded
//      iii. Notifies listeners when themed ResourceDictionary instances get 
//           unloaded (generic ResourceDictionary instances are never unloaded)
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Enables enumeration of generic and themed <see cref="ResourceDictionary"/> instances, and provides 
    /// a notification infrastructure for listening to loading and unloading of ResourceDictionary instances.
    /// </summary>
    /// <remarks>
    /// This type supports the .NET Framework infrastructure and is not intended to be used directly 
    /// from application code.
    /// </remarks>
    public static class ResourceDictionaryDiagnostics
    {
        /// <summary>
        /// When a managed debugger is attached, enumerates all instances of 
        /// themed <see cref="ResourceDictionary"/> instances loaded
        /// by the application.
        /// </summary>
        public static IEnumerable<ResourceDictionaryInfo> ThemedResourceDictionaries
        {
            get
            {
                if (!ResourceDictionaryDiagnostics.IsResourceDictionaryDiagnosticsEnabled)
                {
                    return ResourceDictionaryDiagnostics.EmptyResourceDictionaries;
                }

                return SystemResources.ThemedResourceDictionaries;
            }
        }

        /// <summary>
        /// When a managed debugger is attached, enumerates all instances of 
        /// generic <see cref="ResourceDictionary"/> instances loaded
        /// by the application.
        /// </summary>
        public static IEnumerable<ResourceDictionaryInfo> GenericResourceDictionaries
        {
            get
            {
                if (!ResourceDictionaryDiagnostics.IsResourceDictionaryDiagnosticsEnabled)
                {
                    return ResourceDictionaryDiagnostics.EmptyResourceDictionaries;
                }

                return SystemResources.GenericResourceDictionaries;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a themed <see cref="ResourceDictionary"/> is loaded
        /// by the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryLoadedEventArgs> ThemedResourceDictionaryLoaded
        {
            add
            {
                if (ResourceDictionaryDiagnostics.IsResourceDictionaryDiagnosticsEnabled)
                {
                    SystemResources.ThemedDictionaryLoaded += value;
                }
            }

            remove
            {
                SystemResources.ThemedDictionaryLoaded -= value;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a themed <see cref="ResourceDictionary"/> is unloaded
        /// from the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryUnloadedEventArgs> ThemedResourceDictionaryUnloaded
        {
            add
            {
                if (ResourceDictionaryDiagnostics.IsResourceDictionaryDiagnosticsEnabled)
                {
                    SystemResources.ThemedDictionaryUnloaded += value;
                }
            }

            remove
            {
                SystemResources.ThemedDictionaryUnloaded -= value;
            }
        }

        /// <summary>
        /// Occurs when a managed debugger is attached, and a generic <see cref="ResourceDictionary"/> is loaded
        /// by the application.
        /// </summary>
        public static event EventHandler<ResourceDictionaryLoadedEventArgs> GenericResourceDictionaryLoaded
        {
            add
            {
                if(ResourceDictionaryDiagnostics.IsResourceDictionaryDiagnosticsEnabled)
                {
                    SystemResources.GenericDictionaryLoaded += value;
                }
            }

            remove
            {
                SystemResources.GenericDictionaryLoaded -= value;
            }
        }

        internal static bool IsResourceDictionaryDiagnosticsEnabled
        {
            get
            {
                return (System.Diagnostics.Debugger.IsAttached || ResourceDictionaryDiagnostics.s_EnableForTestPurposes);
            }
        }

        private static readonly ReadOnlyCollection<ResourceDictionaryInfo> EmptyResourceDictionaries 
            = new List<ResourceDictionaryInfo>().AsReadOnly();
        private static bool s_EnableForTestPurposes = false;
    }
}
