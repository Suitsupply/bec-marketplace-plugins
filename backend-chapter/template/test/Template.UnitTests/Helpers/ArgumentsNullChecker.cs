using System.Reflection;
using AutoFixture.Kernel;

namespace Template.UnitTests.Helpers;

public static class ArgumentsNullChecker
{
    private static readonly IFixture Fixture = new Fixture().Customize(new AutoMoqCustomization());

    public static void CheckConstructorAndMethodsParameters<T>(T instance) where T : class
    {
        CheckConstructorParameters<T>();
        CheckMethodParameters(instance);
    }

    public static void CheckConstructorParameters<T>()
    {
        var constructors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var nullabilityContext = new NullabilityInfoContext();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (!TryCreateDefaultArguments(parameters, out var defaultArguments))
                continue;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.HasDefaultValue || !(parameter.ParameterType.IsInterface || parameter.ParameterType.IsClass))
                    continue;

                if (nullabilityContext.Create(parameter).WriteState == NullabilityState.Nullable)
                    continue;

                var argument = defaultArguments[i];
                foreach (var invalidArgument in GetNullOrEmptyStringVariations(argument))
                {
                    var invalidArguments = defaultArguments.Select((v, j) => i == j ? invalidArgument : v).ToArray();

                    Action action = () =>
                    {
                        try
                        {
                            constructor.Invoke(invalidArguments);
                        }
                        catch (TargetInvocationException e)
                        {
                            throw e.InnerException ?? e;
                        }
                    };

                    try
                    {
                        Assert.That(Assert.Throws<ArgumentNullException>(action)!.ParamName, Is.EqualTo(parameter.Name));
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(
                            $"The '{typeof(T).FullName}' constructor has an invalid constraint for the '{parameter.Name}' parameter.", error);
                    }
                }
            }
        }
    }

    public static void CheckMethodParameters(object instance)
    {
        var type = instance.GetType();
        CheckMethodParametersCore(type, BindingFlags.Public | BindingFlags.Instance, instance);
    }

    public static void CheckStaticMethodParameters(Type type)
    {
        CheckMethodParametersCore(type, BindingFlags.Public | BindingFlags.Static, instance: null);
    }

    private static void CheckMethodParametersCore(Type type, BindingFlags flags, object? instance)
    {
        var methods = type.GetMethods(flags);
        var nullabilityContext = new NullabilityInfoContext();

        foreach (var methodInfo in methods)
        {
            if (methodInfo.IsSpecialName || methodInfo.DeclaringType != type)
                continue;

            var method = methodInfo;
            if (method.IsGenericMethod)
                method = method.MakeGenericMethod(GetGenericArgumentTypes(method));

            var parameters = method.GetParameters();
            if (!TryCreateDefaultArguments(parameters, out var defaultArguments))
                continue;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.HasDefaultValue || parameter.IsOut || parameter.ParameterType.IsByRef)
                    continue;

                if (!(parameter.ParameterType.IsInterface || parameter.ParameterType.IsClass))
                    continue;

                if (nullabilityContext.Create(parameter).WriteState == NullabilityState.Nullable)
                    continue;

                var argument = defaultArguments[i];
                foreach (var invalidArgument in GetNullOrEmptyStringVariations(argument))
                {
                    var invalidArguments = defaultArguments.Select((v, j) => i == j ? invalidArgument : v).ToArray();

                    Action action = () =>
                    {
                        try
                        {
                            var result = method.Invoke(instance, invalidArguments);
                            if (result is Task { IsFaulted: true, Exception: { } } task)
                                throw task.Exception.InnerException ?? task.Exception;
                        }
                        catch (TargetInvocationException e)
                        {
                            throw e.InnerException ?? e;
                        }
                    };

                    try
                    {
                        Assert.That(Assert.Catch<ArgumentException>(action)!.ParamName, Is.EqualTo(parameter.Name));
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(
                            $"The '{type.FullName}'.{methodInfo.Name} method has an invalid constraint for the '{parameter.Name}' parameter.", error);
                    }
                }
            }
        }
    }

    private static object?[] GetNullOrEmptyStringVariations(object? parameter)
        => parameter is string ? [null, string.Empty, " "] : [null];

    private static bool TryCreateDefaultArguments(ParameterInfo[] parameters, out object?[] arguments)
    {
        arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.HasDefaultValue)
            {
                arguments[i] = p.DefaultValue;
            }
            else if (p.IsOut || p.ParameterType.IsByRef)
            {
                arguments[i] = null;
            }
            else
            {
                try
                {
                    arguments[i] = new SpecimenContext(Fixture).Resolve(p.ParameterType);
                }
                catch
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static Type[] GetGenericArgumentTypes(MethodBase method)
        => [.. method
            .GetGenericArguments()
            .Select(type => type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault() ?? typeof(object))];
}