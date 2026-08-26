using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // INSTRUCTION
    // =========================================================
    // Represents a single bytecode instruction executed by the VM.
    //
    // An instruction contains:
    // - an operation code (OpCode);
    // - an integer argument;
    // - an optional Value;
    // - source code location information for debugging.
    //
    // The VM reads these instructions sequentially and performs
    // the corresponding operation.
    public struct Instruction
    {
        // Operation that the VM must execute.
        public OpCode Op;

        // Integer argument associated with the operation.
        //
        // Depending on the OpCode, this can represent:
        // - a constant/local/global index;
        // - a jump target;
        // - a function address;
        // - an argument count;
        // - another operation-specific value.
        public int Arg;

        // Optional value associated with the instruction.
        //
        // For example, LoadConst can store the actual constant
        // inside this field.
        public Value Value;

        // Source code line where this instruction was generated.
        public int Line;

        // Source code column where this instruction was generated.
        public int Column;

        // Source file from which this instruction originated.
        public string File;

        // =========================================================
        // CONSTRUCTORS
        // =========================================================

        // Creates an instruction containing only an operation code.
        //
        // Arg is initialized to zero and Value to its default value.
        public Instruction(OpCode op)
        {
            Op = op;
            Arg = 0;
            Value = default;
        }

        // Creates an instruction with an operation code
        // and an integer argument.
        public Instruction(OpCode op, int arg)
        {
            Op = op;
            Arg = arg;
            Value = default;
        }

        // Creates an instruction with an operation code
        // and a Value operand.
        public Instruction(OpCode op, Value value)
        {
            Op = op;
            Value = value;
            Arg = 0;
        }

        // =========================================================
        // DEBUG REPRESENTATION
        // =========================================================

        // Returns a compact textual representation of the instruction.
        //
        // This is useful when displaying or debugging generated
        // bytecode.
        public override string ToString()
        {
            return $"{Op} {Arg} {Value}";
        }
    }
}
