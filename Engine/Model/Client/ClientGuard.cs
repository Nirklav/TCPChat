using Engine.Model.Entities;
using System.Collections.Generic;
using System.Security;

namespace Engine.Model.Client
{
  public class ClientGuard : ModelGuard<ClientModel>
  {
    #region contructor
    [SecurityCritical]
    public ClientGuard(ClientModel model) : base(model)
    {

    }
    #endregion

    #region properties
    public User User
    {
      [SecuritySafeCritical]
      get { return _model.User; }
    }

    public Dictionary<string, User> Users
    {
      [SecuritySafeCritical]
      get { return _model.Users; }
    }

    public Dictionary<string, Room> Rooms
    {
      [SecuritySafeCritical]
      get { return _model.Rooms; }
    }

    public List<DownloadingFile> DownloadingFiles
    {
      [SecuritySafeCritical]
      get { return _model.DownloadingFiles; }
    }

    public List<PostedFile> PostedFiles
    {
      [SecuritySafeCritical]
      get { return _model.PostedFiles; }
    }
    #endregion
  }
}
