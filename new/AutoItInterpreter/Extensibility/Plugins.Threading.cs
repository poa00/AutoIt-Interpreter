﻿using System.Threading.Tasks;
using System;

using Unknown6656.AutoIt3.Runtime;
using Unknown6656.Common;

namespace Unknown6656.AutoIt3.Extensibility.Plugins.Debugging
{
    public sealed class ThreadingFunctionProvider
        : AbstractFunctionProvider
    {
        public override ProvidedNativeFunction[] ProvidedFunctions { get; } = new[]
        {
            ProvidedNativeFunction.Create(nameof(ThreadIsRunning), 1, ThreadIsRunning),
            ProvidedNativeFunction.Create(nameof(ThreadKill), 1, ThreadKill),
            ProvidedNativeFunction.Create(nameof(ThreadStart), 1, 256, ThreadStart),
            ProvidedNativeFunction.Create(nameof(ThreadWait), 1, ThreadWait),
            ProvidedNativeFunction.Create(nameof(ThreadGetID), 0, 1, ThreadGetID, Variant.Default),
        };


        public ThreadingFunctionProvider(Interpreter interpreter)
            : base(interpreter)
        {
        }

        public static FunctionReturnValue ThreadIsRunning(CallFrame frame, Variant[] args)
        {
            if (args[0].TryResolveHandle(frame.Interpreter, out ThreadHandle? handle))
                return (Variant)handle.Thread.IsRunning;
            else
                return FunctionReturnValue.Error(1);
        }

        public static FunctionReturnValue ThreadKill(CallFrame frame, Variant[] args)
        {
            if (args[0].TryResolveHandle(frame.Interpreter, out ThreadHandle? handle))
            {
                Variant running = handle.Thread.IsRunning;

                handle.Thread.Stop();
                handle.Runner.Wait();
                frame.Interpreter.GlobalObjectStorage.Delete(args[0]);

                return running;
            }
            else
                return FunctionReturnValue.Error(1);
        }

        public static FunctionReturnValue ThreadWait(CallFrame frame, Variant[] args)
        {
            if (args[0].TryResolveHandle(frame.Interpreter, out ThreadHandle? handle))
            {
                handle.Runner.Wait();

                FunctionReturnValue result = handle.Runner.Result;

                frame.Interpreter.GlobalObjectStorage.Delete(args[0]);

                return result;
            }
            else
                return FunctionReturnValue.Error(1);
        }

        public static FunctionReturnValue ThreadStart(CallFrame frame, Variant[] args)
        {
            if (!args[0].IsFunction(out ScriptFunction? func))
                func = frame.Interpreter.ScriptScanner.TryResolveFunction(args[0].ToString());

            if (func is { })
            {
                AU3Thread thread = frame.Interpreter.CreateNewThread();
                Task<FunctionReturnValue> runner = thread.StartAsync(func, frame.PassedArguments[1..]);
                Variant handle = frame.Interpreter.GlobalObjectStorage.Store(new ThreadHandle(thread, runner));

                return handle;
            }

            return FunctionReturnValue.Error(1);
        }

        public static FunctionReturnValue ThreadGetID(CallFrame frame, Variant[] args)
        {
            AU3Thread thread;

            if (args[0].TryResolveHandle(frame.Interpreter, out ThreadHandle? handle))
                thread = handle.Thread;
            else if (args[0].IsDefault)
                thread = frame.CurrentThread;
            else
                return FunctionReturnValue.Error(1);

            return (Variant)thread.ThreadID;
        }


        private sealed class ThreadHandle
            : IDisposable
        {
            public AU3Thread Thread { get; }
            public Task<FunctionReturnValue> Runner { get; }


            public ThreadHandle(AU3Thread thread, Task<FunctionReturnValue> runner)
            {
                Thread = thread;
                Runner = runner;
            }

            public void Dispose()
            {
                if (Runner.Status is TaskStatus.Running or TaskStatus.WaitingForChildrenToComplete)
                    Runner.Wait();

                Runner.Dispose();
                Thread.Dispose();
            }

            public override string ToString() => Thread.ToString();
        }
    }
}
