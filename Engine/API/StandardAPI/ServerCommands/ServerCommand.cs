using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.API.StandardAPI.ServerCommands
{
  //Команды для сервера: (формат сообщений XX XX Serialized(this.MessageContent))
  //Расшифровка XX XX:
  //00 00: Запрос регистрации (в главной комнате)
  //00 01: Запрос выхода (из всех комнат)

  //00 10: Отправка сообщения всем клиентам в комнате
  //00 11: Отправка личного сообщения конкретному юзеру

  //00 20: Запрос открытого пароля пользователя

  //00 30: Создать комнату
  //00 31: Удалить комнату
  //00 32: Пригласить пользователей в комнату
  //00 33: Кикнуть пользователей из комнаты
  //00 34: Выйти из комнаты
  //00 35: Запрос обновления комнаты
  //00 36: Сделать пользователя администратором комнаты

  //00 40: Пинг запрос

  //00 50: Добавить файл на раздачу комнаты
  //00 51: Удалить файл с раздачи комнаты

  //00 60: Запрос прямого соединения
  //00 61: Ответ, говорящий о готовности принять входное содеинение

  //7F FF: Пустая команда

  enum ServerCommand : ushort
  {
    Register = 0x0000,
    Unregister = 0x0001,

    SendRoomMessage = 0x0010,
    SendPrivateMessage = 0x0011,

    GetUserOpenKeyRequest = 0x0020,

    CreateRoom = 0x0030,
    DeleteRoom = 0x0031,
    InvateUsers = 0x0032,
    KickUsers = 0x0033,
    ExitFromRoom = 0x0034,
    RefreshRoom = 0x0035,
    SetRoomAdmin = 0x0036,

    PingRequest = 0x0040,

    AddFileToRoom = 0x0050,
    RemoveFileFromRoom = 0x0051,

    P2PConnectRequest = 0x0060,
    P2PReadyAccept = 0x0061,

    Empty = 0x7FFF
  }
}
