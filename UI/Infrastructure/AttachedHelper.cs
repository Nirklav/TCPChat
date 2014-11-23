using System.Windows;
using System.Windows.Controls;

namespace UI.Infrastructure
{
  public class AttachedHelper
  {
    #region ScrollViewer AutoScroll
    public static bool GetAutoScroll(DependencyObject obj)
    {
      return (bool)obj.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DependencyObject obj, bool value)
    {
      obj.SetValue(AutoScrollProperty, value);
    }

    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
      "AutoScroll",
      typeof(bool),
      typeof(AttachedHelper),
      new PropertyMetadata(false, AutoScrollPropertyChanged));

    private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ScrollViewer scrollViewer = d as ScrollViewer;

      if (scrollViewer != null)
        scrollViewer.ScrollToEnd();
    }
    #endregion

    #region TextBox MessageCaret
    public static int GetMessageCaret(DependencyObject obj)
    {
      return (int)obj.GetValue(MessageCaretProperty);
    }

    public static void SetMessageCaret(DependencyObject obj, int value)
    {
      obj.SetValue(MessageCaretProperty, value);
    }

    public static readonly DependencyProperty MessageCaretProperty = DependencyProperty.RegisterAttached(
      "MessageCaret",
      typeof(int),
      typeof(AttachedHelper),
      new PropertyMetadata(MessageCaretPropertyChanged));

    private static void MessageCaretPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TextBox textBox = d as TextBox;

      if (textBox != null)
      {
        textBox.CaretIndex = GetMessageCaret(d);
      }
    }
    #endregion
  }
}
