using System.Security.Cryptography.X509Certificates;
using Engine.Helpers;
using Engine.Model.Common;

namespace Engine.Model.Client
{
  public class ClientCertificatesStorage : CertificatesStorage
  {
    private readonly IClientNotifier _notifier;

    public ClientCertificatesStorage(string path, IClientNotifier notifier, Logger logger)
      : base(path, logger)
    {
      _notifier = notifier;
    }

    protected override void OnAdded(X509Certificate2 certificate)
    {
      base.OnAdded(certificate);
      _notifier.TrustedCertificatesChanged(new TrustedCertificatesEventArgs(certificate, false));
    }

    protected override void OnRemoved(X509Certificate2 certificate)
    {
      base.OnRemoved(certificate);
      _notifier.TrustedCertificatesChanged(new TrustedCertificatesEventArgs(certificate, true));
    }
  }
}
