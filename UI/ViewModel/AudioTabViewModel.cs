using OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
      set
      {
        outputDevices = value;
        OnPropertyChanged("OutputDevices");
      }
    }

    public IList<string> InputDevices
    {
      get { return inputDevices; }
      set
      {
        inputDevices = value;
        OnPropertyChanged("InputDevices");
      }
    }

    public IList<string> InputConfigs
    {
      get { return inputConfigs; }
      set
      {
        inputConfigs = value;
        OnPropertyChanged("InputConfigs");
      }
    }

    public int SelectedInputIndex
    {
      get { return selectedInputIndex; }
      set
      {
        selectedInputIndex = value;
        OnPropertyChanged("SelectedInputIndex");
      }
    }

    public int SelectedConfigIndex
    {
      get { return selectedConfigIndex; }
      set
      {
        selectedConfigIndex = value;
        OnPropertyChanged("SelectedConfigIndex");
      }
    }

    public int SelectedOutputIndex
    {
      get { return selectedOutputIndex; }
      set
      {
        selectedOutputIndex = value;
        OnPropertyChanged("SelectedOutputIndex");
      }
    }

    public string SelectedOutput { get; set; }
    public string SelectedInput { get; set; }
    public string SelectedConfig { get; set; }
    #endregion

    public AudioTabViewModel(string name) : base(name)
    {
      OutputDevices = AudioContext.AvailableDevices;
      InputDevices = AudioCapture.AvailableDevices;
      InputConfigs = new[] { "Конфиг 1", "Конфиг 2" };

      SelectedOutputIndex = 0;
      SelectedInputIndex = 0;
      SelectedConfigIndex = 0;
    }

    public override void SaveSettings()
    {
      
    }
  }
}
