using System;
using System.Collections.Generic;

namespace ConsoleServer
{
  public class ArgsParser
  {
    public delegate bool TryParse<T>(string str, out T value);

    private Dictionary<string, string> _params;
    private Action _fallback;

    public ArgsParser(string[] args, Action fallback)
    {
      if (args.Length % 2 != 0)
        throw new InvalidOperationException("invalid args count");

      _params = new Dictionary<string, string>();
      for (int i = 0; i < args.Length; i += 2)
        _params.Add(args[i], args[i + 1]);

      _fallback = fallback;
    }

    public string GetOrDefault(string key, string def)
    {
      if (!_params.TryGetValue(key, out string value))
        return def;
      return value;
    }

    public T GetOrDefault<T>(string key, TryParse<T> parser, T def)
    {
      var valueStr = GetOrDefault(key, null);
      if (valueStr == null)
        return def;

      if (!parser(valueStr, out T value))
        return def;
      return value;
    }

    public string Get(string key)
    {
      if (!_params.TryGetValue(key, out string value))
        throw CallFallbackAndCreateException("Param not found: " + key);
      return value;
    }

    public T Get<T>(string key, TryParse<T> parser)
    {
      var valueStr = Get(key);
      if (!parser(valueStr, out T value))
        throw CallFallbackAndCreateException("Can't cast " + key + " to " + typeof(T).Name);
      return value;
    }

    private Exception CallFallbackAndCreateException(string message)
    {
      _fallback();
      return new InvalidOperationException(message);
    }
  }
}
