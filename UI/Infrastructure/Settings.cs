using Engine.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using Keys = System.Windows.Forms.Keys;

namespace UI.Infrastructure
{
  public class Settings
  {
    private static Settings _current;
    private static readonly object _syncObj = new object();
    private const string FileName = "Settings.xml";

    public static Settings Current
    {
      get
      {
        if (_current != null)
          return _current;

        lock (_syncObj)
        {
          if (_current == null)
          {
            var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (!File.Exists(fileName))
              return _current = GetDefault();

            var serializer = new XmlSerializer(typeof(Settings));
            using (var stream = File.Open(fileName, FileMode.Open))
              _current = (Settings)serializer.Deserialize(stream);
          }
        }

        return _current;
      }
    }

    private static Settings GetDefault()
    {
      return new Settings
      {
        Locale = "en-US",

        Nick = "User",
        NickColor = Color.FromArgb(170, 50, 50),
        RandomColor = true,

        FormSize = new Size(380, 470),
        Alerts = true,

        ServerAddress = "93.170.186.160:10021",
        TrustedCertificatesPath = "TrustedCertificates",

        ServerStartAddress = "0.0.0.0:10021",
        ServerStartP2PPort = 10022,

        RecorderKey = Keys.E,
        Frequency = 44100,
        Bits = 16
      };
    }

    public static void SaveSettings()
    {
      if (_current == null)
        return;

      lock (_syncObj)
      {
        var serializer = new XmlSerializer(typeof(Settings));
        using (var stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + FileName))
          serializer.Serialize(stream, _current);
      }
    }

    #region properties
    public string Locale { get; set; }

    public string Nick { get; set; }
    public SavedColor NickColor { get; set; }
    public bool RandomColor { get; set; }
    public string AdminPassword { get; set; }

    public Size FormSize { get; set; }
    public bool Alerts { get; set; }

    public string ServerAddress { get; set; }
    public string CertificatePath { get; set; }
    public string TrustedCertificatesPath { get; set; }

    public string ServerStartAddress { get; set; }
    public int ServerStartP2PPort { get; set; }
    public string ServerStartCertificatePath { get; set; }
    
    public Keys RecorderKey { get; set; }
    public string OutputAudioDevice { get; set; }
    public string InputAudioDevice { get; set; }
    public int Frequency { get; set; }
    public int Bits { get; set; }

    public List<PluginSetting> Plugins { get; set; }
    #endregion

    public class SavedColor
    {
      private SavedColor() { }

      public byte R { get; set; }
      public byte G { get; set; }
      public byte B { get; set; }

      public static implicit operator Color(SavedColor color)
      {
        return Color.FromArgb(color.R, color.G, color.B);
      }

      public static implicit operator SavedColor(Color color)
      {
        return new SavedColor { R = color.R, G = color.G, B = color.B };
      }
    }
  }
}
