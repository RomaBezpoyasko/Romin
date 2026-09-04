using Romin;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Net.Http;
using System.IO;

using static System.Net.Mime.MediaTypeNames;

namespace Romin
{
    // =========================================================
    // VM HOST
    // =========================================================
    // VMHost is the main bridge between the Romin runtime and
    // the external .NET environment.
    //
    // It is responsible for:
    // - loading Romin modules;
    // - compiling source code;
    // - creating parsers;
    // - running modules in the VM;
    // - expanding base modules;
    // - providing access to .NET system functionality.
    public class VMHost
    {
        // System services available to the Romin runtime.
        public Sys Sys { get; } = new Sys();

        // Cache of already loaded modules.
        //
        // The key is the module file path.
        public Dictionary<string, Module> Modules = new();

        /// <summary>
        /// Load and run module from file path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<Module> LoadModule(string path)
        {
            // Return the already loaded module if it exists.
            if (Modules.TryGetValue(path, out var m))
                return m;

            // Compile the module source into bytecode.
            var module = await Compile(path);

            // Execute the compiled module.
            Run(module);

            // Store the module in the cache.
            Modules[path] = module;

            return module;
        }

        /// <summary>
        /// Load and run module from file path with parameters
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Module> LoadModule(string path, Table parameters)
        {
            if (Modules.TryGetValue(path, out var m))
                return m;

            var module = new Romin.Module();
            foreach (var entry in parameters)
            {
                if (entry.Key.Kind != ValueKind.String)
                    throw new Exception("Module parameter name must be string");

                module.Set(entry.Key.S, entry.Value);
            }
            var code = await GetCode(path);
            var parser = GetParser(code, path);

            parser.Parse(module);
            Run(module);

            Modules[path] = module;

            return module;
        }

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Compile module only to bytecode and return as module
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<Module> Compile(string path)
        {
            var code = await GetCode(path);
            return CompileSource(code, path);
        }

        /// <summary>
        /// Return code from file in folder or from web
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<string> GetCode(string path)
        {
            string code;
            if (IsUrl(path))
            {
                code = await HttpClient.GetStringAsync(path);
            }
            else
            {
                code = await File.ReadAllTextAsync(path);
            }
            return code;
        }

        /// <summary>
        /// True if string is URL
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp
                       || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Do script 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Module CompileSource(string source, string file = null)
        {
            // Create a parser for the source code.
            var parser = GetParser(source, file);

            // Parse the source and generate bytecode.
            return parser.Parse();
        }

        /// <summary>
        /// Get parser with tokens
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Parser GetParser(string source, string file = null)
        {
            // Convert source text into tokens using the lexer.
            var lexer = new Lexer(source, file);
            var tokens = lexer.Tokenize();

            // If the source contains 'base' statements,
            // expand the referenced modules before parsing.
            if (lexer.HasBase)
                tokens = ExpandBase(tokens);

            // Create the parser using the current VM host.
            return new Parser(tokens, this);
        }

        /// <summary>
        /// Run script in module
        /// </summary>
        /// <param name="module"></param>
        public async Task Run(Module module)
        {
            // Create a VM associated with this host.
            var vm = new VM(this);

            // Execute the module's bytecode.
            await vm.Run(module);
        }

        /// <summary>
        /// Load base module in the same context
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="currentDir"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<Token> ExpandBase(
            List<Token> tokens)
        {
            // Create a new token list containing
            // the original source plus expanded base modules.
            var result = new List<Token>();

            for (int i = 0; i < tokens.Count; i++)
            {
                // base "module.rn"
                // When a base statement is found,
                // load and tokenize the referenced source file.
                if (tokens[i].Type == TokenType.Base)
                {
                    i++;

                    // A file name must follow the 'base' keyword.
                    if (tokens[i].Type != TokenType.String)
                        throw new Exception(
                            "Expected file name after base");

                    // Read the referenced file.
                    string file = tokens[i].Value;
                    string text =
                        File.ReadAllText(file);

                    // Tokenize the referenced module.
                    var childTokens =
                        new Lexer(text, file).Tokenize();

                    // Recursively expand nested base statements.
                    childTokens =
                        ExpandBase(childTokens);

                    // Append all tokens except EOF.
                    foreach (var t in childTokens)
                    {
                        if (t.Type != TokenType.EOF)
                            result.Add(t);
                    }

                    continue;
                }

                // Keep ordinary tokens unchanged.
                result.Add(tokens[i]);
            }

            return result;
        }
    }

    // =========================================================
    // ENVIRONMENT
    // =========================================================
    // Env stores variables and their values.
    //
    // An environment can represent:
    // - module/global variables;
    // - local variables;
    // - captured variables for closures.
    //
    // Parent allows environments to form a hierarchy.
    public class Env
    {
        // Optional parent environment.
        public Env Parent;

