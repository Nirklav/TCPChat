using System;
using System.Security.Cryptography.X509Certificates;

namespace Engine
{
  public class TrustedCertificatesEventArgs : EventArgs
  {
    public X509Certificate2 Certificate { get; private set; }
    public bool Removed { get; private set; }

    public TrustedCertificatesEventArgs(X509Certificate2 cert, bool removed)
    {
      Certificate = cert;
      Removed = removed;
    }
  }
}
