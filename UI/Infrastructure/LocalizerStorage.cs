using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UI.Infrastructure
{
  public class LocalizerStorage
  {
    private static readonly object _langSyncObject = new object();
    private static string[] _languages;

    private readonly string _locale;
    private readonly Dictionary<string, string> _storage;

    public LocalizerStorage(string storageLocale)
    {
      var fileName = GetFilePath(storageLocale);

      _locale = storageLocale;
      _storage = LoadXml(fileName);
    }

    public string Locale { get { return _locale; } }

    public string Get(string locale, string key)
    {
      if (!string.Equals(this._locale, locale, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Storage not support this locale");

      string result;
      _storage.TryGetValue(key, out result);
      return result;
    }

    public static string[] Languages
    {
      get
      {
        if (_languages != null)
          return _languages;

        lock (_langSyncObject)
        {
          if (_languages != null)
            return _languages;

          var result = new List<string>();
          var files = Directory.EnumerateFiles(GetDir(), "*.xml", SearchOption.TopDirectoryOnly);
          foreach (var path in files)
          {
            var fileName = Path.GetFileName(path);
            // Localization.ru-ru.xml => ru-ru
            var splitted = fileName.Split('.');
            if (splitted.Length != 3)
              continue;

            var lang = splitted[1];
            result.Add(lang);
          }

          return _languages = result.ToArray();
        }
      }
    }

    private static Dictionary<string, string> LoadXml(string filePath)
    {
      var result = new Dictionary<string, string>();

      using (var file = File.OpenRead(filePath))
      using (var reader = XmlReader.Create(file))
      {
        reader.MoveToContent(); // To Localization tag

        while (reader.Read())
        {
          switch (reader.NodeType)
          {
            case XmlNodeType.Element:
              if (reader.AttributeCount != 2)
                throw new InvalidDataException("xml file malformed");

              var key = reader.GetAttribute("key");
              var value = reader.GetAttribute("value");
              result.Add(key, value);
              break;
          }
        }
      }

      return result;
    }

    private static string GetDir()
    {
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization");
    }

    private static string GetFilePath(string locale)
    {
      var relative = string.Format("Localization.{0}.xml", locale);
      return Path.Combine(GetDir(), relative);
    }
  }
}