        // Values stored by numeric slot.
        public Value[] Values;

        // Maps variable names to their numeric slots.
        public Dictionary<string, int> Map;

        // Index of the next available slot.
        private int next;

        // Creates an environment with the specified initial capacity.
        public Env(int capacity = 64)
        {
            Values = new Value[capacity];
            Map = new();
        }

        /// <summary>
        /// Add new value (func, var etc)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public int Add(string name, Value value)
        {
            // Allocate the next available slot.
            int id = next++;

            // Increase the storage array when it becomes full.
            if (id >= Values.Length)
                Array.Resize(ref Values, Values.Length * 2);

            // Store the value.
            Values[id] = value;

            // Associate the variable name with the slot.
            Map[name] = id;

            return id;
        }

        /// <summary>
        /// Add if don't exists and return
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetOrAdd(string name)
        {
            // Return the existing slot if the variable is already known.
            if (Map.TryGetValue(name, out int id))
                return id;

            // Create a new variable initialized with Null.
            return Add(name, Value.Null);
        }

        /// <summary>
        /// Try to get value by name and returns index if true
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool TryGetIndex(string name, out int index)
        {
            // Try to find the variable's storage index.
            return Map.TryGetValue(name, out index);
        }

        /// <summary>
        /// Set value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Set(string name, Value value)
        {
            // Update an existing variable.
            if (Map.TryGetValue(name, out int id))
            {
                Values[id] = value;
                return;
            }

            // Create the variable if it does not exist.
            Add(name, value);
        }

        /// <summary>
        /// Get value by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Value Get(string name)
        {
            // Look for the variable in the current environment.
            if (Map.TryGetValue(name, out int index))
                return Values[index];

            // If it does not exist locally,
            // search in the parent environment.
            if (Parent != null)
                return Parent.Get(name);

            // Unknown variables resolve to Null.
            return Value.Null;
        }

        /// <summary>
        /// Return data as table
        /// </summary>
        /// <param name="includeNull"></param>
        /// <returns></returns>
        public Table ToTable(bool includeNull = false)
        {
            // Convert the environment contents into a Romin Table.
            var table = new Table();

            foreach (var (name, index) in Map)
            {
                var value = Values[index];

                // Skip Null values unless explicitly requested.
                if (!includeNull && value.Kind == ValueKind.Null)
                    continue;

                // Store the variable name as the table key.
                table.Set(new Value(name), value);
            }

            return table;
        }

        /// <summary>
        /// Return Env dictionary for debugging view
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Value> DebugView()
        {
            // Create a simple dictionary representation
            // suitable for debugging tools.
            var dict = new Dictionary<string, Value>();

            // Expose all allocated value slots.
            for (int i = 0; i < Values.Count(); i++)
            {
                dict[$"local_{i}"] = Values[i];
            }

            return dict;
        }
    }

    // =========================================================
    // MODULE
    // =========================================================
    // Represents a compiled Romin module.
    //
    // A module contains:
    // - generated bytecode;
    // - its global environment;
    // - optional debug information.
    public class Module
    {
        // Bytecode instructions generated by the parser.
        public List<Instruction> Code = new();

        // Global environment belonging to this module.
        public Env Env = new();

        // Optional debug symbol information.
        public DebugSymbols Debug { get; set; }

        // Returns the bytecode as a readable multi-line string.
        public string AsList =>
            string.Join("\r\n", Code.Select(r => r.ToString()).ToArray());

        // Register new object by name to be used in code
        public void Set(string name, object obj)
            => Env.Add(name, new Value(obj));

        public void Set(string name, Value v)
            => Env.Add(name, v);
    }

    // =========================================================
    // SYSTEM SERVICES
    // =========================================================
    // Provides access from Romin to .NET reflection,
    // assemblies and runtime types.
    //
    // Sys is responsible for:
    // - loading assemblies;
    // - resolving .NET types;
    // - creating .NET objects;
    // - caching types and constructors.
    public class Sys
    {
        // Assemblies that are allowed to be loaded lazily.
        private readonly HashSet<string> _assemblies = new()
        {
            "System"
        };

        // Cache of resolved types.
        private readonly Dictionary<string, Type> _typeCache = new();

        // Cache of constructors for resolved types.
        private readonly Dictionary<Type, ConstructorInfo[]> _ctorCache = new();

        // Cache of loaded assemblies.
        private readonly Dictionary<string, Assembly> _assemblyCache = new();

        // Synchronization object used by all caches.
        private readonly object _lock = new();

        // =========================================================
        // LOAD ASSEMBLY
        // =========================================================

        // Registers an assembly for lazy loading.
        public void Use(string assembly)
        {
            lock (_lock)
                _assemblies.Add(assembly);
        }

