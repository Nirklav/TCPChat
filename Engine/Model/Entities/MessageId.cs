namespace Engine.Model.Entities
{
  public enum MessageId
  {
    None = 0,

    ApiNotSupported = 1,                   // Данный API не поддерживается сервером.
    ConnectionRetryAttempt = 2,            // Попытка соединения с сервером...

    NotRegisteredBadName = 10,             // Соединение не может быть зарегистрировано с таким ником. Выберите другой.
    NotRegisteredNameAlreadyExist = 11,    // Соединение не может быть зарегистрировано с таким ником. Он занят.

    RoomAdminChanged = 20,                 // Вы назначены администратором комнаты. {0} - название команаты.
    RoomAccessDenied = 21,                 // Вы не входите в состав этой комнаты.
    RoomAlreadyExist = 22,                 // Комната с таким именем уже создана, выберите другое имя.
    RoomCantLeaveMainRoom = 23,            // Невозможно выйти из основной комнаты.
    RoomItsMainRoom = 24,                  // Невозможно произвести это действие с основной комнатой.
    RoomNotExist = 25,                     // Команата с таким именем не существует.
    RoomUserNotExist = 26,                 // Такого пользователя нет в комнате TODO: первести

    FileRemoveAccessDenied = 30,           // Вы не можете удалить данный файл. Не хватает прав.
    FileRemoved = 31,                      // Файл удален {0} - имя файла

    MessageEditAccessDenied = 40,          // Вы не можете редактировать это сообщение.

    P2PUserNotExist = 50,                  // Данного пользователя не существует.
  }
}
