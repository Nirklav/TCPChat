using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Client
{
  public class ClientContext : ModelContext<ClientModel>
  {
    #region contructor
    [SecurityCritical]
    public ClientContext(ClientModel model) : base(model)
    {

    }
    #endregion

    #region properties
    public User User
    {
      [SecuritySafeCritical]
      get { return model.User; }
    }

    public Dictionary<string, User> Users
    {
      [SecuritySafeCritical]
      get { return model.Users; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecuritySafeCritical]
      get { return model.Rooms; }
    }

    public List<DownloadingFile> DownloadingFiles
    {
      [SecuritySafeCritical]
      get { return model.DownloadingFiles; }
    }

    public List<PostedFile> PostedFiles
    {
      [SecuritySafeCritical]
      get { return model.PostedFiles; }
    }
    #endregion
  }
}
