using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Engine.Model.Common
{
  public static class NotifierGenerator
  {
    private const string InvokeEventPrefix = "invoke_";
    private const string GeneretedTypePostfix = "_genered_type";
    private const string GeneretedModuleName = "notifier_genered_module";
    private const string GeneretedAssemblyName = "notifier_genered_asm";

    private static readonly AssemblyBuilder assemblyBuilder;
    private static readonly ModuleBuilder moduleBuilder;

    private static readonly Dictionary<Type, Type> invokers;
    private static readonly Dictionary<Type, Type> contexts;

    static NotifierGenerator()
    {
      invokers = new Dictionary<Type, Type>();
      contexts = new Dictionary<Type, Type>();

      assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(GeneretedAssemblyName), AssemblyBuilderAccess.Run);
      moduleBuilder = assemblyBuilder.DefineDynamicModule(GeneretedModuleName);
    }

    public static TInterface MakeContext<TInterface>()
    {
      var contextType = MakeContext(typeof(TInterface));
      return (TInterface)Activator.CreateInstance(contextType);
    }

    private static Type MakeContext(Type interfaceType)
    {
      Type resultType;
      if (!contexts.TryGetValue(interfaceType, out resultType))
      {
        var invokeMethod = typeof(NotifierContext).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

        var builder = moduleBuilder.DefineType(interfaceType.Name + GeneretedTypePostfix);
        builder.SetParent(typeof(NotifierContext));
        builder.AddInterfaceImplementation(interfaceType);

        var events = interfaceType.GetEvents();
        foreach (var eventInfo in events)
        {
          var eventType = eventInfo.EventHandlerType;
          var eventGenericArg = eventType.GetGenericArguments()[0];
          var eventFiledInfo = AddEvent(builder, eventInfo, eventGenericArg);

          var parameters = new Type[] { typeof(object), eventGenericArg };

          var methodBuilder = builder.DefineMethod(InvokeEventPrefix + eventInfo.Name, MethodAttributes.Public, null, parameters);
          var il = methodBuilder.GetILGenerator();
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

        resultType = builder.CreateType();
        contexts.Add(interfaceType, resultType);
      }

      return resultType;
    }

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

    private static MethodBuilder GenerateEventMethod(TypeBuilder builder, MethodInfo overriding, MethodInfo helperMethod, FieldInfo eventField)
    {
      var methodParameters = overriding
        .GetParameters()
        .Select(p => p.ParameterType)
        .ToArray();

      var methodBuilder = builder.DefineMethod(overriding.Name, MethodAttributes.Public | MethodAttributes.Virtual, overriding.ReturnType, methodParameters);
      builder.DefineMethodOverride(methodBuilder, overriding);

      var il = methodBuilder.GetILGenerator();
      il.DeclareLocal(eventField.FieldType);

      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldflda, eventField);
      il.Emit(OpCodes.Stloc_0);

      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldloc_0);
      il.Emit(OpCodes.Ldarg_1);
      il.Emit(OpCodes.Callvirt, helperMethod);

      il.Emit(OpCodes.Ret);

      return methodBuilder;
    }

    internal static TInterface MakeInvoker<TInterface>(params object[] context)
    {
      if (!typeof(TInterface).IsInterface)
        throw new InvalidOperationException("TInterface must be interface");

      Type resultType;
      if (!invokers.TryGetValue(typeof(TInterface), out resultType))
      {
        var attributes = (NotifierAttribute[])typeof(TInterface).GetCustomAttributes(typeof(NotifierAttribute), true);
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

        Type contextType;
        if (!contexts.TryGetValue(invokerAttribute.Context, out contextType))
          contextType = MakeContext(invokerAttribute.Context);

        var builder = moduleBuilder.DefineType(typeof(TInterface).Name + GeneretedTypePostfix);
        builder.SetParent(notifierType);
        builder.AddInterfaceImplementation(typeof(TInterface));

        var methods = typeof(TInterface).GetMethods();
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

        resultType = builder.CreateType();
        invokers.Add(typeof(TInterface), resultType);
      }

      var invoker = (Notifier)Activator.CreateInstance(resultType);
      foreach (var ctx in context)
        invoker.Add(ctx);

      return (TInterface)(object)invoker;
    }
  }
}
