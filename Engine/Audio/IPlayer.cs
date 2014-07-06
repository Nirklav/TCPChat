using Engine.Model.Entities;
using System;

namespace Engine.Audio
{
  public interface IPlayer : IDisposable
  {
    /// <summary>
    /// Устанавливает устройство воспроизводящее звук.
    /// </summary>
    /// <param name="deviceName">Имя устройства.</param>
    void SetOptions(string deviceName);

    /// <summary>
    /// Ставит в очередь на воспроизведение массив звуковых данных, для пользователя.
    /// </summary>
    /// <param name="id">Id пользователя.</param>
    /// <param name="packNumber">
    /// Номер пакета. 
    /// Если меньше чем текущий воспроизведенный, пакет воспроизведен не будет.
    /// </param>
    /// <param name="pack">Пакет с данными о записи.</param>
    void Enqueue(string id, long packNumber, SoundPack pack);

    /// <summary>
    /// Оставноить воспроизведение для пользователя.
    /// </summary>
    /// <param name="id">Id Пользователя.</param>
    void Stop(string id);

    /// <summary>
    /// Остановить воспроизведение.
    /// </summary>
    void Stop();
  }
}