        // =========================================================
        // CREATE INSTANCE
        // =========================================================

        // Creates an instance of a type specified by its name.
        public object New(string type, params Value[] args)
        {
            // Resolve the type first.
            var t = FindCoreType(type);

            // Delegate object creation to the Type overload.
            return New(t, args);
        }

        // Creates an instance of an already resolved .NET type.
        public object New(Type t, params Value[] args)
        {
            // Get all constructors, using the constructor cache.
            var ctors = GetConstructors(t);

            // Keep the last error to provide a meaningful
            // exception when all constructors fail.
            Exception lastError = null;

            foreach (var ctor in ctors)
            {
                // Read constructor parameters.
                var ps = ctor.GetParameters();

                // Ignore constructors with a different argument count.
                if (ps.Length != args.Length)
                    continue;

                try
                {
                    // Convert Romin values into .NET objects
                    // expected by the constructor.
                    var converted = new object[args.Length];

                    for (int i = 0; i < args.Length; i++)
                        converted[i] = Convertor.ConvertArg(args[i], ps[i].ParameterType);

                    // Invoke the constructor using reflection.
                    return ctor.Invoke(converted);
                }
                catch (TargetInvocationException ex)
                {
                    // Unwrap exceptions thrown inside the constructor.
                    lastError = ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    // Save conversion/reflection errors.
                    lastError = ex;
                }
            }

            // At least one constructor was found,
            // but all matching constructors failed.
            if (lastError != null)
                throw new Exception(
                    $"Failed to create '{t}': {lastError.Message}",
                    lastError);

            // No compatible constructor was found.
            throw new Exception($"Cannot construct type {t}", lastError);
        }

        // =========================================================
        // TYPE RESOLVER
        // =========================================================

        // Resolves Romin built-in type names and common .NET types.
        public Type FindCoreType(string name)
        {
            switch (name)
            {
                // Primitive aliases.
                case "string": return typeof(string);
                case "int": return typeof(int);
                case "object": return typeof(object);
                case "DateTime": return typeof(DateTime);
                case "decimal": return typeof(decimal);
                case "bool": return typeof(bool);

                // Common generic collection types.
                case "List": return typeof(List<>);
                case "Dictionary": return typeof(Dictionary<,>);
                case "HashSet": return typeof(HashSet<>);
                case "Queue": return typeof(Queue<>);
                case "Stack": return typeof(Stack<>);
            }

            // If the type is not a built-in type,
            // use the general type resolver.
            return FindType(name);
        }

        // =========================================================
        // FIND TYPE (MAIN CACHE PIPELINE)
        // =========================================================

        // Resolves a .NET type using several lookup strategies.
        public Type FindType(string name)
        {
            // First check the type cache.
            lock (_lock)
            {
                if (_typeCache.TryGetValue(name, out var cached))
                    return cached;
            }

            // 1. CLR direct resolve
            // Try Type.GetType first.
            var type = Type.GetType(name);

            if (type != null)
                return CacheType(name, type);

            // 2. Already loaded assemblies
            // Search through assemblies that are already loaded
            // into the current AppDomain.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = FindTypeInAssembly(asm, name);

                if (type != null)
                    return CacheType(name, type);
            }

            // 3. Lazy loaded assemblies
            // Load registered assemblies only when necessary.
            foreach (var asmName in _assemblies)
            {
                var asm = GetOrLoadAssembly(asmName);

                type = FindTypeInAssembly(asm, name);

                if (type != null)
                    return CacheType(name, type);
            }

            // Type could not be resolved by any strategy.
            throw new Exception($"Type '{name}' not found");
        }

        // =========================================================
        // ASSEMBLY CACHE
        // =========================================================

        // Loads an assembly once and stores it in the cache.
        Assembly GetOrLoadAssembly(string name)
        {
            lock (_lock)
            {
                // Return cached assembly when available.
                if (_assemblyCache.TryGetValue(name, out var asm))
                    return asm;

                try
                {
                    // Load the assembly by its name.
                    asm = Assembly.Load(name);

                    // Cache the loaded assembly.
                    _assemblyCache[name] = asm;

                    return asm;
                }
                catch
                {
                    // Assembly could not be loaded.
                    // Return null so the caller can continue searching.
                    return null;
                }
            }
        }

        // =========================================================
        // FIND TYPE IN ASSEMBLY
        // =========================================================

        // Searches for a type inside a specific assembly.
        Type FindTypeInAssembly(Assembly asm, string name)
        {
            // No assembly means there is nothing to search.
            if (asm == null)
                return null;

            try
            {
                // First try the exact CLR full name.
                var t = asm.GetType(name);

                if (t != null)
                    return t;

                // If exact lookup failed, enumerate available types.
                foreach (var x in SafeGetTypes(asm))
                {
                    if (x == null)
                        continue;

                    // Match either the full name
                    // or the simple type name.
                    if (x.FullName == name ||
                        x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return x;
                    }
                }
            }
            catch { }

            return null;
        }

