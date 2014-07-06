using Engine.Model.Entities;
using System;

namespace Engine.Audio
{
  public interface IRecorder : IDisposable
  {
    /// <summary>
    /// Происходит при заполнении буффера.
    /// </summary>
    event EventHandler<RecordedEventArgs> Recorded;

    /// <summary>
    /// Устанавливает настройки качества записи. И устройство записывающее звук.
    /// </summary>
    /// <param name="deviceName">Имя устройства.</param>
    /// <param name="quality">Количество записи.</param>
    void SetOptions(string deviceName, AudioQuality quality);

    /// <summary>
    /// Запускает запись с микрофона.
    /// </summary>
    void Start();

    /// <summary>
    /// Остонавливает запись с микрофона.
    /// </summary>
    void Stop();
  }
}
