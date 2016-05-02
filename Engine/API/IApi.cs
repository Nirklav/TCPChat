namespace Engine.API
{
  public interface IApi<in TArgs>
    where TArgs : CommandArgs
  {
    string Name { get; }

    ICommand<TArgs> GetCommand(long id);
  }

  public static class Api
  {
    /// <summary>
    /// Версия и имя API.
    /// </summary>
    public const string Name = "StandardAPI v3.1";
  }
}
