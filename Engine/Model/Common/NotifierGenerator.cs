using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;

namespace Engine.Model.Common
{
  public static class NotifierGenerator
  {
    private const string InvokeEventPrefix = "invoke_";
    private const string GeneretedTypePostfix = "_generated_type";
    private const string GeneretedModuleName = "notifier_generated_module";
    private const string GeneretedAssemblyName = "notifier_generated_asm";

    private static readonly AssemblyBuilder _assemblyBuilder;
    private static readonly ModuleBuilder _moduleBuilder;

    private static readonly ConcurrentDictionary<Type, Type> _invokers;
    private static readonly ConcurrentDictionary<Type, Type> _events;

    [SecuritySafeCritical]
    static NotifierGenerator()
    {
      _invokers = new ConcurrentDictionary<Type, Type>();
      _events = new ConcurrentDictionary<Type, Type>();

      _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(GeneretedAssemblyName), AssemblyBuilderAccess.Run);
      _moduleBuilder = _assemblyBuilder.DefineDynamicModule(GeneretedModuleName);
    }

    #region Events

    /// <summary>
    /// Сreates a type based on the interface.
    /// Containing a set of events, and methods for their call.
    /// </summary>
    /// <typeparam name="TInterface">Interface that declares the event.</typeparam>
    /// <returns>Returns object that implement interface.</returns>
    [SecuritySafeCritical]
    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    public static TInterface MakeEvents<TInterface>()
    {
      var interfaceType = typeof(TInterface);

      if (!interfaceType.IsInterface)
        throw new InvalidOperationException("TInterface must be an interface");

      var eventsType = _events.GetOrAdd(interfaceType, CreateEvents);
      return (TInterface)Activator.CreateInstance(eventsType);
    }

    [SecurityCritical]
    private static Type CreateEvents(Type interfaceType)
    {
      var invokeMethod = typeof(NotifierEvents).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

      var builder = _moduleBuilder.DefineType(interfaceType.Name + GeneretedTypePostfix);
      builder.AddInterfaceImplementation(interfaceType);
      builder.SetParent(typeof(NotifierEvents));

      foreach (var eventInfo in interfaceType.GetEvents())
      {
        var eventType = eventInfo.EventHandlerType;
        if (eventType.GetGenericTypeDefinition() != typeof(EventHandler<>))
          throw new InvalidOperationException("Event should be EventHanler<T>");

        var eventGenericArg = eventType.GetGenericArguments()[0];

        var eventFiledInfo = AddEvent(builder, eventInfo, eventGenericArg);

        var parameters = new[] { typeof(object), eventGenericArg, typeof(Action<Exception>) };
        var invokeBuilder = builder.DefineMethod(InvokeEventPrefix + eventInfo.Name, MethodAttributes.Public, null, parameters);

        var il = invokeBuilder.GetILGenerator();

        // Load field
        il.Emit(OpCodes.Ldarg_0);               // Load events object (this)
        il.Emit(OpCodes.Dup);                   // Duplicate events object (for field load)
        il.Emit(OpCodes.Ldfld, eventFiledInfo); // Load event
        il.Emit(OpCodes.Ldarg_1);               // Load sender
        il.Emit(OpCodes.Ldarg_2);               // Load args
        il.Emit(OpCodes.Ldarg_3);               // Load callback
        il.Emit(OpCodes.Callvirt, invokeMethod.MakeGenericMethod(eventGenericArg));
        il.Emit(OpCodes.Ret);
      }

      return builder.CreateType();
    }

    [SecurityCritical]
    private static FieldInfo AddEvent(TypeBuilder builder, EventInfo info, Type eventGenericArg)
    {
      var eventFieldBuilder = builder.DefineField(info.Name, info.EventHandlerType, FieldAttributes.Private);

      var add = typeof(NotifierEvents).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(eventGenericArg);
      GenerateEventMethod(builder, info.GetAddMethod(), add, eventFieldBuilder);

      var remove = typeof(NotifierEvents).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(eventGenericArg);
      GenerateEventMethod(builder, info.GetRemoveMethod(), remove, eventFieldBuilder);

      return eventFieldBuilder;
    }

    [SecurityCritical]
    private static void GenerateEventMethod(TypeBuilder builder, MethodInfo overriding, MethodInfo helperMethod, FieldInfo eventField)
    {
      var methodParameters = overriding
        .GetParameters()
        .Select(p => p.ParameterType)
        .ToArray();

      var methodBuilder = builder.DefineMethod(overriding.Name, MethodAttributes.Public | MethodAttributes.Virtual, overriding.ReturnType, methodParameters);
      builder.DefineMethodOverride(methodBuilder, overriding);

      var il = methodBuilder.GetILGenerator();
  
      il.Emit(OpCodes.Ldarg_0);                 // Load events objects (this)
      il.Emit(OpCodes.Dup);                     // Dublicate evenst object (for field load)
      il.Emit(OpCodes.Ldflda, eventField);      // Load field address
      il.Emit(OpCodes.Ldarg_1);                 // Load event arg
      il.Emit(OpCodes.Callvirt, helperMethod);  // Call helper (add or remove)

      il.Emit(OpCodes.Ret);
    }

    #endregion

    #region Invoker

