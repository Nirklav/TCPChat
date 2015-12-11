namespace Engine.API.ClientCommands
{
  abstract class ClientCommand<TContent>
    : Command<TContent, ClientCommandArgs>
  {
  }
}
