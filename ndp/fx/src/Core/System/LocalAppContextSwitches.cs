// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==


using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class LocalAppContextSwitches 
    {
        //
        //  Switch.System.Security.Cryptography.X509Certificates.RSACertificateExtensions.DontReliablyClonePrivateKey
        //
        //    4.6 introduced the GetRSAPrivateKey() extension method on the X509Certificate2 type.
        //
        //    Unlike its predecessor (X509Certificate2.PrivateKey) this method is supposed to return a freshly allocated object
        //    each time so that the caller can safely dispose and use it without interfering with other callers of this method.
        //
        //    Unfortunately, 4.6 and 4.6.1 only honor this when returning a CNG key. If the certificate's private key was
        //    a CSP key, GetRSAPrivateKey() returns the same object each time, whose lifetime is that of the X509Certificate2 object.
        //
        //    4.6.2. fixed this method so it returns a freshly allocated object each time as intended.
        //
        //    Setting this switch to true reinstates the old broken behavior.
        // 
        internal const string DontReliablyClonePrivateKeyStr = @"Switch.System.Security.Cryptography.X509Certificates.RSACertificateExtensions.DontReliablyClonePrivateKey";
        private static int _dontReliablyClonePrivateKeyName;
        public static bool DontReliablyClonePrivateKey
        {
             [MethodImpl(MethodImplOptions.AggressiveInlining)]
             get
             {
                 return LocalAppContext.GetCachedSwitchValue(DontReliablyClonePrivateKeyStr, ref _dontReliablyClonePrivateKeyName);
             }
        }

        //
        //  Switch.System.Security.Cryptography.AesCryptoServiceProvider.DontCorrectlyResetDecryptor
        //
        //    Affects the behavior of AesCryptoServiceProvider.CreateDecryptor().TransformBlock() after a TransformFinalBlock()
        //    has been called, when a chained encryption mode such as CBC is being used.
        //
        //    Prior to 4.6.2., AesCryptoServiceProvider's Decryptor transform failed to reinitialize the transform as advertised after a
        //    TransformFinalBlock() call. The result is that further calls to TransformBlock() would yield incorrect values as AesCryptoServiceProvider
        //    would treat it as continuation of the prior decryption rather than the start of a new one.
        //
        //    4.6.2 and onward has fixed this behavior so that TransformFinalBlock() reinitializes the transform as advertised.
        //
        //    Setting this switch to "true" reinstates the pre-4.6.2. bugged behavior.
        //
        internal const string AesCryptoServiceProviderDontCorrectlyResetDecryptorStr = @"Switch.System.Security.Cryptography.AesCryptoServiceProvider.DontCorrectlyResetDecryptor";
        private static int _aesCryptoServiceProviderDontCorrectlyResetDecryptorName;
        public static bool AesCryptoServiceProviderDontCorrectlyResetDecryptor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(AesCryptoServiceProviderDontCorrectlyResetDecryptorStr, ref _aesCryptoServiceProviderDontCorrectlyResetDecryptorName);
            }
        }
    }

    internal static partial class AppContextDefaultValues
    {
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int version)
        {
            // When defining a new switch  you should add it to the last known version.
            // For instance, if you are adding a switch in .NET 4.6 (the release after 4.5.2) you should defined your switch
            // like this:
            //    if (version <= 40502) ...
            // This ensures that all previous versions of that platform (up-to 4.5.2) will get the old behavior by default
            // NOTE: When adding a default value for a switch please make sure that the default value is added to ALL of the existing platforms!
            // NOTE: When adding a new if statement for the version please ensure that ALL previous switches are enabled (ie. don't use else if)
            switch (platformIdentifier)
            {
                case ".NETCore":
                case ".NETFramework":
                {
                    if (version <= 40601)
                    {
                        LocalAppContext.DefineSwitchDefault(LocalAppContextSwitches.AesCryptoServiceProviderDontCorrectlyResetDecryptorStr, true);
                    }
                    break;
                }
            }
        }
    }
}

