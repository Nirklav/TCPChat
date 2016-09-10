namespace Engine.Api
{
  public interface IApi<in TArgs>
    where TArgs : CommandArgs
  {
    string Name { get; }

    void Perform(IAction action);

    ICommand<TArgs> GetCommand(long id);
  }

  public static class Api
  {
    /// <summary>
    /// Версия и имя API.
    /// </summary>
    public const string Name = "StandardAPI v3.4";
  }
}
