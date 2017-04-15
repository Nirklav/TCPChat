using Engine.Api.Server.Admin;
using Engine.Model.Client;
using System;
using System.Security;

namespace Engine.Api.Client.Admin
{
  [Serializable]
  public class ClientSendAdminAction : IAction
  {
    private readonly string _password;
    private readonly string _textCommand;

    [SecuritySafeCritical]
    public ClientSendAdminAction(string password, string textCommand)
    {
      if (!ServerAdminCommand.IsTextCommand(textCommand))
        throw new ArgumentException("this is not a text command");

      _password = password;
      _textCommand = textCommand;
    }

    [SecuritySafeCritical]
    public void Perform()
    {
      var messageContent = new ServerAdminCommand.MessageContent { Password = _password, TextCommand = _textCommand };
      ClientModel.Client.SendMessage(ServerAdminCommand.CommandId, messageContent);
    }
  }
}
