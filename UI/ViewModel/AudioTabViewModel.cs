using OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace UI.ViewModel
{
  public class AudioTabViewModel : SettingsTabViewModel
  {
    #region fields
    private IList<string> outputDevices;
    private IList<string> inputDevices;
    private IList<string> inputConfigs;
    private int selectedInputIndex;
    private int selectedOutputIndex;
    private int selectedConfigIndex;
    #endregion

    #region properties
    public IList<string> OutputDevices
    {
      get { return outputDevices; }
      set { SetValue(value, "OutputDevices", v => outputDevices = v); }
    }

    public IList<string> InputDevices
    {
      get { return inputDevices; }
      set { SetValue(value, "InputDevices", v => inputDevices = v); }
    }

    public IList<string> InputConfigs
    {
      get { return inputConfigs; }
      set { SetValue(value, "InputConfigs", v => inputConfigs = v); }
    }

    public int SelectedInputIndex
    {
      get { return selectedInputIndex; }
      set { SetValue(value, "SelectedInputIndex", v => selectedInputIndex = v); }
    }

    public int SelectedConfigIndex
    {
      get { return selectedConfigIndex; }
      set { SetValue(value, "SelectedConfigIndex", v => selectedConfigIndex = v); }
    }

    public int SelectedOutputIndex
    {
      get { return selectedOutputIndex; }
      set { SetValue(value, "SelectedOutputIndex", v => selectedOutputIndex = v); }
    }

    public string SelectedOutput { get; set; }
    public string SelectedInput { get; set; }
    public string SelectedConfig { get; set; }
    #endregion

    public AudioTabViewModel(string name, Dispatcher dispatcher) : base(name, dispatcher)
    {
      OutputDevices = AudioContext.AvailableDevices;
      InputDevices = AudioCapture.AvailableDevices;
      InputConfigs = new[]
      {
        "8 бит / 22050 Гц",
        "16 бит / 22050 Гц",
        "8 бит / 44100 Гц",
        "16 бит / 44100 Гц"
      };

      SelectedOutputIndex = 0;
      SelectedInputIndex = 0;
      SelectedConfigIndex = 0;
    }

    public override void SaveSettings()
    {
      
    }
  }
}
