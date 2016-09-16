namespace Engine.Api
{
  public interface IApi<in TArgs>
    where TArgs : CommandArgs
  {
    /// <summary>
    /// Name and version of Api.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the command by identifier.
    /// </summary>
    /// <param name="id">Command identifier.</param>
    /// <returns>Command.</returns>
    ICommand<TArgs> GetCommand(long id);

    /// <summary>
    /// Perform the remote action.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    void Perform(IAction action);
  }

  public static class Api
  {
    /// <summary>
    /// Name and version of Api.
    /// </summary>
    public const string Name = "StandardAPI v4.0";
  }
}
