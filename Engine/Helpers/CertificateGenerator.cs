using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Engine.Helpers
{
  public static class CertificateGenerator
  {
    [SecuritySafeCritical]
    public static byte[] CreateSelfSignedPfx(string subject, DateTime startTime, DateTime endTime, SecureString password)
    {
      if (subject == null)
        throw new ArgumentNullException("Subject is null");

      var startSystemTime = ToSystemTime(startTime);
      var endSystemTime = ToSystemTime(endTime);
      var containerName = Guid.NewGuid().ToString();

      var dataHandle = new GCHandle();
      var providerContext = IntPtr.Zero;
      var cryptKey = IntPtr.Zero;
      var certContext = IntPtr.Zero;
      var certStore = IntPtr.Zero;
      var storeCertContext = IntPtr.Zero;
      var passwordPtr = IntPtr.Zero;

      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Check(NativeMethods.CryptAcquireContextW(out providerContext, containerName, null, ProviderType.RsaAes, 8 /*CRYPT_NEWKEYSET*/));
        Check(NativeMethods.CryptGenKey(providerContext, 1 /*AT_KEYEXCHANGE*/, 0x08000001 /*RSA2048BIT_KEY | CRYPT_EXPORTABLE*/, out cryptKey));

        // errorStringPtr gets a pointer into the middle of the subject string,
        // so subject needs to be pinned until after we've copied the value
        // of errorStringPtr.
        dataHandle = GCHandle.Alloc(subject, GCHandleType.Pinned);
        var nameDataLength = 0;

        if (!NativeMethods.CertStrToNameW(
          0x00010001, // X509_ASN_ENCODING | PKCS_7_ASN_ENCODING
          dataHandle.AddrOfPinnedObject(),
          3, // CERT_X500_NAME_STR = 3
          IntPtr.Zero,
          null,
          ref nameDataLength,
          out IntPtr errorStringPtr))
        {
          var error = Marshal.PtrToStringUni(errorStringPtr);
          throw new ArgumentException(error);
        }

        var subjectData = new byte[nameDataLength];
        if (!NativeMethods.CertStrToNameW(
          0x00010001, // X509_ASN_ENCODING | PKCS_7_ASN_ENCODING
          dataHandle.AddrOfPinnedObject(),
          3, // CERT_X500_NAME_STR = 3
          IntPtr.Zero,
          subjectData,
          ref nameDataLength,
          out errorStringPtr))
        {
          var error = Marshal.PtrToStringUni(errorStringPtr);
          throw new ArgumentException(error);
        }

        dataHandle.Free();

        dataHandle = GCHandle.Alloc(subjectData, GCHandleType.Pinned);
        var subjectBlob = new CryptoApiBlob(subjectData.Length, dataHandle.AddrOfPinnedObject());

        var kpi = new CryptKeyProviderInformation
        {
          ContainerName = containerName,
          ProviderType = ProviderType.RsaAes,
          KeySpec = 1 // AT_KEYEXCHANGE
        };

        var ai = new CryptAlgorithmIdentifier
        {
          pszObjId = "1.2.840.113549.1.1.13", // SHA512RSA
          Parameters = IntPtr.Zero
        };

        certContext = NativeMethods.CertCreateSelfSignCertificate(providerContext, ref subjectBlob, 0, ref kpi, ref ai, ref startSystemTime, ref endSystemTime, IntPtr.Zero);
        Check(certContext != IntPtr.Zero);
        dataHandle.Free();

        certStore = NativeMethods.CertOpenStore("Memory" /*sz_CERT_STORE_PROV_MEMORY*/, 0, IntPtr.Zero, 0x2000 /*CERT_STORE_CREATE_NEW_FLAG*/, IntPtr.Zero);
        Check(certStore != IntPtr.Zero);

        Check(NativeMethods.CertAddCertificateContextToStore(certStore, certContext, 1, /*CERT_STORE_ADD_NEW*/ out storeCertContext));
        NativeMethods.CertSetCertificateContextProperty(storeCertContext, 2 /* CERT_KEY_PROV_INFO_PROP_ID */, 0, ref kpi);

        if (password != null)
          passwordPtr = Marshal.SecureStringToCoTaskMemUnicode(password);

        var pfxBlob = new CryptoApiBlob();
        Check(NativeMethods.PFXExportCertStoreEx(certStore, ref pfxBlob, passwordPtr, IntPtr.Zero, 7)); // EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY

        var pfxData = new byte[pfxBlob.DataLength];
        dataHandle = GCHandle.Alloc(pfxData, GCHandleType.Pinned);
        pfxBlob.Data = dataHandle.AddrOfPinnedObject();
        Check(NativeMethods.PFXExportCertStoreEx(certStore, ref pfxBlob, passwordPtr, IntPtr.Zero, 7)); // EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY
        dataHandle.Free();

        return pfxData;
      }
      finally
      {
        if (passwordPtr != IntPtr.Zero)
          Marshal.ZeroFreeCoTaskMemUnicode(passwordPtr);

        if (dataHandle.IsAllocated)
          dataHandle.Free();

        if (certContext != IntPtr.Zero)
          NativeMethods.CertFreeCertificateContext(certContext);

        if (storeCertContext != IntPtr.Zero)
          NativeMethods.CertFreeCertificateContext(storeCertContext);

        if (certStore != IntPtr.Zero)
          NativeMethods.CertCloseStore(certStore, 0);

        if (cryptKey != IntPtr.Zero)
          NativeMethods.CryptDestroyKey(cryptKey);

        if (providerContext != IntPtr.Zero)
        {
          NativeMethods.CryptReleaseContext(providerContext, 0);
          NativeMethods.CryptAcquireContextW(
            out providerContext,
            containerName,
            null,
            ProviderType.RsaAes,
            0x10); // CRYPT_DELETEKEYSET
        }
      }
    }

    [SecuritySafeCritical]
    private static SystemTime ToSystemTime(DateTime dateTime)
    {
      var fileTime = dateTime.ToFileTime();
      Check(NativeMethods.FileTimeToSystemTime(ref fileTime, out SystemTime systemTime));
      return systemTime;
    }

    [SecuritySafeCritical]
    private static void Check(bool nativeCallSucceeded)
    {
      if (!nativeCallSucceeded)
      {
        int error = Marshal.GetHRForLastWin32Error();
        Marshal.ThrowExceptionForHR(error);
      }
    }

    private enum ProviderType : int
    {
      RsaFull = 1,
      RsaSig = 2,
      Dss = 3,
      Fortezza = 4,
      MsExchange = 5,
      Ssl = 6,
      RsaSChannel = 12,
      DssDh = 13,
      EcEcdsaSig = 14,
      EcEcnraSig = 15,
      EcEcdsaFull = 16,
      EcEcnraFULL = 17,
      DhSchannel = 18,
      SpyRusLynks = 20,
      Rng = 21,
      IntelSex = 22,
      RreplaceOwf = 23,
      RsaAes = 24
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime
    {
      public short Year;
      public short Month;
      public short DayOfWeek;
      public short Day;
      public short Hour;
      public short Minute;
      public short Second;
      public short Milliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CryptoApiBlob
    {
      public int DataLength;
      public IntPtr Data;

      public CryptoApiBlob(int dataLength, IntPtr data)
      {
        this.DataLength = dataLength;
        this.Data = data;
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CryptKeyProviderInformation
    {
      [MarshalAs(UnmanagedType.LPWStr)] public string ContainerName;
      [MarshalAs(UnmanagedType.LPWStr)] public string ProviderName;
      public ProviderType ProviderType;
      public int Flags;
      public int ProviderParameterCount;
      public IntPtr ProviderParameters; // PCRYPT_KEY_PROV_PARAM
      public int KeySpec;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CryptAlgorithmIdentifier
    {
      [MarshalAs(UnmanagedType.LPStr)] public String pszObjId;
      public IntPtr Parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CruptObjIdBlob
    {
      public uint cbData;
      [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
      public byte[] pbData;
    }

    private static class NativeMethods
    {
      [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool FileTimeToSystemTime(
        [In] ref long fileTime,
        out SystemTime systemTime);

      [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CryptAcquireContextW(
        out IntPtr providerContext,
        [MarshalAs(UnmanagedType.LPWStr)] string container,
        [MarshalAs(UnmanagedType.LPWStr)] string provider,
        ProviderType providerType,
        int flags);

      [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CryptReleaseContext(
        IntPtr providerContext,
        int flags);

      [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CryptGenKey(
        IntPtr providerContext,
        int algorithmId,
        int flags,
        out IntPtr cryptKeyHandle);

      [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CryptDestroyKey(
        IntPtr cryptKeyHandle);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CertStrToNameW(
        int certificateEncodingType,
        IntPtr x500,
        int strType,
        IntPtr reserved,
        [MarshalAs(UnmanagedType.LPArray)] [Out] byte[] encoded,
        ref int encodedLength,
        out IntPtr errorString);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern IntPtr CertCreateSelfSignCertificate(
        IntPtr providerHandle,
        [In] ref CryptoApiBlob subjectIssuerBlob,
        int flags,
        [In] ref CryptKeyProviderInformation keyProviderInformation,
        [In] ref CryptAlgorithmIdentifier signatureAlgorithm,
        [In] ref SystemTime startTime,
        [In] ref SystemTime endTime,
        IntPtr extensions);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CertFreeCertificateContext(IntPtr certificateContext);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern IntPtr CertOpenStore(
        [MarshalAs(UnmanagedType.LPStr)] string storeProvider,
        int messageAndCertificateEncodingType,
        IntPtr cryptProvHandle,
        int flags,
        IntPtr parameters);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CertCloseStore(
        IntPtr certificateStoreHandle,
        int flags);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CertAddCertificateContextToStore(
        IntPtr certificateStoreHandle,
        IntPtr certificateContext,
        int addDisposition,
        out IntPtr storeContextPtr);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool CertSetCertificateContextProperty(
        IntPtr certificateContext,
        int propertyId,
        int flags,
        [In] ref CryptKeyProviderInformation data);

      [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool PFXExportCertStoreEx(
        IntPtr certificateStoreHandle,
        ref CryptoApiBlob pfxBlob,
        IntPtr password,
        IntPtr reserved,
        int flags);
    }
  }
}
