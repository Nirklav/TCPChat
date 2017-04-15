using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace UI.Infrastructure
{
  public class Localizer
  {
    #region Singleton

    private readonly static Lazy<Localizer> localizer = new Lazy<Localizer>(() => new Localizer());
    public static Localizer Instance
    {
      get { return localizer.Value; }
    }

    #endregion

    private readonly Dictionary<string, LocalizerStorage> storages = new Dictionary<string, LocalizerStorage>(StringComparer.OrdinalIgnoreCase);
    private string locale;

    public event EventHandler<EventArgs> LocaleChanged;

    public void Set(string locale)
    {
      this.locale = locale;

      var e = Interlocked.CompareExchange(ref LocaleChanged, null, null);
      if (e != null)
        e(this, EventArgs.Empty);
    }

    public string Localize(string key, params string[] formatParams)
    {
      LocalizerStorage storage;

      if (!storages.TryGetValue(locale, out storage))
        storages.Add(locale, storage = new LocalizerStorage(locale));

      var localized = storage.Get(locale, key);
      if (localized == null)
        return key;

      if (formatParams == null)
        return localized;

      try
      {
        return string.Format(localized, formatParams);
      }
      catch (FormatException)
      {
        ClientModel.Logger.WriteWarning("Broken localization for key {0}", key);
        return string.Empty;
      }
    }

    public string Localize(SystemMessageId message, params string[] formatParams)
    {
      var key = GetKey(message);
      return Localize(key, formatParams);
    }

    public string Localize(ErrorCode code, params string[] formatParams)
    {
      var key = GetKey(code);
      return Localize(key, formatParams);
    }

    private string GetKey(SystemMessageId message)
    {
      return string.Format("systemMessage-{0}", message);
    }

    private string GetKey(ErrorCode code)
    {
      return string.Format("errorCode-{0}", code);
    }
  }
}
