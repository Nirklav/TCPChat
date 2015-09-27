using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Client
{
  [SecurityCritical]
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
      [SecurityCritical]
      get { return model.User; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecurityCritical]
      get { return model.Rooms; }
    }

    public List<DownloadingFile> DownloadingFiles
    {
      [SecurityCritical]
      get { return model.DownloadingFiles; }
    }

    public List<PostedFile> PostedFiles
    {
      [SecurityCritical]
      get { return model.PostedFiles; }
    }
    #endregion
  }
}
