using System;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // SCRIPT FUNCTION
    // =========================================================
    // Represents a function written in the Romin language.
    //
    // A ScriptFunction does not contain the function's bytecode
    // directly. Instead, it stores the address of the first
    // instruction in the module's bytecode.
    public class ScriptFunction
    {
        // Address of the first bytecode instruction
        // belonging to this function.
        public int Address;

        // Number of parameters expected by the function.
        public int ParamCount;

        // Environment captured by the function.
        //
        // This can later be used to implement closures,
        // allowing a function to access variables from
        // the environment where it was created.
        public Env Closure;

        // Module containing the function's bytecode.
        public Module Module;

        // Creates a new script function.
        //
        // 'address' points to the beginning of the function body.
        // 'paramCount' specifies how many parameters it accepts.
        public ScriptFunction(int address, int paramCount)
        {
            Address = address;
            ParamCount = paramCount;
        }
    }

    // =========================================================
    // NATIVE FUNCTION
    // =========================================================
    // Represents a function implemented directly in C#.
    //
    // Native functions allow the Romin VM to call host/.NET
    // functionality without generating Romin bytecode for it.
    //
    // Examples can include:
    // - print();
    // - load();
    // - system functions;
    // - host-specific functions.
    public class NativeFunction : ICallable
    {
        // Delegate containing the actual C# implementation
        // of the native function.
        private Func<List<Value>, Value> _fn;

        /// <summary>
        /// Creates an embedded native function.
        /// </summary>
        /// <param name="fn">
        /// C# delegate that receives the function arguments
        /// and returns a Romin Value.
        /// </param>
        public NativeFunction(Func<List<Value>, Value> fn)
        {
            _fn = fn;
        }

        // Executes the native function.
        //
        // The VM instance is passed to allow native functions
        // to access VM state or services when required.
        // The actual delegate currently uses only the arguments.
        public Value Invoke(VM vm, List<Value> args)
        {
            return _fn(args);
        }
    }

    // =========================================================
    // CALLABLE
    // =========================================================
    // Common interface for objects that can be invoked
    // as functions by the VM.
    //
    // Both script functions and native functions can use
    // this interface so the VM can treat callable objects
    // uniformly.
    public interface ICallable
    {
        // Invokes the callable object.
        //
        // 'vm' is the current virtual machine.
        // 'args' contains the arguments supplied by the caller.
        Value Invoke(VM vm, List<Value> args);
    }
}
