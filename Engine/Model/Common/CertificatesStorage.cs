using Engine.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Engine.Model.Common
{
  public class CertificatesStorage
  {
    private readonly string _path;
    private readonly Dictionary<string, X509Certificate2> _certificates;
    private readonly Logger _logger;

    public CertificatesStorage(string path, Logger logger)
    {
      _path = path;
      _certificates = new Dictionary<string, X509Certificate2>();
      _logger = logger;

      Load();
    }

    private void Load()
    {
      var info = Directory.CreateDirectory(_path);
      foreach (var file in info.EnumerateFiles("*.cer"))
      {
        try
        {
          var cert = new X509Certificate2(file.FullName);
          _certificates.Add(file.Name, cert);
        }
        catch (Exception e)
        {
          _logger.Write(e);
        }
      }
    }

    public void Add(X509Certificate2 certificate)
    {
      lock (_certificates)
      {
        var name = CreateFileName(certificate);
        _certificates.Add(name, certificate);
        var filePath = Path.Combine(_path, name);
        File.WriteAllBytes(filePath, certificate.RawData);

        OnAdded(certificate);
      }
    }
    
    public void Remove(X509Certificate2 certificate)
    {
      lock (_certificates)
      {
        string foundName = null;
        foreach (var kvp in _certificates)
        {
          var name = kvp.Key;
          var cert = kvp.Value;

          if (cert.Equals(certificate))
            foundName = name;
        }

        _certificates.Remove(foundName);
        var filePath = Path.Combine(_path, foundName);
        File.Delete(filePath);

        OnRemoved(certificate);
      }
    }

    private static string CreateFileName(X509Certificate2 certificate)
    {
      var time = DateTime.UtcNow.ToString("dd-MM-yyyy-HH-mm-ss-FFF");
      var commonName = certificate.GetNameInfo(X509NameType.SimpleName, false);
      var builderCommonName = new StringBuilder(commonName);

      foreach (var ch in Path.GetInvalidFileNameChars())
        builderCommonName.Replace(ch, '_');
      
      return string.IsNullOrEmpty(certificate.FriendlyName)
        ? string.Format("{0}_{1}.cer", builderCommonName, time)
        : string.Format("{0}_{1}.cer", certificate.FriendlyName, time);
    }

    public X509Certificate2Collection CreateCollection()
    {
      lock (_certificates)
      {
        var collection = new X509Certificate2Collection();
        foreach (var cert in _certificates.Values)
          collection.Add(cert);
        return collection;
      }
    }

    public bool Exist(X509Certificate2 certificate)
    {
      lock (_certificates)
      {
        foreach (var cert in _certificates.Values)
          if (cert.Equals(certificate))
            return true;
        return false;
      }
    }

    public void ForEach(Action<X509Certificate2> iterator)
    {
      lock (_certificates)
        foreach (var cert in _certificates.Values)
          iterator(cert);
    }

    protected virtual void OnAdded(X509Certificate2 certificate) { }
    protected virtual void OnRemoved(X509Certificate2 certificate) { }
  }
}
