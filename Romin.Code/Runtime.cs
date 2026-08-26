using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Romin
{

    // =========================================================
    // RUNTIME
    // =========================================================
    // Provides runtime integration between the Romin VM and the
    // .NET runtime.
    //
    // The class is responsible for:
    // - resolving .NET methods, properties and fields;
    // - invoking .NET methods;
    // - getting and setting .NET properties and fields;
    // - caching reflection information;
    // - compiling reflection calls into fast delegates.
    public class Runtime
    {
        #region Delegates

        // Delegate used for invoking a .NET method.
        // target contains the object instance for instance methods.
        // For static methods target is null.
        public delegate object FastMethod(
            object target,
                object[] args);

        // Delegate used for reading a property or field.
        public delegate object FastGetter(
            object target);

        // Delegate used for writing a property.
        public delegate void FastSetter(
            object target,
            object value);

        #endregion

        #region Cash

        // Cache of compiled method delegates.
        // Once a method has been compiled into a delegate,
        // the delegate can be reused without reflection overhead.
        static readonly Dictionary<MethodKey, FastMethod>
            Methods = new();

        // Cache of resolved MethodInfo objects.
        // Used to avoid searching through methods repeatedly.
        static readonly Dictionary<MethodKey, MethodInfo>
            MethodCache = new();

        // Cache containing all methods available for a specific type.
        // This avoids repeated calls to Type.GetMethods().
        static readonly Dictionary<Type, MethodInfo[]>
            MethodsByType = new();

        // Cache of compiled property/field getters.
        static readonly Dictionary<PropertyKey, FastGetter>
            Getters = new();

        // Cache of compiled property setters.
        static readonly Dictionary<PropertyKey, FastSetter>
            Setters = new();

        // Returns all public instance and static methods of a type.
        // The result is cached after the first lookup.
        static MethodInfo[] GetMethodsCached(Type type)
        {
            if (!MethodsByType.TryGetValue(type, out var methods))
            {
                methods = type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static);

                MethodsByType[type] = methods;
            }
            return methods;
        }

        // Cache that stores what kind of member was found:
        // property, field or method.
        static readonly Dictionary<MemberKey, MemberKind>
            MemberCache = new();

        #endregion

        #region Compile

        // =========================================================
        // COMPILE METHOD
        // =========================================================
        // Converts a MethodInfo into a compiled delegate.
        //
        // Instead of calling MethodInfo.Invoke() every time,
        // the runtime builds an expression tree and compiles it
        // into executable code.
        static FastMethod CompileMethod(MethodInfo method)
        {
            // Parameter representing the target object.
            var targetExp =
                Expression.Parameter(typeof(object));

            // Parameter representing the method arguments.
            var argsExp =
                Expression.Parameter(typeof(object[]));

            var parameters =
                method.GetParameters();

            // Check whether the last parameter is a params array.
            bool hasParams =
                parameters.Length > 0 &&
                Attribute.IsDefined(parameters[^1], typeof(ParamArrayAttribute));

            // Expressions representing converted method arguments.
            var callArgs =
                new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                // Get argument i from the object[] array.
                var arg = Expression.ArrayIndex(argsExp, Expression.Constant(i));

                // Convert the argument to the actual parameter type.
                callArgs[i] =
                    Expression.Convert(arg, parameters[i].ParameterType);
            }

            Expression instance = null;

            // Instance methods require a target object.
            // Static methods do not have an instance.
            if (!method.IsStatic)
            {
                instance = Expression.Convert(
                    targetExp,
                    method.DeclaringType);
            }

            // Build the actual method call expression.
            var call =
                Expression.Call(
                    instance,
                    method,
                    callArgs);

            Expression body;

            // Methods returning void are converted into a delegate
            // that returns null.
            if (method.ReturnType == typeof(void))
            {
                body = Expression.Block(
                    call,
                    Expression.Constant(null));
            }
            else
            {
                // Convert any return value to object.
                body = Expression.Convert(
                    call,
                    typeof(object));
            }

            // Compile the expression tree into a callable delegate.
            return Expression
                .Lambda<FastMethod>(
                    body,
                    targetExp,
                    argsExp)
                .Compile();
        }

        // =========================================================
        // COMPILE PROPERTY GETTER
        // =========================================================
        // Creates a fast delegate for reading a .NET property.
        static FastGetter CompileGetter(PropertyInfo prop)
        {
            // Target object parameter.
            var target =
                Expression.Parameter(typeof(object));

            Expression property;

            // Static properties do not require an instance.
            if (prop.GetMethod.IsStatic)
            {
                property =
                    Expression.Property(
                        null,
                        prop);
            }
            else
            {
                // Convert the generic object target to the
                // actual declaring type.
                var instance =
                    Expression.Convert(
                        target,
                        prop.DeclaringType);

                property =
                    Expression.Property(
                        instance,
                        prop);
            }

            // Convert the property value to object.
            var convert =
                Expression.Convert(
                    property,
                    typeof(object));

            // Compile the getter into a delegate.
            return Expression
                .Lambda<FastGetter>(
                    convert,
                    target)
                .Compile();
        }

        // =========================================================
        // COMPILE PROPERTY SETTER
        // =========================================================
        // Creates a fast delegate for assigning a .NET property.
        static FastSetter CompileSetter(PropertyInfo prop)
        {
            // Target object parameter.
            var target =
                Expression.Parameter(typeof(object));

            // New property value.
            var value =
                Expression.Parameter(typeof(object));

            // Convert target to the declaring type.
            var instance =
                Expression.Convert(
                    target,
                    prop.DeclaringType);

            // Convert the supplied value to the property type.
            var convertedValue =
                Expression.Convert(
                    value,
                    prop.PropertyType);

            // Build property assignment expression.
            var body =
                Expression.Assign(
                    Expression.Property(
                        instance,
                        prop),
                    convertedValue);

            // Compile assignment into a delegate.
            return Expression
                .Lambda<FastSetter>(
                    body,
                    target,
                    value)
                .Compile();
        }

        // =========================================================
        // COMPILE FIELD GETTER
        // =========================================================
        // Creates a fast delegate for reading a public field.
        static FastGetter CompileFieldGetter(FieldInfo field)
        {
            var target =
                Expression.Parameter(typeof(object));

            Expression fieldExp;

            // Static fields do not require an instance.
            if (field.IsStatic)
            {
                fieldExp =
                    Expression.Field(
                        null,
                        field);
            }
            else
            {
                // Convert target to the declaring type.
                var instance =
                    Expression.Convert(
                        target,
                        field.DeclaringType);

                fieldExp =
                    Expression.Field(
                        instance,
                        field);
            }

            // Convert the field value to object.
            var convert =
                Expression.Convert(
                    fieldExp,
                    typeof(object));

            // Compile the field getter.
            return Expression
                .Lambda<FastGetter>(
                    convert,
                    target)
                .Compile();
        }

        // =========================================================
        // DETERMINE MEMBER KIND
        // =========================================================
        // Determines whether a member is a property, field or method.
        static MemberKind CompileMember(Type type, string name)
        {
            // PROPERTY
            // Check for a public instance or static property.
            if (type.GetProperty(name,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static) != null)
            {
                return MemberKind.Property;
            }

            // FIELD
            // Check for a public instance or static field.
            if (type.GetField(name,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static) != null)
            {
                return MemberKind.Field;
            }

            // METHOD 
            // Search all cached methods for the requested name.
            var methods = GetMethodsCached(type);

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == name)
                    return MemberKind.Method;
            }

            // No supported member was found.
            throw new Exception($"Member '{name}' not found");
        }

        #endregion

        #region Resolve

        /// <summary>
        /// Finds a .NET method matching the specified name
        /// and argument types.
        /// </summary>
        /// <param name="type">Type containing the method.</param>
        /// <param name="name">Method name.</param>
        /// <param name="args">Arguments supplied by Romin.</param>
        /// <returns>Matching MethodInfo or null.</returns>
        public static MethodInfo ResolveMethod(
            Type type,
            string name,
            object[] args)
        {
            // Get methods from the type cache.
            var methods = GetMethodsCached(type);

            foreach (var method in methods)
            {
                // Ignore methods with a different name.
                if (method.Name != name)
                    continue;

                var parameters = method.GetParameters();

                // Check whether the method uses params T[].
                bool hasParams =
                    parameters.Length > 0 &&
                    parameters[^1].IsDefined(typeof(ParamArrayAttribute), false);

                // Ordinary method requires exactly the same
                // number of arguments.
                if (!hasParams)
                {
                    if (parameters.Length != args.Length)
                        continue;
                }
                else
                {
                    // A params method requires at least all fixed arguments.
                    if (args.Length < parameters.Length - 1)
                        continue;
                }

                bool valid = true;

                // Validate every supplied argument.
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];

                    // params T[]
                    if (param.IsDefined(typeof(ParamArrayAttribute), false))
                    {
                        var elemType = param.ParameterType.GetElementType();

                        // Check all remaining arguments against
                        // the params element type.
                        for (int j = i; j < args.Length; j++)
                        {
                            if (!CanConvert(args[j], elemType))
                            {
                                valid = false;
                                break;
                            }
                        }
                        break;
                    }

                    // ordinary parameter
                    if (i >= args.Length)
                    {
                        valid = false;
                        break;
                    }

                    // Check whether the supplied value can be converted
                    // to the required parameter type.
                    if (!CanConvert(args[i], param.ParameterType))
                    {
                        valid = false;
                        break;
                    }
                }

                // Return the first compatible method.
                if (valid)
                    return method;
            }

            // No compatible method was found.
            return null;
        }

        /// <summary>
        /// Checks whether a runtime value can be converted
        /// to the specified .NET type.
        /// </summary>
        static bool CanConvert(object value, Type targetType)
        {
            // Null is valid for reference types and nullable value types.
            if (value == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            var vt = value.GetType();

            // Exact type match.
            if (vt == targetType)
                return true;

            // Every value can be passed as object.
            if (targetType == typeof(object))
                return true;

            // Check normal inheritance/interface compatibility.
            if (targetType.IsAssignableFrom(vt))
                return true;

            // slow path LAST
            // Try normal .NET conversion only after faster checks fail.
            try
            {
                Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Invoke 

        /// <summary>
        /// Converts a variable number of arguments into the
        /// actual params array expected by a .NET method.
        /// </summary>
        static object[] NormalizeParams(MethodInfo method, object[] args)
        {
            var ps = method.GetParameters();

            // Method has no parameters.
            if (ps.Length == 0)
                return args;

            var last = ps[^1];

            // Method does not use params.
            if (!last.IsDefined(typeof(ParamArrayAttribute), false))
                return args;

            // Number of normal fixed parameters.
            int fixedCount = ps.Length - 1;

            if (args.Length < fixedCount)
                throw new Exception("Not enough arguments");

            var elemType = last.ParameterType.GetElementType();

            // Result array contains fixed parameters plus
            // the generated params array.
            var result = new object[ps.Length];

            for (int i = 0; i < fixedCount; i++)
                result[i] = args[i];

            // Calculate how many values belong to params.
            int paramsCount = args.Length - fixedCount;

            // Create an array of the correct element type.
            var arr = Array.CreateInstance(elemType, paramsCount);

            for (int i = 0; i < paramsCount; i++)
                arr.SetValue(args[fixedCount + i], i);

            result[^1] = arr;

            return result;
        }

        /// <summary>
        /// Invokes a .NET method using cached reflection information
        /// and a compiled delegate.
        /// </summary>
        public static object InvokeMethod(Type type, object target, string name, object[] args)
        {
            args ??= Array.Empty<object>();

            // A method is identified by its type, name and
            // number of arguments.
            var key = new MethodKey(type, name, args.Length);

            // METHOD INFO CACHE
            // Resolve the MethodInfo only once for this key.
            if (!MethodCache.TryGetValue(key, out var method))
            {
                method = ResolveMethod(type, name, args);

                if (method == null)
                    throw new Exception(
                        $"Method '{name}' not found");

                MethodCache[key] = method;
            }

            // NORMALIZE PARAMS
            // Convert regular arguments into a params array
            // when required by the method signature.
            var normalizedArgs = NormalizeParams(method, args);

            // FAST DELEGATE CACHE
            // Compile the method into a delegate only once.
            if (!Methods.TryGetValue(key, out var fast))
            {
                fast = CompileMethod(method);
                Methods[key] = fast;
            }

            // Execute the compiled delegate.
            return fast(target, normalizedArgs);
        }

        /// <summary>
        /// Gets a property or field value using cached delegates.
        /// </summary>
        public static object GetProperty(object target, string name)
        {
            // Determine whether the target is a Type or an instance.
            var type = target is Type t ? t : target.GetType();

            // Remember whether the requested member is static.
            bool isStatic = target is Type;

            var key = new PropertyKey(type, name, isStatic);

            // GETTER CACHE
            if (!Getters.TryGetValue(key, out var getter))
            {
                // First try to find a public property.
                var prop = type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.IgnoreCase);

                if (prop != null)
                {
                    // Compile property access into a delegate.
                    getter = CompileGetter(prop);
                    Getters[key] = getter;
                }
                else
                {
                    // If no property exists, try a public field.
                    var field = type.GetField(
                        name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.IgnoreCase);

                    if (field == null)
                        throw new Exception(
                            $"Property or field '{name}' not found");

                    // Compile field access into a delegate.
                    getter = CompileFieldGetter(field);
                    Getters[key] = getter;
                }
            }
            // Static members receive null as their instance.
            //return getter(target is Type ? null : target);

            try
            {
                return getter(target is Type ? null : target);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error getting property '{name}' " +
                    $"from type '{type.FullName}': {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Sets a .NET property using a cached compiled setter.
        /// </summary>
        public static void SetProperty(object target, string name, object value)
        {
            // Determine target type.
            var type = target.GetType();

            bool isStatic = target is Type;

            var key = new PropertyKey(type, name, isStatic);

            // SETTER CACHE
            if (!Setters.TryGetValue(key, out var setter))
            {
                // Find the property by name.
                var prop = type.GetProperty(name);

                if (prop == null)
                    throw new Exception(
                        $"Property '{name}' not found");

                // Compile property assignment into a delegate.
                setter = CompileSetter(prop);
                Setters[key] = setter;
            }

            // Execute the compiled setter.
            setter(target, value);
        }

        /// <summary>
        /// Finds a property, field or method on an object.
        /// The result type is cached for subsequent lookups.
        /// </summary>
        public static object GetMember(object target, string name)
        {
            // Determine whether target is a Type or a normal instance.
            var type = target is Type t ? t : target.GetType();

            var key = new MemberKey(type, name);

            // Resolve member kind only once.
            if (!MemberCache.TryGetValue(key, out var kind))
            {
                kind = CompileMember(type, name);
                MemberCache[key] = kind;
            }

            // Static members use null as their target instance.
            object instance = target is Type ? null : target;

            switch (kind)
            {
                // Property access.
                case MemberKind.Property:
                    return GetProperty(target, name);

                // Field access uses the same getter infrastructure.
                case MemberKind.Field:
                    return GetProperty(target, name);

                // Methods are represented by BoundMethod.
                // The actual invocation happens later.
                case MemberKind.Method:
                    return new BoundMethod(target, type, name);
            }

            throw new Exception();
        }

        /// <summary>
        /// Common runtime method invocation entry point.
        /// Supports both Romin tables and .NET objects.
        /// </summary>
        public static object Invoke(object target, string name, object[] args)
        {
            // Romin tables have their own built-in methods.
            if (target is Table t)
            {
                return InvokeTable(t, name, args);
            }

            // fallback to .NET reflection system
            if (target != null)
            {
                var type = target.GetType();

                // Try to find a compatible .NET method.
                var method = ResolveMethod(type, name, args);

                if (method != null)
                    return InvokeMethod(type, target, name, args);
            }

            // Method could not be found.
            throw new Exception($"Method '{name}' not found on {target}");
        }

        /// <summary>
        /// Invokes built-in methods supported directly by a Romin Table.
        /// </summary>
        static object InvokeTable(Table t, string name, object[] args)
        {
            return name switch
            {
                // Check whether the table contains a specified key.
                "contains" => t.Contains(new Value(args[0])),

                // Check whether the table has a specified value/key
                // according to the Table implementation.
                "has" => t.Has(new Value(args[0])),

                // Return the number of table elements.
                "count" => t.Count(),

                // Remove all table elements.
                "clear" => t.Clear(),

                // Unknown table method.
                _ => throw new Exception($"Table has no method '{name}'")
            };
        }

        #endregion

        #region Key

        // =========================================================
        // METHOD CACHE KEY
        // =========================================================
        // Uniquely identifies a cached method by:
        // - declaring type;
        // - method name;
        // - argument count.
        public readonly struct MethodKey : IEquatable<MethodKey>
        {
            public readonly Type Type;
            public readonly string Name;
            public readonly int ArgCount;

            public MethodKey(Type type, string name, int argCount)
            {
                Type = type;
                Name = name;
                ArgCount = argCount;
            }

            // Compare two method cache keys.
            public bool Equals(MethodKey other)
                => Type == other.Type && Name == other.Name && ArgCount == other.ArgCount;

            // Compare with another object.
            public override bool Equals(object obj)
                => obj is MethodKey other && Equals(other);

            // Generate a hash code for dictionary lookup.
            public override int GetHashCode()
                => HashCode.Combine(Type, Name, ArgCount);
        }

        // =========================================================
        // PROPERTY CACHE KEY
        // =========================================================
        // Identifies a cached property/field getter or setter.
        public readonly struct PropertyKey : IEquatable<PropertyKey>
        {
            public readonly Type Type;
            public readonly string Name;
            public readonly bool Static;

            public PropertyKey(
                Type type,
                string name,
                bool isStatic)
            {
                Type = type;
                Name = name;
                Static = isStatic;
            }

            // Compare two property cache keys.
            public bool Equals(PropertyKey other)
            {
                return Type == other.Type
                    && Name == other.Name
                    && Static == other.Static;
            }

            // Compare with another object.
            public override bool Equals(object obj)
            {
                return obj is PropertyKey other
                    && Equals(other);
            }

            // Generate a hash code for dictionary lookup.
            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Type,
                    Name,
                    Static);
            }
        }

        // =========================================================
        // MEMBER CACHE KEY
        // =========================================================
        // Identifies a cached member by its type and name.
        public readonly struct MemberKey
        {
            public readonly Type Type;
            public readonly string Name;

            public MemberKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            // Generate a hash code for dictionary lookup.
            public override int GetHashCode()
                => HashCode.Combine(Type, Name);

            // Compare two member keys.
            public override bool Equals(object obj)
                => obj is MemberKey other &&
                   other.Type == Type &&
                   other.Name == Name;
        }

        #endregion
    }

    // Describes what kind of .NET member was resolved.
    public enum MemberKind
    {
        Property,
        Field,
        Method
    }

    // =========================================================
    // BOUND METHOD
    // =========================================================
    // Represents a .NET method that has already been associated
    // with a target object.
    //
    // The actual method invocation is performed later by the VM.
    public readonly struct BoundMethod
    {
        // Object on which the method should be invoked.
        // For static methods this can be a Type.
        public readonly object Target;

        // Type containing the method.
        public readonly Type Type;

        // Name of the method.
        public readonly string Name;

        public BoundMethod(object target, Type type, string name)
        {
            Target = target;
            Type = type;
            Name = name;
        }
    }
}
