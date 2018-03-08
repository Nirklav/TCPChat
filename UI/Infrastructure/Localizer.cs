using Engine.Exceptions;
using Engine.Model.Client;
using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;
using System.Threading;

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

    private readonly Dictionary<string, LocalizerStorage> _storages = new Dictionary<string, LocalizerStorage>(StringComparer.OrdinalIgnoreCase);
    private string _locale;

    public event EventHandler<EventArgs> LocaleChanged;

    public void Set(string locale)
    {
      this._locale = locale;

      var e = Interlocked.CompareExchange(ref LocaleChanged, null, null);
      if (e != null)
        e(this, EventArgs.Empty);
    }

    public string Localize(string key, params string[] formatParams)
    {
      LocalizerStorage storage;

      if (!_storages.TryGetValue(_locale, out storage))
        _storages.Add(_locale, storage = new LocalizerStorage(_locale));

      var localized = storage.Get(_locale, key);
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
