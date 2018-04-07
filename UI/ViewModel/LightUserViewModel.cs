using Engine.Model.Client;
using Engine.Model.Common.Entities;
using UI.Infrastructure;
using WPFColor = System.Windows.Media.Color;

namespace UI.ViewModel
{
  public class LightUserViewModel : BaseViewModel
  {
    public UserId UserId { get; private set; }
    public bool IsFake { get { return UserId == UserId.Empty; } }

    public string Nick { get; private set; }
    public WPFColor NickColor { get; private set; }

    public LightUserViewModel(UserId userId, BaseViewModel parent)
      : base(parent, false)
    {
      UserId = userId;

      using (var client = ClientModel.Get())
      {
        var user = client.Chat.TryGetUser(userId);
        if (user == null)
        {
          Nick = userId.Nick;
          NickColor = WPFColor.FromRgb(0, 0, 0);
        }
        else
        {
          Nick = userId.Nick;
          NickColor = WPFColor.FromRgb(user.NickColor.R, user.NickColor.G, user.NickColor.B);
        }
      }
    }

    public LightUserViewModel(string nickLKey, BaseViewModel parent)
      : base(parent, false)
    {
      UserId = UserId.Empty;
      Nick = Localizer.Instance.Localize(nickLKey);
      NickColor = WPFColor.FromRgb(0, 0, 0);
    }
  }
}
