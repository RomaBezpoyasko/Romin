using System;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // OPCODES
    // =========================================================
    // Defines all bytecode operations supported by the Romin VM.
    //
    // The parser/compiler converts Romin source code into a sequence
    // of these operations. The VM then executes them one by one.
    //
    // Each OpCode represents a specific low-level operation such as:
    // - loading and storing values;
    // - arithmetic and comparisons;
    // - function calls;
    // - table access;
    // - loops and jumps;
    // - object creation;
    // - exception handling.
    public enum OpCode
    {
        // =========================================================
        // BUILTIN FUNCTIONS
        // =========================================================

        // Calls a function implemented directly by the Romin runtime
        // or host environment.
        CallBuiltin,


        // =========================================================
        // STACK / VARIABLES
        // =========================================================

        // Pushes a constant Value onto the VM stack.
        LoadConst,

        // Loads a local variable from the current function frame
        // and pushes it onto the stack.
        LoadLocal,

        // Loads a global variable from the module environment
        // and pushes it onto the stack.
        LoadGlobal,

        // Takes the value from the stack and stores it
        // in a local variable.
        StoreLocal,

        // Takes the value from the stack and stores it
        // in a global variable.
        StoreGlobal,


        // =========================================================
        // TABLES
        // =========================================================

        // Creates a new empty Romin table and pushes it onto
        // the VM stack.
        CreateTable,


        // =========================================================
        // GETTERS / SETTERS
        // =========================================================

        // Reads a value from a table/object using an index or key.
        //
        // Example:
        //     table[key]
        GetIndex,

        // Stores a value in a table/object using an index or key.
        //
        // Example:
        //     table[key] = value
        SetIndex,

        // Sets a value in a table/object without producing
        // an additional result value on the stack.
        SetIndexMute,


        // =========================================================
        // ARITHMETIC
        // =========================================================

        // Adds two values.
        Add,

        // Subtracts the second value from the first value.
        Sub,

        // Multiplies two values.
        Mul,

        // Divides the first value by the second value.
        Div,


        // =========================================================
        // COMPARISON
        // =========================================================

        // Checks whether two values are equal.
        Equal,

        // Checks whether two values are not equal.
        NotEqual,

        // Checks whether the first value is less than the second.
        Less,

        // Checks whether the first value is less than or equal
        // to the second.
        LessEqual,

        // Checks whether the first value is greater than the second.
        Greater,

        // Checks whether the first value is greater than or equal
        // to the second.
        GreaterEqual,


        // =========================================================
        // LOGIC
        // =========================================================

        // Performs logical AND operation.
        And,

        // Performs logical OR operation.
        Or,


        // =========================================================
        // FUNCTIONS
        // =========================================================

        // Creates a function object from compiled function metadata.
        CreateFunction,

        // Calls a function using the specified number of arguments.
        Call,

        // Marks or represents a function-related bytecode operation.
        Func,

        // Returns a value from the current function.
        Return,


        // =========================================================
        // ITERATORS / LOOPS
        // =========================================================

        // Creates a range value used by the iterator.
        //
        // Example:
        //     1..10
        MakeRange,

        // Initializes an iterator for a value.
        ForIterInit,

        // Advances the iterator and produces the next item.
        ForIterNext,


        // =========================================================
        // JUMPS / CONTROL FLOW
        // =========================================================

        // Unconditionally jumps to another bytecode address.
        Jump,

        // Jumps when the value on the stack evaluates to false.
        JumpIfFalse,

        // Jumps when the value on the stack evaluates to true.
        JumpIfTrue,

        // Jumps when the value on the stack is not null.
        JumpIfNotNull,

        // Jumps when the value on the stack is null.
        JumpIfNull,

        // Removes the top value from the VM stack.
        Pop,


        // =========================================================
        // NULL COALESCING / TYPES / OBJECT CREATION
        // =========================================================

        // Performs a null-coalescing operation.
        //
        // Used for expressions such as:
        //     a ?? b
        Coalesce,

        // Loads a .NET type into the VM stack.
        //
        // Used before object construction or other type-based
        // operations.
        LoadType,

        // Creates a new .NET object using a loaded type
        // and constructor arguments.
        New,


        // =========================================================
        // UNARY OPERATORS
        // =========================================================

        // Negates a numeric value.
        //
        // Example:
        //     -value
        Negate,

        // Performs logical NOT.
        //
        // Example:
        //     !value
        Not,


        // =========================================================
        // TRY / CATCH
        // =========================================================

        // Starts an exception-protected region.
        //
        // The argument usually points to the catch handler.
        TryBegin,

        // Marks the end of the protected try region.
        TryEnd,

        // Throws an exception from the VM.
        Throw
    }
}