    /// <summary>
    /// Creates type based on the interface.
    /// That contains set of objects with events.
    /// And methods that invoke events.
    /// </summary>
    /// <typeparam name="TInterface">Inteface that declares methods.</typeparam>
    /// <param name="events">
    /// Objects with events that be added to inovker.
    /// </param>
    /// <returns>Returns object that implement interface.</returns>
    [SecurityCritical]
    internal static TInterface MakeInvoker<TInterface>(params object[] events)
    {
      var interfaceType = typeof(TInterface);

      if (!interfaceType.IsInterface)
        throw new InvalidOperationException("TInterface must be an interface");

      var invokerType = _invokers.GetOrAdd(interfaceType, CreateInvoker);
      var invoker = (Notifier)Activator.CreateInstance(invokerType);
      foreach (var ctx in events)
        invoker.Add(ctx);

      return (TInterface)(object)invoker;
    }

    [SecurityCritical]
    private static Type CreateInvoker(Type interfaceType)
    {
      var attributes = (NotifierAttribute[])interfaceType.GetCustomAttributes(typeof(NotifierAttribute), true);
      if (attributes == null || attributes.Length == 0)
        throw new InvalidOperationException("InvokerAttribute not finded");

      var invokerAttribute = attributes.First();
      var notifierType = typeof(Notifier);
      if (invokerAttribute.BaseNotifier != null)
      {
        if (!typeof(Notifier).IsAssignableFrom(invokerAttribute.BaseNotifier))
          throw new InvalidOperationException("BaseInvoker should inherit from Notifier");

        notifierType = invokerAttribute.BaseNotifier;
      }

      var getEvents = notifierType.GetMethod("GetEvents", BindingFlags.Instance | BindingFlags.Public);
      var getEnumerator = typeof(IEnumerable<object>).GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
      var moveNext = typeof(IEnumerator).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public);
      var current = typeof(IEnumerator<object>).GetProperty("Current", BindingFlags.Instance | BindingFlags.Public);
      var dispose = typeof(IDisposable).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public);

      var eventsType = _events.GetOrAdd(invokerAttribute.Events, CreateEvents);

      var builder = _moduleBuilder.DefineType(interfaceType.Name + GeneretedTypePostfix);
      builder.SetParent(notifierType);
      builder.AddInterfaceImplementation(interfaceType);
      builder.DefineDefaultConstructor(MethodAttributes.Public);

      var methods = interfaceType.GetMethods();
      foreach (var method in methods)
      {
        var eventInvoker = eventsType.GetMethod(InvokeEventPrefix + method.Name, BindingFlags.Instance | BindingFlags.Public);

        var returnType = method.ReturnType;
        var methodParameters = method
          .GetParameters()
          .Select(p => p.ParameterType)
          .ToArray();

        if (methodParameters.Length == 1)
        {
          if (!typeof(EventArgs).IsAssignableFrom(methodParameters[0]))
            throw new InvalidOperationException("First argument should be EventArgs");
        }
        else if (methodParameters.Length == 2)
        {
          if (!typeof(EventArgs).IsAssignableFrom(methodParameters[0]))
            throw new InvalidOperationException("First argument should be EventArgs");

          if (typeof(Action<Exception>) != methodParameters[1])
            throw new InvalidOperationException("Second argument should be Action<Exception>");
        }
        else
          throw new InvalidOperationException("Invalid parameters count");

        var methodBuilder = builder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual, returnType, methodParameters);
        builder.DefineMethodOverride(methodBuilder, method);

        var il = methodBuilder.GetILGenerator();

        var cycleLabel = il.DefineLabel();
        var donotExitLabel = il.DefineLabel();
        var exitLabel = il.DefineLabel();

        il.DeclareLocal(typeof(IEnumerator<object>));

        // Set enumerator local
        il.Emit(OpCodes.Ldarg_0);                 // Load notifier
        il.Emit(OpCodes.Callvirt, getEvents);     // Call get event method (returns IEnumearable)
        il.Emit(OpCodes.Callvirt, getEnumerator); // Call get enumerator method
        il.Emit(OpCodes.Stloc_0);                 // Set enumerator to local

        il.BeginExceptionBlock();

        il.MarkLabel(cycleLabel);

        il.Emit(OpCodes.Ldloc_0);                  // Load enumerator
        il.Emit(OpCodes.Callvirt, moveNext);       // Call move next
        il.Emit(OpCodes.Brtrue_S, donotExitLabel); // Check move next result, if false - break method
        il.Emit(OpCodes.Leave_S, exitLabel);       // Exit and call finally block

        il.MarkLabel(donotExitLabel);

        il.Emit(OpCodes.Ldloc_0);                          // Load enumerator
        il.Emit(OpCodes.Callvirt, current.GetGetMethod()); // Get current
        il.Emit(OpCodes.Ldnull);                           // Load null (sender)
        il.Emit(OpCodes.Ldarg_1);                          // Load args

        if (methodParameters.Length == 1)                  // Load callback
          il.Emit(OpCodes.Ldnull);  
        else
          il.Emit(OpCodes.Ldarg_2);

        il.Emit(OpCodes.Callvirt, eventInvoker);           // Call invoker

        il.Emit(OpCodes.Br, cycleLabel);                   // Process next element

        il.BeginFinallyBlock();

        // Call enumerator dispose
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Callvirt, dispose);

        il.EndExceptionBlock();

        il.MarkLabel(exitLabel);
        il.Emit(OpCodes.Ret);
      }

      return builder.CreateType();
    }

    #endregion
  }
}
