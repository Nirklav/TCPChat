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
    /// Устанавливает настройки качества записи.
    /// </summary>
    /// <param name="channels">Колисчество каналов.</param>
    /// <param name="bitPerChannel">Количество бит на канал.</param>
    /// <param name="frequency">Частота записи.</param>
    /// <param name="samplesSize">Размер буффера в семплах.</param>
    void SetOptions(int channels, int bitPerChannel, int frequency, int samplesSize);

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
