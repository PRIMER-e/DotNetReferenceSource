// Copyright (c) Microsoft Corporation.  All rights reserved.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Provides extension methods for retrieving <see cref="DSA" /> implementations for the
    /// public and private keys of a <see cref="X509Certificate2" />.
    /// </summary>
    public static class DSACertificateExtensions
    {
        /// <summary>
        /// Gets the <see cref="DSA" /> public key from the certificate, or <c>null</c>
        /// if the certificate does not have a DSA public key.
        /// </summary>
        [SecuritySafeCritical]
        public static DSA GetDSAPublicKey(this X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (!IsDSA(certificate))
            {
                return null;
            }

            unsafe
            {
                DSAParameters dp = new DSAParameters();

                SafeLocalAllocHandle dssKeyLocalAlloc = null;
                try
                {
                    byte[] encodedPublicKey = certificate.PublicKey.EncodedKeyValue.RawData;
                    uint cbDSSKey;
                    if (!CapiNative.DecodeObject((IntPtr)(CapiNative.X509_DSS_PUBLICKEY), encodedPublicKey, out dssKeyLocalAlloc, out cbDSSKey))
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    if (cbDSSKey < Marshal.SizeOf(typeof(CapiNative.CRYPTOAPI_BLOB)))
                        throw new CryptographicException();

                    CapiNative.CRYPTOAPI_BLOB* pDssKeyBlob = (CapiNative.CRYPTOAPI_BLOB*)(dssKeyLocalAlloc.DangerousGetHandle());
                    dp.Y = ToBigEndianByteArray(*pDssKeyBlob);
                }
                finally
                {
                    if (dssKeyLocalAlloc != null)
                    {
                        dssKeyLocalAlloc.Dispose();
                        dssKeyLocalAlloc = null;
                    }
                }

                SafeLocalAllocHandle dssParametersLocalHandle = null;
                try
                {
                    byte[] encodedKeyAlgorithmParameters = certificate.GetKeyAlgorithmParameters();
                    uint cbDSSParams;
                    if (!CapiNative.DecodeObject((IntPtr)(CapiNative.X509_DSS_PARAMETERS), encodedKeyAlgorithmParameters, out dssParametersLocalHandle, out cbDSSParams))
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    if (cbDSSParams < Marshal.SizeOf(typeof(CapiNative.CERT_DSS_PARAMETERS)))
                        throw new CryptographicException();

                    CapiNative.CERT_DSS_PARAMETERS* pDssParameters = (CapiNative.CERT_DSS_PARAMETERS*)(dssParametersLocalHandle.DangerousGetHandle());
                    dp.P = ToBigEndianByteArray(pDssParameters->p);
                    dp.Q = ToBigEndianByteArray(pDssParameters->q);
                    dp.G = ToBigEndianByteArray(pDssParameters->g);
                }
                finally
                {
                    if (dssParametersLocalHandle != null)
                    {
                        dssParametersLocalHandle.Dispose();
                        dssParametersLocalHandle = null;
                    }
                }

                DSACng dsaCng = new DSACng();
                dsaCng.ImportParameters(dp);
                return dsaCng;
            }
        }

        /// <summary>
        /// Gets the <see cref="DSA" /> private key from the certificate, or <c>null</c>
        /// if the certificate does not have a DSA private key.
        /// </summary>
        [SecuritySafeCritical]
        public static DSA GetDSAPrivateKey(this X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("certificate");
            }

            if (!certificate.HasPrivateKey || !IsDSA(certificate))
            {
                return null;
            }

            using (SafeCertContextHandle certificateContext = X509Native.GetCertificateContext(certificate))
            using (SafeNCryptKeyHandle privateKeyHandle = X509Native.TryAcquireCngPrivateKey(certificateContext))
            {
                if (privateKeyHandle == null)
                {
                    // fall back to CAPI if we cannot acquire the key using CNG.
                    DSACryptoServiceProvider dsaCsp = (DSACryptoServiceProvider)certificate.PrivateKey;
                    CspParameters cspParameters = CopyCspParameters(dsaCsp);
                    DSACryptoServiceProvider clone = new DSACryptoServiceProvider(cspParameters);
                    return clone;
                }

                CngKey key = CngKey.Open(privateKeyHandle, CngKeyHandleOpenOptions.None);
                return new DSACng(key);
            }
        }

        private static bool IsDSA(X509Certificate2 certificate)
        {
            return certificate.PublicKey.Oid.Value == "1.2.840.10040.4.1";
        }

        internal static CspParameters CopyCspParameters(ICspAsymmetricAlgorithm cspAlgorithm)
        {
            CspKeyContainerInfo cspInfo = cspAlgorithm.CspKeyContainerInfo;

            CspParameters cspParameters = new CspParameters(cspInfo.ProviderType, cspInfo.ProviderName, cspInfo.KeyContainerName)
            {
                Flags = CspProviderFlags.UseExistingKey,
                KeyNumber = (int)cspInfo.KeyNumber,
            };

            if (cspInfo.MachineKeyStore)
            {
                cspParameters.Flags |= CspProviderFlags.UseMachineKeyStore;
            }

            return cspParameters;
        }

        [SecuritySafeCritical]
        private static unsafe byte[] ToBigEndianByteArray(CapiNative.CRYPTOAPI_BLOB blob)
        {
            int count = blob.cbData;
            byte[] data = new byte[count];
            Marshal.Copy(blob.pbData, data, 0, count);
            Array.Reverse(data);
            return data;
        }
    }
}
