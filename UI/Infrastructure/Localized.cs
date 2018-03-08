using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace UI.Infrastructure
{
  public class Localized : MarkupExtension
  {
    private string _key;

    public Localized(string key)
    {
      this._key = key;
    }

    [ConstructorArgument("key")]
    public string Key
    {
      get { return _key; }
      set { _key = value; }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      var binding = new Binding("Value") { Source = new LocalizedBindingSource(_key) };
      return binding.ProvideValue(serviceProvider);
    }
  }

  class LocalizedBindingSource : 
    IWeakEventListener,
    INotifyPropertyChanged,
    IDisposable
  {
    private string _key;

    public event PropertyChangedEventHandler PropertyChanged;

    #region constructor
    public LocalizedBindingSource(string key)
    {
      _key = key;
      LocalizerEventManager.AddListener(Localizer.Instance, this);
    }

    public void Dispose()
    {
      LocalizerEventManager.RemoveListener(Localizer.Instance, this);
    }
    #endregion

    public object Value
    {
      get { return Localizer.Instance.Localize(_key); }
    }

    public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
      if (managerType == typeof(LocalizerEventManager))
      {
        OnLanguageChanged(sender, e);
        return true;
      }
      return false;
    }

    private void OnLanguageChanged(object sender, EventArgs args)
    {
      var e = PropertyChanged;
      if (e != null)
        e(this, new PropertyChangedEventArgs("Value"));
    }
  }

  class LocalizerEventManager : WeakEventManager
  {
    private LocalizerEventManager()
    { }

    public static void AddListener(Localizer source, IWeakEventListener listener)
    {
      if (source == null)
        throw new ArgumentNullException("source");
      if (listener == null)
        throw new ArgumentNullException("listener");

      CurrentManager.ProtectedAddListener(source, listener);
    }

    public static void RemoveListener(Localizer source, IWeakEventListener listener)
    {
      if (source == null)
        throw new ArgumentNullException("source");
      if (listener == null)
        throw new ArgumentNullException("listener");

      CurrentManager.ProtectedRemoveListener(source, listener);
    }

    private static LocalizerEventManager CurrentManager
    {
      get
      {
        var managerType = typeof(LocalizerEventManager);
        var manager = (LocalizerEventManager)GetCurrentManager(managerType);

        if (manager == null)
        {
          manager = new LocalizerEventManager();
          SetCurrentManager(managerType, manager);
        }

        return manager;
      }
    }

    protected override ListenerList NewListenerList()
    {
      return new ListenerList();
    }

    protected override void StartListening(object source)
    {
      var typedSource = (Localizer)source;
      typedSource.LocaleChanged += OnLocaleChanged;
    }

    protected override void StopListening(object source)
    {
      var typedSource = (Localizer)source;
      typedSource.LocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(object sender, EventArgs e)
    {
      DeliverEvent(sender, e);
    }
  }
}