        // =========================================================
        // SAFE GET TYPES
        // =========================================================

        // Safely obtains all types from an assembly.
        //
        // Some assemblies may throw ReflectionTypeLoadException
        // when only some types can be loaded.
        IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Return only successfully loaded types.
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                // Ignore assemblies that cannot expose their types.
                return Enumerable.Empty<Type>();
            }
        }

        // =========================================================
        // CONSTRUCTOR CACHE
        // =========================================================

        // Returns and caches all public constructors of a type.
        ConstructorInfo[] GetConstructors(Type t)
        {
            lock (_lock)
            {
                // Return cached constructors when available.
                if (_ctorCache.TryGetValue(t, out var cached))
                    return cached;

                // Read constructors using reflection.
                var ctors = t.GetConstructors();

                // Store them for future object creation.
                _ctorCache[t] = ctors;

                return ctors;
            }
        }

        // =========================================================
        // CACHE TYPE
        // =========================================================

        // Stores a successfully resolved type in the type cache.
        Type CacheType(string name, Type t)
        {
            lock (_lock)
                _typeCache[name] = t;

            return t;
        }
    }

    // =========================================================
    // CONVERTOR
    // =========================================================
    // Converts Romin values and ordinary .NET objects
    // into types expected by .NET methods and constructors.
    public class Convertor
    {
        /// <summary>
        /// Convert to proper type
        /// </summary>
        /// <param name="val"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertArg(object val, Type type)
        {
            // Null remains null.
            if (val == null)
                return null;

            // If the object already has the requested type,
            // no conversion is necessary.
            if (type.IsInstanceOfType(val))
                return val;

            // nullable
            // For Nullable<T>, work with the underlying T.
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
                type = underlying;

            // string fast path
            // Strings are converted directly using ToString().
            if (type == typeof(string))
                return val.ToString();

            // avoid repeated ToString calls (cache once)
            // Convert the source value to a string once
            // for operations that require textual representation.
            string str = val as string ?? val.ToString();

            // Convert string to enum.
            if (type.IsEnum)
                return Enum.Parse(type, str);

            // Convert string to Guid.
            if (type == typeof(Guid))
                return Guid.Parse(str);

            // Convert string to Uri.
            if (type == typeof(Uri))
                return new Uri(str);

            // primitives
            // Use the standard .NET conversion mechanism
            // for primitive and compatible types.
            return Convert.ChangeType(val, type);
        }

        /// <summary>
        /// Convert to proper type
        /// </summary>
        /// <param name="val"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ConvertArg(Value val, Type type)
        {
            // Romin Null becomes .NET null.
            if (val.Kind == ValueKind.Null)
                return null;

            // When object is expected, return the underlying object
            // if one exists; otherwise return the Value itself.
            if (type == typeof(object))
                return val.O ?? val;

            // Nullable<T>
            // Work with the underlying type for nullable values.
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
                type = underlying;

            // Convert according to the runtime ValueKind.
            switch (val.Kind)
            {
                // Romin integer.
                case ValueKind.Int:
                    return Convert.ChangeType(val.I, type);

                // Romin floating-point value.
                case ValueKind.Double:
                    return Convert.ChangeType(val.F, type);

                case ValueKind.Decimal:
                    return Convert.ChangeType(val.M, type);

                // Romin boolean.
                case ValueKind.Bool:
                    return Convert.ChangeType(val.I != 0, type);

                // Romin string.
                case ValueKind.String:
                    // No conversion is necessary when a string is expected.
                    if (type == typeof(string))
                        return val.S;

                    // Convert string to enum.
                    if (type.IsEnum)
                        return Enum.Parse(type, val.S);

                    // Convert string to Guid.
                    if (type == typeof(Guid))
                        return Guid.Parse(val.S);

                    // Convert string to Uri.
                    if (type == typeof(Uri))
                        return new Uri(val.S);

                    // Use standard .NET conversion for other types.
                    return Convert.ChangeType(val.S, type);

                // Wrapped .NET object.
                case ValueKind.Object:
                    // Null underlying object.
                    if (val.O == null)
                        return null;

                    // Return the object directly when it already
                    // matches the requested type.
                    if (type.IsInstanceOfType(val.O))
                        return val.O;

                    // Otherwise attempt standard .NET conversion.
                    return Convert.ChangeType(val.O, type);

                // For other ValueKinds, return the underlying object.
                default:
                    return val.O;
            }
        }
    }
}