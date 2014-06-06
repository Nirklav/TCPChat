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
    public WaitingCommandContainer(string connectionId, ushort id, object content)
    {
      ConnectionId = connectionId;
      CommandId = id;
      MessageContent = content;
    }

    /// <summary>
    /// Индетификатор соединениея.
    /// </summary>
    public string ConnectionId { get; set; }

    /// <summary>
    /// Индетификатор команды.
    /// </summary>
    public ushort CommandId { get; set; }

    /// <summary>
    /// Параметр команды.
    /// </summary>
    public object MessageContent { get; set; }
  }
}
