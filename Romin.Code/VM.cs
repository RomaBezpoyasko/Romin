using Romin;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Romin
{
    /// <summary>
    /// Executes bytecode instructions for a module.
    /// Maintains a call stack of execution frames.
    /// </summary>
    public class VM
    {
        #region Constructor

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="host"></param>
        public VM(VMHost host)
        {
            Host = host;
        }

        #endregion

        #region Members

        // Stack of active function calls
        private Stack<CallFrame> frames = new();

        // VM host responsible for module loading and CLR integration
        private VMHost Host { get; set; }

        private Stack<TryFrame> tryFrames = new();

        public Action<VMState>? OnInstruction;
        public Action<int>? OnCallDepthChanged;

        #endregion

        #region Run

        /// <summary>
        /// Executes a module starting from instruction 0.
        /// </summary>
        /// <param name="module"></param>
        public async Task Run(Module module)
        {
            frames.Clear();
            frames.Push(new CallFrame
            {
                Ip = 0,
                Module = module,
                Env = module.Env
            });

            while (frames.Count > 0)
            {
                var frame = frames.Peek();

                if (frame.Ip >= frame.Module.Code.Count)
                {
                    frames.Pop();
                    continue;
                }
                try
                {
                    var ins = frame.Module.Code[frame.Ip++];
                    await Execute(ins, frame);
                }
                catch (Exception ex)
                {
                    HandleException(frame, ex);
                }
            }
        }

        /// <summary>
        /// Executes a single bytecode instruction.
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="frame"></param>
        /// <exception cref="Exception"></exception>
        private async Task Execute(Instruction ins, CallFrame frame)
        {
            OnInstruction?.Invoke(new VMState
            {
                Instruction = ins,
                Frame = frame,
                VM = this
            });

            var st = frame.Stack;
            switch (ins.Op)
            {
                // =========================
                // LOAD / STORE
                // =========================
                // Push constant value onto the stack
                case OpCode.LoadConst:
                    st.Push(ins.Value);
                    break;
                // Load variable by slot index
                case OpCode.LoadLocal:
                    {
                        st.Push(frame.Env.Values[ins.Arg]);
                        break;
                    }
                case OpCode.StoreLocal:
                    frame.Env.Values[ins.Arg] = st.Pop();
                    break;
                // Store value into local variable slot
                case OpCode.LoadGlobal:
                    st.Push(frame.Module.Env.Values[ins.Arg]);
                    break;
                case OpCode.StoreGlobal:
                    frame.Module.Env.Values[ins.Arg] = st.Pop();
                    break;
                // =========================
                // TABLE
                // =========================
                // Create an empty script table
                case OpCode.CreateTable:
                    st.Push(Value.NewTable());
                    break;
                // =========================
                // INDEX
                // =========================
                // Assign value to table entry or CLR property
                case OpCode.SetIndex:
                    {
                        var value = st.Pop();
                        var index = st.Pop();
                        var obj = st.Pop();

                        if (obj.Kind == ValueKind.Table &&
                            obj.T != null)
                        {
                            obj.T.Set(index, value);
                            break;
                        }

                        if (index.Kind == ValueKind.String)
                        {
                            Runtime.SetProperty(
                                obj.O,
                                index.S,
                                value.AsObject());
                            break;
                        }
                        throw new Exception("Object is not index assignable");
                    }
                case OpCode.SetIndexMute:
                    {
                        var value = st.Pop();
                        var index = st.Pop();
                        var obj = st.Peek(); // leave table on stack

                        if (obj.Kind != ValueKind.Table || obj.T == null)
                            throw new Exception("SetIndexMute target is not a table");

                        obj.T.Set(index, value);
                        break;
                    }
                case OpCode.GetIndex:
                    {
                        var index = st.Pop();
                        var obj = st.Pop();

                        // =========================
                        // CLR TYPE (static members)
                        // =========================
                        if (obj.O is Type t)
                        {
                            var member = Runtime.GetMember(t, index.S);
                            st.Push(new Value(member));
                            break;
                        }

                        // =========================
                        // CLR INSTANCE
                        // =========================
                        if (obj.O != null)
                        {
                            var member = Runtime.GetMember(obj.O, index.S);
                            st.Push(Value.FromObject(member));
                            break;
                        }

                        // =========================
                        // TABLE
                        // =========================
                        if (obj.T != null)
                        {
                            var val = obj.T.Get(index);
                            st.Push(val);
                            break;
                        }

                        // =========================
                        // CLR INSTANCE / VALUE TYPE
                        // =========================
                        var target = obj.AsObject();

                        if (target != null)
                        {
                            var member = Runtime.GetMember(target, index.S);
                            st.Push(Value.FromObject(member));
                            break;
                        }

                        throw new Exception("Invalid GetIndex target");
                    }
                // =========================
                // ARITHMETIC
                // =========================
                case OpCode.Add:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(a.Add(b));
                        break;
                    }
                case OpCode.Sub:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(a.Sub(b));
                        break;
                    }
                case OpCode.Mul:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(a.Mul(b));
                        break;
                    }
                case OpCode.Div:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(a.Div(b));
                        break;
                    }
                // =========================
                // COMPARE
                // =========================
                case OpCode.Equal:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(a.Equals(b)));
                        break;
                    }
                case OpCode.NotEqual:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(!a.Equals(b)));
                        break;
                    }
                case OpCode.Less:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(a.AsDouble() < b.AsDouble()));
                        break;
                    }
                case OpCode.Greater:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(a.AsDouble() > b.AsDouble()));
                        break;
                    }
                case OpCode.LessEqual:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(a.AsDouble() <= b.AsDouble()));
                        break;
                    }
                case OpCode.GreaterEqual:
                    {
                        var b = st.Pop();
                        var a = st.Pop();

                        st.Push(new Value(a.AsDouble() >= b.AsDouble()));
                        break;
                    }
                // =========================
                // NOT, NEGATE
                // =========================
                case OpCode.Not:
                    {
                        var v = st.Pop();
                        st.Push(new Value(!v.IsTrue));

                        break;
                    }
                case OpCode.Negate:
                    {
                        var v = st.Pop();

                        if (v.Kind == ValueKind.Int)
                            st.Push(new Value(-v.I));

                        else if (v.Kind == ValueKind.Double)
                            st.Push(new Value(-v.F));

                        else if (v.Kind == ValueKind.Decimal)
                            st.Push(new Value(-v.M));

                        else
                            throw new Exception(
                                "Cannot negate value");

                        break;
                    }
                // =========================
                // FUNCTIONS
                // =========================
                case OpCode.CreateFunction:
                    {
                        var proto = (ScriptFunction)ins.Value.O;

                        var fn = new ScriptFunction(
                            proto.Address,
                            proto.ParamCount)
                        {
                            Closure = frame.Env,
                            Module = frame.Module
                        };

                        st.Push(new Value(fn));
                        break;
                    }
                case OpCode.Call:
                    {
                        int argc = ins.Arg;
                        var args = new Value[argc];

                        for (int i = argc - 1; i >= 0; i--)
                            args[i] = st.Pop();

                        var func = st.Pop();

                        var r = await RunFunction(func, args);
                        st.Push(r);

                        break;
                    }
                case OpCode.CallBuiltin:
                    {
                        int argc = ins.Arg;
                        var args = new Value[argc];

                        for (int i = argc - 1; i >= 0; i--)
                            args[i] = st.Pop();

                        var builtinName = st.Pop().S;
                        var v = await RunBuiltinFunction(builtinName, args);
                        st.Push(v);
                        break;
                    }
                case OpCode.Return:
                    {
                        Value result = Value.Null;

                        if (st.Count > 0)
                            result = st.Pop();

                        frame.ReturnValue = result;
                        frame.Ip = int.MaxValue;

                        break;
                    }
                // =========================
                // JUMPS
                // =========================
                case OpCode.Jump:
                    frame.Ip = ins.Arg;
                    break;
                case OpCode.JumpIfTrue:
                    {
                        if (st.Peek().IsTrue)
                            frame.Ip = ins.Arg;

                        break;
                    }
                case OpCode.JumpIfFalse:
                    {
                        var cond = st.Pop();

                        if (!cond.IsTrue)
                            frame.Ip = ins.Arg;

                        break;
                    }
                case OpCode.JumpIfNotNull:
                    {
                        var v = st.Peek();

                        if (v.Kind != ValueKind.Null)
                            frame.Ip = ins.Arg;

                        break;
                    }
                case OpCode.JumpIfNull:
                    {
                        if (st.Peek().Kind == ValueKind.Null)
                            frame.Ip = ins.Arg;

                        break;
                    }
                case OpCode.Pop:
                    {
                        st.Pop();
                        break;
                    }
                // =========================
                // ITERATORS
                // =========================                    
                case OpCode.ForIterInit:
                    {
                        var v = st.Pop();
                        var e = v.AsEnumerable();

                        if (e == null)
                            throw new Exception("Not iterable");

                        st.Push(new Value(
                            new ScriptEnumerator(
                                e.GetEnumerator(),
                                ins.Arg)));
                        break;
                    }
                case OpCode.ForIterNext:
                    {
                        var sen = (ScriptEnumerator)st.Peek().O;

                        if (!sen.Enumerator.MoveNext())
                        {
                            st.Pop();
                            st.Push(new Value(false));
                            break;
                        }

                        var current = sen.Enumerator.Current;

                        if (current is TableEntry kv)
                        {
                            if (sen.VarCount == 1)
                                st.Push(kv.Value);
                            else
                            {
                                st.Push(kv.Key);
                                st.Push(kv.Value);
                            }
                        }
                        else
                        {
                            if (current is Value v)
                            {
                                st.Push(v);
                            }
                            else if (current is int i)
                            {
                                st.Push(new Value(i));
                            }
                            else
                            {
                                st.Push(new Value(current));
                            }
                        }

                        st.Push(new Value(true));
                        break;
                    }
                case OpCode.MakeRange:
                    {
                        var end = st.Pop();
                        var start = st.Pop();

                        var range = Enumerable.Range(start.I, end.I - start.I + 1);

                        st.Push(new Value(range));
                        break;
                    }
                // =========================
                // CLR
                // =========================
                case OpCode.LoadType:
                    {
                        st.Push(ins.Value);
                        break;
                    }
                case OpCode.New:
                    {
                        int argc = ins.Arg;

                        var args = new Value[argc];

                        for (int i = argc - 1; i >= 0; i--)
                            args[i] = st.Pop();

                        var typeVal = st.Pop();
                        var obj = Host.Sys.New(typeVal.S, args);

                        st.Push(new Value(obj));
                        break;
                    }
                // =========================
                // TRY CATCH
                // =========================
                case OpCode.TryBegin:
                    {
                        tryFrames.Push(new TryFrame
                        {
                            CatchIp = ins.Arg,
                            Frame = frame
                        });
                        break;
                    }
                case OpCode.TryEnd:
                    {
                        if (tryFrames.Count > 0 &&
                            tryFrames.Peek().Frame == frame)
                        {
                            tryFrames.Pop();
                        }
                        break;
                    }
                default:
                    throw new Exception($"Unknown opcode {ins.Op}");
            }
        }

        /// <summary>
        /// Executes a script-defined function.
        /// A new execution frame and local environment are created.
        /// The closure becomes the parent environment.       
        /// <param name="fn"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<Value> RunScriptFunction(
                ScriptFunction fn,
                Value[] args)
        {
            var frame = new CallFrame
            {
                Ip     = fn.Address,
                Module = fn.Module,
                Stack  = new Stack<Value>(),
                Env    = new Env()
            };

            frame.Env.Parent = fn.Closure;

            for (int i = 0; i < args.Length; i++)
                frame.Env.Add($"${i}", args[i]);

            frames.Push(frame);
            OnCallDepthChanged?.Invoke(+1);

            while (frames.Count > 0 && frames.Peek() == frame)
            {
                var ins = frame.Module.Code[frame.Ip++];
                await Execute(ins, frame);

                if (frame.Ip == int.MaxValue)
                    break;
            }

            frames.Pop();
            OnCallDepthChanged?.Invoke(-1);

            return frame.ReturnValue;
        }

        /// <summary>
        /// Executes built-in language functions.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Value> RunBuiltinFunction(string name, Value[] args)
        {
            switch (name)
            {
                case "print":
                    Console.WriteLine(args[0]);
                    return Value.Null;
                case "load":
                    {
                        var module = await Host.LoadModule(
                            args[0].S,
                            args.Length > 1
                                ? args[1].T
                                : new Table());

                        return new Value(module.Env.ToTable());
                    }

                default:
                    throw new Exception($"Unknown builtin {name}");
            }
        }

        /// <summary>
        /// Invokes script functions, ICallable objects
        /// or CLR-bound methods.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Value> RunFunction(Value func, Value[] args)
        {
            // =========================
            // Script function
            // =========================
            if (func.O is ScriptFunction sf)
            {
                return await RunScriptFunction(sf, args);
            }

            // =========================
            // ICallable
            // =========================
            if (func.O is ICallable callable)
            {
                return callable.Invoke(                    
                    this,
                    args.ToList());
            }

            // =========================
            // CLR bound method
            // =========================
            if (func.O is BoundMethod bm)
            {
                object instance =
                    bm.Target is Type ? null : bm.Target;

                object[] netArgs =
                    args.Select(v => v.AsObject())
                    .ToArray();

                var result = Runtime.InvokeMethod(
                    bm.Type,
                    instance,
                    bm.Name,
                    netArgs);

                return Value.FromObject(result);
            }
            throw new Exception($"Object is not callable: {func.Kind}, {func.O?.GetType().Name}");
        }
        
        /// <summary>
        /// Push to stack exception messages
        /// to be used in try catch
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="ex"></param>
        private void HandleException(
                CallFrame frame,
                Exception ex)
        {
            while (tryFrames.Count > 0)
            {
                var t = tryFrames.Pop();

                if (t.Frame != frame)
                    continue;

                frame.Stack.Push(
                    new Value(ex.Message));

                frame.Ip = t.CatchIp;
                return;
            }
            throw ex;
        }

        public List<VM.CallFrame> GetCallStack() => frames.Reverse().ToList();

        #endregion

        #region Classes

        /// <summary>
        /// Represents a single function invocation.
        /// Contains execution state, stack, environment,
        /// and return value.
        /// </summary>
        public class CallFrame
        {
            // Current instruction pointer
            public int Ip;

            // Module being executed
            public required Module Module;

            // Operand stack
            public Stack<Value> Stack = new();

            // Local execution environment
            public required Env Env;

            // Function return value
            public Value ReturnValue = Value.Null;
        }

        /// <summary>
        /// Try catch frame to find place 
        /// where exception is thrown
        /// </summary>
        private class TryFrame
        {
            public int CatchIp;
            public required CallFrame Frame;
        }
        private class ScriptEnumerator
        {
            public IEnumerator Enumerator;
            public int VarCount;

            public ScriptEnumerator(
                IEnumerator enumerator,
                int varCount)
            {
                Enumerator = enumerator;
                VarCount = varCount;
            }
        }

        #endregion
    }
}
