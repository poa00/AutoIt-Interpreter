﻿using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

using AutoItInterpreter.PartialAST;
using AutoItInterpreter.Properties;
using AutoItExpressionParser;
using AutoItCoreLibrary;

namespace AutoItInterpreter
{
    using static InterpreterConstants;
    using static ExpressionAST;


    public static class ApplicationGenerator
    {
#pragma warning disable IDE1006
#pragma warning disable RCS1197
        private const string NAMESPACE = "AutoIt";
        private const string APPLICATION_MODULE = "Program";
        private const string TYPE_VAR_RPOVIDER = nameof(AutoItVariableDictionary);
        private const string TYPE_MAC_RPOVIDER = nameof(AutoItMacroDictionary);
        private const string TYPE = nameof(AutoItVariantType);
        private const string FUNC_MODULE = nameof(AutoItFunctions);
        private const string FUNC_PREFIX = AutoItFunctions.FUNC_PREFIX;
        private const string PARAM_PREFIX = "__param_";
        private const string DISCARD = "__discard";
        private const string MACROS = "__macros";
        private const string MACROS_GLOBAL = MACROS + "_g";
        private const string VARS = "__vars";


        public static string GenerateCSharpCode(InterpreterState state, InterpreterOptions options)
        {
            StringBuilder sb = new StringBuilder();
            string nl = Environment.NewLine;

            sb.AppendLine($@"
/*
    Autogenerated       {DateTime.Now:ddd yyyy-MM-dd, HH:mm:ss.ffffff}
    Using the command   {options.RawCommandLine}

{string.Concat(state.Errors.Select(err => $"  {err}{nl}"))}
*/
".Trim());

            if (state.Fatal && !options.GenerateCodeEvenWithErrors)
            {
                state.ReportKnownError("errors.generator.cannot_create", new DefinitionContext(state.RootDocument, 0));

                return sb.ToString();
            }

            string[] glob = { GLOBAL_FUNC_NAME };
            IEnumerable<string> pins = state.PInvokeSignatures.Select(x => AutoItFunctions.GeneratePInvokeWrapperName(x.Item1, x.Item2.Name));
            Serializer ser = new Serializer(new SerializerSettings(MACROS, VARS, TYPE, FUNC_PREFIX, func =>
            {
                if (state.ASTFunctions.ContainsKey(func.ToLower()))
                    return null;
                else if ((from p in pins
                          where p.Equals(func, StringComparison.InvariantCultureIgnoreCase)
                          select p).FirstOrDefault() is string fn)
                    return fn;
                else
                    try
                    {
                        return $"{FUNC_MODULE}.{typeof(AutoItFunctions).GetMethod(func, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase).Name}";
                    }
                    catch
                    {
                        if (Array.Find(BUILT_IN_FUNCTIONS, bif => bif.Name.Equals(func, StringComparison.InvariantCultureIgnoreCase)).Name is string fun)
                            return $"{FUNC_MODULE}.{fun}";
                        else
                        {
                            state.ReportKnownError("errors.astproc.func_not_declared", default, func);

                            return $"{FUNC_MODULE}.{nameof(AutoItFunctions.__InvalidFunction__)}";
                        }
                    }
            }));
            string tstr(EXPRESSION ex) => ex is null ? "«« error »»" : ser.Serialize(ex);
            bool allman = options.Settings.IndentationStyle == IndentationStyle.AllmanStyle;

            sb.AppendLine($@"
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Linq;
using System.Data;
using System;

using {nameof(AutoItCoreLibrary)};

#pragma warning disable CS0162
#pragma warning disable CS1522

namespace {NAMESPACE}
{{
    public static unsafe class {APPLICATION_MODULE}
    {{
        private static {TYPE_MAC_RPOVIDER} {MACROS_GLOBAL};
        private static {TYPE_VAR_RPOVIDER} {VARS};
        private static {TYPE} {DISCARD};
".TrimEnd());

            foreach (string fn in state.ASTFunctions.Keys.Except(glob).OrderByDescending(fn => fn).Concat(glob).Reverse())
            {
                AST_FUNCTION function = state.ASTFunctions[fn];
                var paramters = function.Parameters.Select(par =>
                {
                    bool opt = par is AST_FUNCTION_PARAMETER_OPT;

                    return $"{(par.ByRef ? "ref " : "")}{TYPE}{(opt ? "?" : "")} {PARAM_PREFIX}{par.Name.Name}{(opt ? " = null" : "")}";
                });

                if (fn == GLOBAL_FUNC_NAME)
                {
                    sb.AppendLine($@"
        public static void Main(string[] argv)
        {{
            Environment.SetEnvironmentVariable(""COREHOST_TRACE"", ""1"", EnvironmentVariableTarget.Process);
            AppDomain.CurrentDomain.AssemblyResolve += (_, a) =>
            {{
                string dll = (a.Name.Contains("","") ? a.Name.Substring(0, a.Name.IndexOf(',')) : a.Name.Replace("".dll"", """")).Replace(""."", ""_"");

                if (dll.EndsWith(""_resources""))
                    return null;

                ResourceManager rm = new ResourceManager(""{NAMESPACE}.Properties.Resources"", Assembly.GetExecutingAssembly());

                return Assembly.Load(rm.GetObject(dll) as byte[]);
            }};

            {TYPE} arguments = {TYPE}.Default;

            if (argv.FirstOrDefault(arg => Regex.IsMatch(arg, ""{AutoItFunctions.MMF_CMDPARG}=.+"")) is string mmfinarg)
                try
                {{
                    using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfinarg.Replace(""{AutoItFunctions.MMF_CMDPARG}="", """").Trim()))
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
                    {{
                        int len = acc.ReadInt32(0);
                        byte[] ser = new byte[len];

                        acc.ReadArray(4, ser, 0, ser.Length);

                        arguments = {TYPE}.{nameof(AutoItVariantType.Deserialize)}(ser);
                    }}
                }}
                catch
                {{
                }}

            {MACROS_GLOBAL} = new {TYPE_MAC_RPOVIDER}({FUNC_MODULE}.{nameof(AutoItFunctions.StaticMacros)}, s =>
            {{
                switch (s.ToLower())
                {{
                    case ""arguments"": return arguments;
                    // TODO
                }}
                return null;
            }});
            {VARS} = new {TYPE_VAR_RPOVIDER}();
            {DISCARD} = {TYPE}.Default;
            {TYPE} result = ___globalentrypoint();

            if (argv.FirstOrDefault(arg => Regex.IsMatch(arg, ""{AutoItFunctions.MMF_CMDRARG}=.+"")) is string mmfoutarg)
                try
                {{
                    MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfoutarg.Replace(""{AutoItFunctions.MMF_CMDRARG}="", """").Trim());
                    
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
                    {{
                        byte[] ser = result.Serialize();

                        acc.Write(0, ser.Length);
                        acc.WriteArray(4, ser, 0, ser.Length);
                        acc.Flush();
                    }}
                }}
                catch
                {{
                }}
        }}

        private static {TYPE} ___globalentrypoint()
        {{
            AutoItMacroDictionary {MACROS} = new AutoItMacroDictionary({MACROS_GLOBAL}, s =>
            {{
                switch (s.ToLower())
                {{
                    case ""numparams"":
                    case ""numoptparams"": return 0;
                    case ""function"": return """";
                }}
                return null;
            }});
".TrimEnd());

                    foreach (AST_LOCAL_VARIABLE v in function.ExplicitLocalVariables)
                        sb.AppendLine($@"            {VARS}[""{v.Variable.Name}""] = {(v.InitExpression is EXPRESSION e ? tstr(e) : TYPE + ".Default")};");

                    _print(function, 4);

                    sb.AppendLine($@"
            return {TYPE}.Default;
        }}
".TrimEnd());
                }
                else
                {
                    sb.AppendLine($@"
        private static {TYPE} {FUNC_PREFIX}{fn}({string.Join(", ", paramters)})
        {{
            AutoItMacroDictionary {MACROS} = new AutoItMacroDictionary({MACROS_GLOBAL}, s =>
            {{
                switch (s.ToLower())
                {{
                    case ""numparams"":
                        return {paramters.Count()};
                    case ""numoptparams"":
                        return {function.Parameters.Count(x => x is AST_FUNCTION_PARAMETER_OPT)};
                    case ""function"":
                        return ""{fn}"";
                }}
                return null;
            }});

            {TYPE} inner()
            {{
".TrimEnd());
                    _print(function, 5);

                    sb.AppendLine($@"
                return {TYPE}.Default;
            }}
            {VARS}.{nameof(AutoItVariableDictionary.InitLocalScope)}();");

                    foreach (VARIABLE v in function.Parameters.Select(x => x.Name).Concat(function.ExplicitLocalVariables.Select(x => x.Variable)))
                        sb.AppendLine($@"            {VARS}.{nameof(AutoItVariableDictionary.PushLocalVariable)}(""{v.Name}"");");

                    foreach (AST_FUNCTION_PARAMETER par in function.Parameters)
                    {
                        sb.Append($@"            {VARS}[""{par.Name.Name}""] = ({TYPE})({PARAM_PREFIX}{par.Name.Name}{(par is AST_FUNCTION_PARAMETER_OPT o ? $" ?? {tstr(o.InitExpression)}" : "")})");

                        if (!par.ByRef)
                            sb.Append($".{nameof(AutoItVariantType.Clone)}()");

                        sb.AppendLine(";");
                    }

                    sb.AppendLine($"            {TYPE} result = inner();");

                    foreach (VARIABLE par in function.Parameters.Where(x => x.ByRef).Select(x => x.Name))
                        sb.AppendLine($@"            {PARAM_PREFIX}{par.Name} = {VARS}[""{par.Name}""];");

                    sb.AppendLine($@"            {VARS}.{nameof(AutoItVariableDictionary.DestroyLocalScope)}();
            return result;
        }}
".TrimEnd());
                }
            }

            foreach ((string lib, PInvoke.PINVOKE_SIGNATURE sig) in state.PInvokeSignatures)
            {
                string wname = AutoItFunctions.GeneratePInvokeWrapperName(lib, sig.Name);

                sb.AppendLine($@"
        [DllImport(""{lib}"", EntryPoint = ""{sig.Name}"")]
        private static extern {sig.ReturnType} {wname}({string.Join(", ", sig.Paramters.Select((p, i) => $"{p} _param{i}"))});
".TrimEnd());
            }

            sb.AppendLine($@"
        private static {TYPE} __critical(string s) => throw new InvalidProgramException(s ?? """");
    }}
}}");

            void _print(AST_STATEMENT e, int indent)
            {
                void println(string s, int i = -1) => sb.Append(new string(' ', 4 * ((i < 1 ? indent : i) - 1))).AppendLine(s);
                void print(AST_STATEMENT s) => _print(s, indent + 1);
                void printblock(AST_STATEMENT[] xs, string p = "", string s = "")
                {
                    if (allman)
                    {
                        if (p.Length > 0)
                            println(p);

                        println("{");
                    }
                    else
                        println(p.Length > 0 ? $"{p} {{" : "{");

                    foreach (AST_STATEMENT x in xs ?? new AST_STATEMENT[0])
                        print(x);

                    if (allman)
                    {
                        println("}");

                        if (s.Length > 0)
                            println(s);
                    }
                    else
                        println(s.Length > 0 ? $"}} {s}" : "}");
                }

                switch (e)
                {
                    case AST_IF_STATEMENT s:
                        printblock(s.If.Statements, $"if ({tstr(s.If.Condition)})");

                        foreach (AST_CONDITIONAL_BLOCK elif in s.ElseIf ?? new AST_CONDITIONAL_BLOCK[0])
                            printblock(elif.Statements, $"else if ({tstr(elif.Condition)})");

                        if (s.OptionalElse is AST_STATEMENT[] b)
                            printblock(b, "else");

                        return;
                    case AST_WHILE_STATEMENT s:
                        printblock(s.WhileBlock.Statements, $"while ({tstr(s.WhileBlock.Condition)})");

                        return;
                    case AST_SCOPE s:
                        println("{");

                        foreach (AST_STATEMENT ls in s.Statements ?? new AST_STATEMENT[0])
                            print(ls);

                        println("}");

                        return;
                    case AST_BREAK_STATEMENT s when s.Level == 1:
                        println("break;");

                        return;
                    case AST_LABEL s:
                        println(s.Name.Replace("<>", "") + ":;", 1);

                        return;
                    case AST_GOTO_STATEMENT s:
                        if (s.Label is null)
                            println("// called `goto´ on non-existent label ----> possible error?");
                        else
                            println($"goto {s.Label.Name.Replace("<>", "")};");

                        return;
                    case AST_ASSIGNMENT_EXPRESSION_STATEMENT s:
                        println(tstr(EXPRESSION.NewAssignmentExpression(s.Expression)) + ';');

                        return;
                    case AST_EXPRESSION_STATEMENT s:
                        println($"{DISCARD} = {tstr(s.Expression)};");

                        return;
                    case AST_INLINE_CSHARP s:
                        println(s.Code);

                        return;
                    case AST_RETURN_STATEMENT s:
                        println($"return {tstr(s.Expression)};");

                        return;
                    case AST_Λ_ASSIGNMENT_STATEMENT s:
                        string fname = s.Function.Trim();
                        string del;

                        if (state.ASTFunctions.ContainsKey(fname))
                            del = $"typeof({APPLICATION_MODULE}).GetMethod(nameof({FUNC_PREFIX}{fname}), BindingFlags.NonPublic | BindingFlags.Static)";
                        else
                            del = $"typeof({FUNC_MODULE}).GetMethod(\"{fname}\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase)";

                        println($"{tstr(s.VariableExpression)} = {TYPE}.{nameof(AutoItVariantType.NewDelegate)}({del});");

                        return;
                    case AST_REDIM_STATEMENT s:
                        string varexpr = tstr(EXPRESSION.NewVariableExpression(s.Variable));
                        string dimexpr = string.Concat(s.DimensionExpressions.Select(dim => $", ({tstr(dim)}).{nameof(AutoItVariantType.ToLong)}()"));

                        println($"{varexpr} = {TYPE}.{nameof(AutoItVariantType.RedimMatrix)}({varexpr}{dimexpr});");

                        return;
                    default:
                        println($"// TODO: {e}"); // TODO

                        return;
                }
            }

            return Regex.Replace(sb.ToString(), @"\s*«\s*(?<msg>.*)\s*»\s*", m => $"(__critical(\"{m.Get("msg").Trim()}\"))");
        }

        public static string GenerateCSharpAssemblyInfo(InterpreterState state) => $@"
// Autogenerated  {DateTime.Now:ddd yyyy-MM-dd, HH:mm:ss.ffffff}

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;


[assembly: AllowReversePInvokeCalls]
[assembly: AssemblyTitle(""{state.CompileInfo.AssemblyProductName}"")]
[assembly: AssemblyDescription(""{state.CompileInfo.AssemblyFileDescription}"")]
[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""{state.CompileInfo.AssemblyCompanyName}"")]
[assembly: AssemblyProduct(""{state.CompileInfo.AssemblyProductName}"")]
[assembly: AssemblyCopyright(""{state.CompileInfo.AssemblyCopyright}"")]
[assembly: AssemblyTrademark(""{state.CompileInfo.AssemblyTrademarks}"")]
[assembly: AssemblyCulture(""{state.CompileInfo.AssemblyComment}"")]
[assembly: ComVisible(true)]
[assembly: Guid(""{Guid.NewGuid():D}"")]
[assembly: AssemblyVersion(""{state.CompileInfo.AssemblyProductVersion}"")]
[assembly: AssemblyFileVersion(""{state.CompileInfo.AssemblyFileVersion}"")]
".TrimStart();

#pragma warning restore IDE1006
#pragma warning restore RCS1197

        public static string GetAssemblyName(InterpreterState state, string projname)
        {
            string asmname = state.CompileInfo.FileName?.Trim('.') ?? projname;

            if (asmname.Contains('.'))
                asmname = asmname.Remove(asmname.IndexOf('.')).Trim();

            if (asmname.Length == 0)
                asmname = projname;

            return asmname.Replace(' ', '_');
        }

        public static int GenerateDotnetProject(ref DirectoryInfo dir, string name, out string log)
        {
            DirectoryInfo ndir = new DirectoryInfo($"{dir.FullName}/{name}");

            if (ndir.Exists)
                try
                {
                    ndir.Delete(true);
                }
                catch
                {
                    ndir.Delete(true); // second time's a chrarm?
                }

            using (Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = dir.FullName,
                    Arguments = $"new console -n \"{name}\" -lang \"C#\" --force",
                    FileName = "dotnet",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            })
            {
                proc.Start();
                proc.WaitForExit();

                using (StreamReader cout = proc.StandardOutput)
                using (StreamReader cerr = proc.StandardError)
                    log = cout.ReadToEnd() + '\n' + cerr.ReadToEnd();

                dir = dir.CreateSubdirectory(name);

                return proc.ExitCode;
            }
        }

        public static void EditDotnetProject(InterpreterState state, TargetSystem target, DirectoryInfo dir, string name)
        {
            if (File.Exists($"{dir.FullName}/Program.cs"))
                File.Delete($"{dir.FullName}/Program.cs");

            string dllpath = $"{dir.FullName}/../{nameof(Resources.autoitcorlib)}.dll";
            string respath = $"{dir.FullName}/resources.resx";

            File.WriteAllBytes(dllpath, Resources.autoitcorlib);
            File.WriteAllText($"{dir.FullName}/{name}.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>exe</OutputType>
        <AssemblyName>{GetAssemblyName(state, name)}</AssemblyName>
        <ApplicationIcon>{state.CompileInfo.IconPath ?? ""}</ApplicationIcon>
        <StartupObject>{NAMESPACE}.{APPLICATION_MODULE}</StartupObject>
        <TargetFramework>netcoreapp2.0</TargetFramework>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <Optimize>true</Optimize>
        <LangVersion>latest</LangVersion>
        <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
        <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
        <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
        <OutputPath>bin</OutputPath>
        <SelfContained>true</SelfContained>
        <RuntimeIdentifier>{target.Identifier}</RuntimeIdentifier>
        <Prefer32Bit>{(!target.Is64Bit).ToString().ToLower()}</Prefer32Bit>
        <DebugType>None</DebugType>
        <DebugSymbols>false</DebugSymbols>
        <CopyOutputSymbolsToPublishDirectory>false</CopyOutputSymbolsToPublishDirectory>
    </PropertyGroup>
    <ItemGroup>
        <Reference Include=""{nameof(Resources.autoitcorlib)}"">
            <HintPath>{dllpath}</HintPath>
        </Reference>
    </ItemGroup>
    <ItemGroup>
        <EmbeddedResource Include=""{dllpath}""/>
    </ItemGroup>
    <ItemGroup>
        <Compile Include=""{name}.cs""/>
    </ItemGroup>
</Project>
");
        }

        public static int BuildDotnetProject(DirectoryInfo dir)
        {
            using (Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = dir.FullName,
                    Arguments = "build",
                    FileName = "dotnet",
                }
            })
            {
                proc.Start();
                proc.WaitForExit();

                return proc.ExitCode;
            }
        }

        public static int PublishDotnetProject(DirectoryInfo dir)
        {
            using (Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = dir.FullName,
                    Arguments = "publish -c Release --force --self-contained -v m",
                    FileName = "dotnet",
                }
            })
            {
                proc.Start();
                proc.WaitForExit();

                return proc.ExitCode;
            }
        }
    }

    public sealed class TargetSystem
    {
        public Compatibility Compatibility { get; }
        public Architecture? TargetArchitecture { get; }
        public bool Is64Bit => TargetArchitecture is Architecture a ? a == Architecture.Arm64 || a == Architecture.X64 : false;
        public string Identifier => Compatibility.ToString().Replace('_', '.') + (TargetArchitecture is null ? "" : '-' + TargetArchitecture.ToString().ToLower());


        public TargetSystem(Compatibility comp, Architecture? arch) => (Compatibility, TargetArchitecture) = (comp, arch);
    }
}
