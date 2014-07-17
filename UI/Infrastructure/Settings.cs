using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Keys = System.Windows.Forms.Keys;

namespace UI.Infrastructure
{
  public class Settings
  {
    private static Settings current;
    private static readonly object syncObj = new object();
    private const string FileName = "Settings.xml";

    public static Settings Current
    {
      get
      {
        if (current != null)
          return current;

        lock (syncObj)
        {
          if (current == null)
          {         
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (FileStream stream = File.Open(AppDomain.CurrentDomain.BaseDirectory + FileName, FileMode.Open))
              current = (Settings)serializer.Deserialize(stream);
          }
        }

        return current;
      }
    }

    public static void SaveSettings()
    {
      if (current == null)
        return;

      lock (syncObj)
      {
        XmlSerializer serializer = new XmlSerializer(typeof(Settings));
        using (FileStream stream = File.Create(AppDomain.CurrentDomain.BaseDirectory + FileName))
          serializer.Serialize(stream, current);
      }
    }

    #region properties
    public string Nick { get; set; }
    public SavedColor NickColor { get; set; }

    public Size FormSize { get; set; }
    public bool Alerts { get; set; }

    public string Address { get; set; }
    public int Port { get; set; }
    public int ServicePort { get; set; }
    public bool StateOfIPv6Protocol { get; set; }

    public Keys RecorderKey { get; set; }
    public string OutputAudioDevice { get; set; }
    public string InputAudioDevice { get; set; }
    public int Frequency { get; set; }
    public int Bits { get; set; }
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
