using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class ClientTabViewModel : SettingsTabViewModel
  {
    #region fields
    private string nick;

    private byte redValue;
    private byte greenValue;
    private byte blueValue;
    #endregion

    #region properties
    public string Nick
    {
      get { return nick; }
      set { SetValue(value, "Nick", v => nick = v); }
    }

    public WPFColor NickColor
    {
      get { return WPFColor.FromRgb(RedValue, GreenValue, BlueValue); }
    }

    public byte RedValue
    {
      get { return redValue; }
      set { SetValue(value, "NickColor", v => redValue = v); }
    }

    public byte GreenValue
    {
      get { return greenValue; }
      set { SetValue(value, "NickColor", v => greenValue = v); }
    }

    public byte BlueValue
    {
      get { return blueValue; }
      set { SetValue(value, "NickColor", v => blueValue = v); }
    }

    #endregion

    public ClientTabViewModel(string name) : base(name)
    {
      Nick = Settings.Current.Nick;

      RedValue = Settings.Current.NickColor.R;
      GreenValue = Settings.Current.NickColor.G;
      BlueValue = Settings.Current.NickColor.B;
    }

    public override void SaveSettings()
    {
      Settings.Current.Nick = Nick;
      Settings.Current.NickColor = Color.FromArgb(RedValue, GreenValue, BlueValue);
    }
  }
}
