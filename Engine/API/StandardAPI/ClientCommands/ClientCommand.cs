using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.API.StandardAPI.ClientCommands
{
  //Команды для клиента: (формат сообщений XX XX Serialized(this.MessageContent))
  //Расшифровка XX XX:
  //80 00: Регистрация принята
  //80 01: Регистрация не принята (ник уже существует)

  //80 10: Вывести общее сообщение для комнаты
  //80 11: Вывести личное сообщение
  //80 12: Вывести системное сообщение

  //80 20: Получен откртый ключ пользователя

  //80 30: Открыта комната
  //80 31: Закрыта комната
  //80 32: Комната обновлена

  //80 40: Опубликовать файл
  //80 41: Файл больше не раздается
  //80 42: Прочитать часть файла
  //80 43: Записать часть файла

  //80 50: Ожидать прямое соединение
  //80 51: Выполнить прямое соединение
  //80 52: Подключится к сервису P2P

  //80 60: Воспроизвести голос
  //80 61: Открыта голосовая комната

  //80 FF: Пинг ответ

  //FF FF: Пустая команда

  enum ClientCommand : ushort
  {
    RegistrationResponse = 0x8000,

    OutRoomMessage = 0x8010,
    OutPrivateMessage = 0x8011,
    OutSystemMessage = 0x8013,

    ReceiveUserOpenKey = 0x8020,

    RoomOpened = 0x8030,
    RoomClosed = 0x8031,
    RoomRefreshed = 0x8032,

    FilePosted = 0x8040,
    PostedFileDeleted = 0x8041,
    ReadFilePart = 0x8042,
    WriteFilePart = 0x8043,

    WaitPeerConnection = 0x8050,
    ConnectToPeer = 0x8051,
    ConnectToP2PService = 0x8052,

    PlayVoice = 0x8060,
    VoiceRoomOpened = 0x8061,

    PingResponce = 0x80FF,

    Empty = 0xFFFF
  }
}
