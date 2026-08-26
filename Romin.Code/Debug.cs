using System;
using System.Collections.Generic;
using System.Text;

namespace Romin;

// =========================================================
// DEBUG SYMBOLS
// =========================================================
// Provides a place for collecting and processing debug
// information generated during VM execution.
//
// Debug symbols can later be used for:
// - source-level debugging;
// - breakpoints;
// - instruction tracing;
// - mapping bytecode instructions to source code;
// - inspecting VM execution state.
public sealed class DebugSymbols
{
    // Determines whether debug information collection is enabled.
    public bool Enabled { get; set; }

    // Processes a single executed instruction.
    //
    // 'ins' contains the instruction being executed.
    // 'ip' is the instruction pointer of that instruction.
    //
    // The method is currently empty and can later be extended
    // with breakpoint handling, tracing, logging, etc.
    public void Emit(Instruction ins, int ip)
    {

    }
}

// =========================================================
// VM STATE
// =========================================================
// Represents a snapshot of the virtual machine state.
//
// VMState is useful for:
// - debugging;
// - breakpoints;
// - exception diagnostics;
// - execution tracing;
// - inspecting the stack and current call frame;
// - examining global variables at a specific point in time.
public class VMState
{
    // Reference to the VM that produced this state.
    public VM VM { get; init; }

    // Instruction currently being executed.
    public Instruction Instruction { get; init; }

    // Instruction pointer corresponding to the current instruction.
    public int IP { get; init; }

    // Current function call frame.
    //
    // Contains information about the active function invocation,
    // such as its execution context and local variables.
    public VM.CallFrame Frame { get; set; }

    // Copy of the VM stack at the moment this state was created.
    //
    // A snapshot allows the stack to be inspected later
    // without depending on the VM's current state.
    public Value[] StackSnapshot { get; init; }

    // Snapshot of global variables at the same point in execution.
    //
    // The dictionary maps global variable names to their values.
    public Dictionary<string, Value> Globals { get; init; }
}
