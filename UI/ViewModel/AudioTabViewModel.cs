using Engine.Model.Client;
using Engine.Model.Entities;
using OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using UI.Infrastructure;

namespace UI.ViewModel
{
  public class AudioTabViewModel : SettingsTabViewModel
  {
    #region fields
    private IList<string> outputDevices;
    private IList<string> inputDevices;
    private IList<AudioQuality> inputConfigs;
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

    public IList<AudioQuality> InputConfigs
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

    public AudioTabViewModel(string name) : base(name)
    {
      OutputDevices = AudioContext.AvailableDevices;
      InputDevices = AudioCapture.AvailableDevices;
      InputConfigs = new[]
      {
        new AudioQuality(1, 8, 22050),
        new AudioQuality(1, 16, 22050),
        new AudioQuality(1, 8, 44100),
        new AudioQuality(1, 16, 44100)
      };

      SelectedOutputIndex = 0;
      SelectedInputIndex = 0;
      SelectedConfigIndex = 0;
    }

    public override void SaveSettings()
    {
      Settings.Current.Frequency = InputConfigs[SelectedConfigIndex].Frequency;
      Settings.Current.Bits = InputConfigs[SelectedConfigIndex].Bits;
      Settings.Current.OutputAudioDevice = OutputDevices[SelectedOutputIndex];
      Settings.Current.InputAudioDevice = InputDevices[selectedInputIndex];

      if (ClientModel.IsInited)
      {
        AudioQuality quality = new AudioQuality(1, Settings.Current.Bits, Settings.Current.Frequency);
        ClientModel.Recorder.SetOptions(Settings.Current.InputAudioDevice, quality);
        ClientModel.Player.SetOptions(Settings.Current.OutputAudioDevice);
      }
    }
  }
}
