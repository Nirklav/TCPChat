using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Exceptions
{
  public class ModelException : Exception
  {
    public ErrorCode Code { get; private set; }
    public object State { get; private set; }

    public ModelException(ErrorCode code, string message, Exception inner, object state = null)
      : base(message, inner)
    {
      Code = code;
      State = state;
    }

    public ModelException(ErrorCode code, string message, object state = null)
      : base(message)
    {
      Code = code;
      State = state;
    }

    public ModelException(ErrorCode code, object state = null) 
      : this(code, code.ToString(), state)
    {

    }
  }
}
