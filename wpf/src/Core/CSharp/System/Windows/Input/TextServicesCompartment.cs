//---------------------------------------------------------------------------
//
// <copyright file=TextServicesCompartment.cs company=Microsoft>
//    Copyright (C) Microsoft Corporation.  All rights reserved.
// </copyright>
// 
// Description: Manages Text Services Compartment.
//
// History:  
//  07/30/2003 : yutakas - Ported from .net tree.
//
//---------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using System.Collections;
using MS.Utility;
using MS.Win32;
using MS.Internal;

namespace System.Windows.Input 
{
    //------------------------------------------------------
    //
    //  TextServicesCompartment class
    //
    //------------------------------------------------------
 
    internal class TextServicesCompartment
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <SecurityNote>
        /// Critical - directly calls unmanaged code based on guid
        /// </SecurityNote>
        [SecurityCritical]
        internal TextServicesCompartment(Guid guid, UnsafeNativeMethods.ITfCompartmentMgr compartmentmgr)
        {
            _guid = guid;
            _compartmentmgr = new SecurityCriticalData<UnsafeNativeMethods.ITfCompartmentMgr>(compartmentmgr);
            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods
        #endregion Public Methods        
 
        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
 
  
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
 
 
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///     Advise the notify sink of the compartment update.
        /// </summary>
        /// <SecurityNote>
        /// Critical - wires up multiple unmanaged objects together
        /// </SecurityNote>
        [SecurityCritical]
        internal void AdviseNotifySink(UnsafeNativeMethods.ITfCompartmentEventSink sink)
        {
            Debug.Assert(_cookie == UnsafeNativeMethods.TF_INVALID_COOKIE, "cookie is already set.");

            UnsafeNativeMethods.ITfCompartment compartment = GetITfCompartment();
            if (compartment == null)
                return;

            UnsafeNativeMethods.ITfSource source = compartment as UnsafeNativeMethods.ITfSource;

            // workaround because I can't pass a ref to a readonly constant
            Guid guid = UnsafeNativeMethods.IID_ITfCompartmentEventSink;

            source.AdviseSink(ref guid, sink, out _cookie);
            Marshal.ReleaseComObject(compartment);
            Marshal.ReleaseComObject(source);
        }

        /// <summary>
        ///     Unadvise the notify sink of the compartment update.
        /// </summary>
        /// <SecurityNote>
        /// Critical - wires up multiple unmanaged objects together
        /// </SecurityNote>
        [SecurityCritical]
        internal void UnadviseNotifySink()
        {
            Debug.Assert(_cookie != UnsafeNativeMethods.TF_INVALID_COOKIE, "cookie is not set.");

            UnsafeNativeMethods.ITfCompartment compartment = GetITfCompartment();
            if (compartment == null)
                return;

            UnsafeNativeMethods.ITfSource source = compartment as UnsafeNativeMethods.ITfSource;
            source.UnadviseSink(_cookie);
            _cookie = UnsafeNativeMethods.TF_INVALID_COOKIE;

            Marshal.ReleaseComObject(compartment);
            Marshal.ReleaseComObject(source);
        }

        /// <summary>
        ///    Retrieve ITfCompartment
        /// </summary>
        /// <SecurityNote>
        /// Critical - returns critical resource
        /// </SecurityNote>
        [SecurityCritical]
        internal UnsafeNativeMethods.ITfCompartment GetITfCompartment()
        {
            UnsafeNativeMethods.ITfCompartment itfcompartment;
            _compartmentmgr.Value.GetCompartment(ref _guid, out itfcompartment);
            return itfcompartment;
        }

        #endregion Internal methods
        
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        
        #region Internal Properties

        /// <summary>
        ///     Cast the compartment variant to bool.
        /// </summary>
        internal bool BooleanValue
        {
            get
            {
                object obj = Value;
                if (obj == null)
                    return false;

                if ((int)obj != 0)
                    return true;
 
                return false;
            }
            set
            {
                Value = value ? 1 : 0;
            }
        }

        /// <summary>
        ///     Cast the compartment variant to int.
        /// </summary>
        internal int IntValue
        {
            get
            {
                object obj = Value;
                if (obj == null)
                    return 0;

                return (int)obj;
            }
            set
            {
                Value = value;
            }
        }

        /// <summary>
        ///     Get the compartment variant.
        /// </summary>
        /// <SecurityNote>
        /// Critical - access unmanaged code
        /// TreatAsSafe - returns "safe" variant based value from the store
        /// </SecurityNote>
        internal object Value
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                UnsafeNativeMethods.ITfCompartment compartment = GetITfCompartment();

                if (compartment == null)
                    return null;

                object obj;

                compartment.GetValue(out obj);

                Marshal.ReleaseComObject(compartment);

                return obj;
            }
            [SecurityCritical, SecurityTreatAsSafe]
            set
            {
                UnsafeNativeMethods.ITfCompartment compartment = GetITfCompartment();

                if (compartment == null)
                    return;

                compartment.SetValue(0 /* clientid */, ref value);
                Marshal.ReleaseComObject(compartment);
            }
        }

        #endregion Internal Properties
 
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
                
        #region Private Fields

        /// <SecurityNote>
        ///     Critical: UnsafeNativeMethods.ITfCompartmentMgr has methods with SuppressUnmanagedCodeSecurity.
        /// </SecurityNote>
        [SecurityCritical]
        private readonly SecurityCriticalData<UnsafeNativeMethods.ITfCompartmentMgr> _compartmentmgr;

        private Guid _guid;
        private int _cookie;

        #endregion Private Fields
 
    }
}
