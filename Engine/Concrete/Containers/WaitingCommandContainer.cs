using Engine.Concrete.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Concrete.Containers
{
  /// <summary>
  /// Команды ожидающие прямого подключения к клиенту.
  /// </summary>
  public class WaitingCommandContainer
  {
    /// <summary>
    /// Создает экземпляр класса.
    /// </summary>
    /// <param name="info">Пользователь которому необходимо послать команду.</param>
    /// <param name="id">Индетификатор команды.</param>
    /// <param name="content">Параметр команды.</param>
    public WaitingCommandContainer(User info, ushort id, object content)
    {
      Info = info;
      CommandId = id;
      MessageContent = content;
    }

    /// <summary>
    /// Пользователь которому необходимо послать команду, после подключения.
    /// </summary>
    public User Info { get; set; }

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
