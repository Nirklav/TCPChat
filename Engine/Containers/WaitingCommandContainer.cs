using Engine.Model.Entities;

namespace Engine.Containers
{
  /// <summary>
  /// Команды ожидающие прямого подключения к клиенту.
  /// </summary>
  public class WaitingCommandContainer
  {
    /// <summary>
    /// Создает экземпляр класса.
    /// </summary>
    /// <param name="connectionId">Индетификатор соединениея.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="content">Параметр команды.</param>
    public WaitingCommandContainer(ushort id, object content)
      : this(id, content, false)
    { }

    /// <summary>
    /// Создает экземпляр класса.
    /// </summary>
    /// <param name="connectionId">Индетификатор соединениея.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="content">Параметр команды.</param>
    /// <param name="unconnected">Небезопасное.</param>
    public WaitingCommandContainer(ushort id, object content, bool unconnected)
    {
      CommandId = id;
      MessageContent = content;
      Unconnected = unconnected;
    }

    /// <summary>
    /// Индетификатор команды.
    /// </summary>
    public ushort CommandId { get; set; }

    /// <summary>
    /// Параметр команды.
    /// </summary>
    public object MessageContent { get; set; }

    /// <summary>
    /// Передать небезапосное сообщение.
    /// </summary>
    public bool Unconnected { get; set; }
  }
}
