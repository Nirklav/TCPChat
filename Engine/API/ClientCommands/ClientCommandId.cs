namespace Engine.API.ClientCommands
{
  //Команды для клиента: (формат сообщений XX XX Serialized(this.MessageContent))
  //Расшифровка XX XX:
  //F 00 00: Регистрация принята
  //F 00 01: Регистрация не принята (ник уже существует)

  //F 00 10: Вывести общее сообщение для комнаты
  //F 00 11: Вывести личное сообщение
  //F 00 12: Вывести системное сообщение

  //F 00 30: Открыта комната
  //F 00 31: Закрыта комната
  //F 00 32: Комната обновлена

  //F 00 40: Опубликовать файл
  //F 00 41: Файл больше не раздается
  //F 00 42: Прочитать часть файла
  //F 00 43: Записать часть файла

  //F 00 50: Ожидать прямое соединение
  //F 00 51: Выполнить прямое соединение
  //F 00 52: Подключится к сервису P2P

  //F 00 60: Воспроизвести голос

  //F 00 FF: Пинг ответ

  //F FF FF: Пустая команда

  enum ClientCommandId : long
  {
    RegistrationResponse = 0xF0000,

    OutRoomMessage = 0xF0010,
    OutPrivateMessage = 0xF0011,
    OutSystemMessage = 0xF0013,

    RoomOpened = 0xF0030,
    RoomClosed = 0xF0031,
    RoomRefreshed = 0xF0032,

    FilePosted = 0xF0040,
    PostedFileDeleted = 0xF0041,
    ReadFilePart = 0xF0042,
    WriteFilePart = 0xF0043,

    WaitPeerConnection = 0xF0050,
    ConnectToPeer = 0xF0051,
    ConnectToP2PService = 0xF0052,

    PlayVoice = 0xF0060,

    PingResponce = 0xF00FF,

    Empty = 0xFFFFF
  }
}
