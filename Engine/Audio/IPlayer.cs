using Engine.Model.Common.Entities;
using System;
using System.Collections.Generic;

namespace Engine.Audio
{
  // TODO: rus
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
    void Enqueue(UserId id, long packNumber, SoundPack pack);

    /// <summary>
    /// Оставноить воспроизведение для пользователя.
    /// </summary>
    /// <param name="id">Id Пользователя.</param>
    void Stop(UserId id);

    /// <summary>
    /// Остановить воспроизведение.
    /// </summary>
    void Stop();

    /// <summary>
    /// Возвращает заначение говоряеще о том инциализирован ли класс.
    /// </summary>
    bool IsInited { get; }

    /// <summary>
    /// Возвращает список утросйств воспроизводящих звук.
    /// </summary>
    IList<string> Devices { get; }
  }
}
