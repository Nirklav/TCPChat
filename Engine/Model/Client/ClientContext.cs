using Engine.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Model.Client
{
  public class ClientContext : ModelContext<ClientModel>
  {
    #region contructor
    public ClientContext(ClientModel model) : base(model)
    {

    }
    #endregion

    #region properties
    public User User { get { return model.User; } }
    public Dictionary<string, Room> Rooms { get { return model.Rooms; } }
    public List<DownloadingFile> DownloadingFiles { get { return model.DownloadingFiles; } }
    public List<PostedFile> PostedFiles { get { return model.PostedFiles; } }
    #endregion
  }
}
