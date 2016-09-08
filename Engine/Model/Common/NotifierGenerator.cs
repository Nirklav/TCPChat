using System;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<Type, Type> _contexts;

    [SecuritySafeCritical]
    static NotifierGenerator()
    {
      _invokers = new ConcurrentDictionary<Type, Type>();
      _contexts = new ConcurrentDictionary<Type, Type>();

      _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(GeneretedAssemblyName), AssemblyBuilderAccess.Run);
      _moduleBuilder = _assemblyBuilder.DefineDynamicModule(GeneretedModuleName);
    }

    #region Context

    /// <summary>
    /// Создает тип, на основе интерфейса.
    /// Содержащий в себе набор событий, а также методов для их вызова.
    /// </summary>
    /// <typeparam name="TInterface">Интерфейс, объявляющий какие события должны быть реализованы.</typeparam>
    /// <returns>Экземпляр созданного типа, на основе интерфейса.</returns>
    [SecuritySafeCritical]
    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    public static TInterface MakeContext<TInterface>()
    {
      var interfaceType = typeof(TInterface);

      if (!interfaceType.IsInterface)
        throw new InvalidOperationException("TInterface must be an interface");

      var contextType = _contexts.GetOrAdd(interfaceType, CreateContext);
      return (TInterface)Activator.CreateInstance(contextType);
    }

    [SecurityCritical]
    private static Type CreateContext(Type interfaceType)
    {
      var invokeMethod = typeof(NotifierContext).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

      var builder = _moduleBuilder.DefineType(interfaceType.Name + GeneretedTypePostfix);
      builder.AddInterfaceImplementation(interfaceType);
      builder.SetParent(typeof(NotifierContext));

      var events = interfaceType.GetEvents();
      foreach (var eventInfo in events)
      {
        var eventType = eventInfo.EventHandlerType;
        var eventGenericArg = eventType.GetGenericArguments()[0];

        var eventFiledInfo = AddEvent(builder, eventInfo, eventGenericArg);

        var parameters = new Type[] { typeof(object), eventGenericArg };

        var invokeMethodBuilder = builder.DefineMethod(InvokeEventPrefix + eventInfo.Name, MethodAttributes.Public, null, parameters);
        var il = invokeMethodBuilder.GetILGenerator();
        il.DeclareLocal(eventType);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, eventFiledInfo);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Callvirt, invokeMethod.MakeGenericMethod(eventGenericArg));
        il.Emit(OpCodes.Ret);
      }

      return builder.CreateType();
    }

    [SecurityCritical]
    private static FieldInfo AddEvent(TypeBuilder builder, EventInfo info, Type eventGenericArg)
    {
      var helperAddMethod = typeof(NotifierContext).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
      helperAddMethod = helperAddMethod.MakeGenericMethod(eventGenericArg);

      var helperRemoveMethod = typeof(NotifierContext).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic);
      helperRemoveMethod = helperRemoveMethod.MakeGenericMethod(eventGenericArg);

      var eventFieldBuilder = builder.DefineField(info.Name, info.EventHandlerType, FieldAttributes.Private);

      var addMethod = GenerateEventMethod(builder, info.GetAddMethod(), helperAddMethod, eventFieldBuilder);
      var removeMethod = GenerateEventMethod(builder, info.GetRemoveMethod(), helperRemoveMethod, eventFieldBuilder);

      return eventFieldBuilder;
    }

    [SecurityCritical]
    private static MethodBuilder GenerateEventMethod(TypeBuilder builder, MethodInfo overriding, MethodInfo helperMethod, FieldInfo eventField)
    {
      var methodParameters = overriding
        .GetParameters()
        .Select(p => p.ParameterType)
        .ToArray();

      var methodBuilder = builder.DefineMethod(overriding.Name, MethodAttributes.Public | MethodAttributes.Virtual, overriding.ReturnType, methodParameters);
      builder.DefineMethodOverride(methodBuilder, overriding);

      var il = methodBuilder.GetILGenerator();
  
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldflda, eventField);
      il.Emit(OpCodes.Ldarg_1);
      il.Emit(OpCodes.Callvirt, helperMethod);

      il.Emit(OpCodes.Ret);

      return methodBuilder;
    }

    #endregion

    #region Invoker

    /// <summary>
    /// Создает тип, на основе интерфейса.
    /// Содержит в себе набор контекстов с событиями, а также методы.
    /// При вызове которых будут вызыватся события у контекстов.
    /// </summary>
    /// <typeparam name="TInterface">Интерфейс объявлящий набор методов.</typeparam>
    /// <param name="context">
    /// Конексты которые будут сразу помещены в экземпляп типа. 
    /// Для добавления новых экземпляр необходимо привести к типу <typeparamref name="Notifier"/>
    /// </param>
    /// <returns>Экземпляр созданного типа, на основе интерфейса.</returns>
    [SecurityCritical]
    internal static TInterface MakeInvoker<TInterface>(params object[] context)
    {
      var interfaceType = typeof(TInterface);

      if (!interfaceType.IsInterface)
        throw new InvalidOperationException("TInterface must be an interface");

      var invokerType = _invokers.GetOrAdd(interfaceType, CreateInvoker);
      var invoker = (Notifier)Activator.CreateInstance(invokerType);
      foreach (var ctx in context)
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

      var contextsGetMethod = notifierType.GetMethod("GetContexts", BindingFlags.Instance | BindingFlags.Public);
      var arrayGetMethod = typeof(object[]).GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(int) }, null);

      var contextType = _contexts.GetOrAdd(invokerAttribute.Context, CreateContext);

      var builder = _moduleBuilder.DefineType(interfaceType.Name + GeneretedTypePostfix);
      builder.SetParent(notifierType);
      builder.AddInterfaceImplementation(interfaceType);

      var ctorBuilder = builder.DefineDefaultConstructor(MethodAttributes.Public);

      var methods = interfaceType.GetMethods();
      foreach (var method in methods)
      {
        var eventInvoker = contextType.GetMethod(InvokeEventPrefix + method.Name, BindingFlags.Instance | BindingFlags.Public);

        var returnType = method.ReturnType;
        var methodParameters = method
          .GetParameters()
          .Select(p => p.ParameterType)
          .ToArray();

        var methodBuilder = builder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual, returnType, methodParameters);
        builder.DefineMethodOverride(methodBuilder, method);

        var il = methodBuilder.GetILGenerator();
        var cycleLabel = il.DefineLabel();
        var donotExitLabel = il.DefineLabel();

        il.DeclareLocal(typeof(object[]));
        il.DeclareLocal(typeof(int));
        il.DeclareLocal(contextType);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, contextsGetMethod);
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Stloc_1);

        il.MarkLabel(cycleLabel);

        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Brtrue_S, donotExitLabel);
        il.Emit(OpCodes.Ret);

        il.MarkLabel(donotExitLabel);

        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Stloc_1);

        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Callvirt, arrayGetMethod);
        il.Emit(OpCodes.Stloc_2);

        il.Emit(OpCodes.Ldloc_2);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Callvirt, eventInvoker);

        il.Emit(OpCodes.Br, cycleLabel);
      }

      return builder.CreateType();
    }

    #endregion
  }
}
