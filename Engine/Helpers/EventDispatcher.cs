using System;

namespace Engine.Helpers
{
  internal static class EventDispatcher
  {
    public static void BeginDispatch<T>(this EventHandler<T> handler, object sender, T args, Action<Exception> onEnd)
      where T: EventArgs
    {
      if (handler != null)
        ArgsDispatcher<T>.BeginDispatch(handler, sender, args, onEnd);
      else
      {
        if (onEnd != null)
          onEnd(null);
      }
    }

    private static class ArgsDispatcher<T>
      where T : EventArgs
    {
      private class State
      {
        public readonly EventHandler<T> Invoked;
        public readonly Action<Exception> OnEnd;

        public State(EventHandler<T> invoked, Action<Exception> onEnd)
        {
          Invoked = invoked;
          OnEnd = onEnd;
        }
      }

      public static void BeginDispatch(EventHandler<T> handler, object sender, T args, Action<Exception> onEnd)
      {
        handler.BeginInvoke(sender, args, EndDispatch, new State(handler, onEnd));
      }

      private static void EndDispatch(IAsyncResult result)
      {
        Exception exception = null;
        var state = (State)result.AsyncState;
        try
        {
          state.Invoked.EndInvoke(result);
        }
        catch (Exception e)
        {
          exception = e;
        }
        finally
        {
          if (state.OnEnd != null)
            state.OnEnd(exception);
        }
      }
    }
  }
}
