using Engine.Model.Client;
using Engine.Model.Entities;
using OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Infrastructure;
using Keys = System.Windows.Forms.Keys;

namespace UI.ViewModel
{
  public class AudioTabViewModel : SettingsTabViewModel
  {
    const string PressTheKey = "Нажмите на клавишу";

    #region fields
    private IList<string> outputDevices;
    private IList<string> inputDevices;
    private IList<AudioQuality> inputConfigs;
    private int selectedInputIndex;
    private int selectedOutputIndex;
    private int selectedConfigIndex;
    private string selectButtonName;
    private volatile bool selectingKey;
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

    public string SelectButtonName
    {
      get { return selectButtonName; }
      set { SetValue(value, "SelectButtonName", v => selectButtonName = v); }
    }
    #endregion

    #region commands

    public ICommand SelectKeyCommand { get; private set; }

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

      SelectedOutputIndex = OutputDevices.IndexOf(Settings.Current.OutputAudioDevice);
      SelectedInputIndex = InputDevices.IndexOf(Settings.Current.InputAudioDevice);
      SelectedConfigIndex = InputConfigs.IndexOf(new AudioQuality(1, Settings.Current.Bits, Settings.Current.Frequency));

      if (SelectedOutputIndex == -1)
        SelectedOutputIndex = 0;

      if (SelectedInputIndex == -1)
        SelectedInputIndex = 0;

      if (SelectedConfigIndex == -1)
        SelectedConfigIndex = 0;

      SelectButtonName = Settings.Current.RecorderKey.ToString();

      SelectKeyCommand = new Command(SelectKey);
    }

    private void SelectKey(object obj)
    {
      if (selectingKey == true)
        return;

      selectingKey = true;
      SelectButtonName = PressTheKey;
      KeyBoard.KeyDown += KeyDown;
    }

    private void KeyDown(Keys key)
    {
      if (!selectingKey)
      {
        KeyBoard.KeyDown -= KeyDown;
        return;
      }

      selectingKey = false;
      SelectButtonName = key.ToString();
    }

    public override void SaveSettings()
    {
      Settings.Current.Frequency = InputConfigs[SelectedConfigIndex].Frequency;
      Settings.Current.Bits = InputConfigs[SelectedConfigIndex].Bits;
      Settings.Current.OutputAudioDevice = OutputDevices[SelectedOutputIndex];
      Settings.Current.InputAudioDevice = InputDevices[selectedInputIndex];

      if (!string.Equals(PressTheKey, SelectButtonName))
        Settings.Current.RecorderKey = (Keys)Enum.Parse(typeof(Keys), SelectButtonName);

      if (ClientModel.IsInited)
      {
        ClientModel.Recorder.SetOptions(Settings.Current.InputAudioDevice, InputConfigs[SelectedConfigIndex]);
        ClientModel.Player.SetOptions(Settings.Current.OutputAudioDevice);
      }
    }
  }
}
