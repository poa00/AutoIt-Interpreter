﻿using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace Unknown6656.AutoIt3.Localization;


public sealed class LanguageLoader
{
    private readonly Dictionary<string, LanguagePack> _packs = new(StringComparer.OrdinalIgnoreCase);


    public string[] LoadedLanguageCodes => [.. _packs.Keys];

    public LanguagePack? CurrentLanguage { get; private set; }

    public LanguagePack? this[string code] => TryGetLanguagePack(code, out LanguagePack? pack) ? pack : null;


    public void LoadLanguagePackFromAssembly(Assembly assembly, string @namespace, bool overwrite_existing = true)
    {
        Regex regex_json = new($@"^.+\.{@namespace}\.(lang)?-*(?<code>\w+)-*(lang)?\.json$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach ((string? code, string name) in from res in assembly.GetManifestResourceNames()
                                                let match = regex_json.Match(res)
                                                where match.Success
                                                select (match.Groups["code"].Value.ToLowerInvariant(), res))
            try
            {
                using Stream? resource = assembly.GetManifestResourceStream(name);

                if (resource is { })
                    using (StreamReader? rd = new(resource))
                        LoadLanguagePackFromYAML(assembly.Location, rd.ReadToEnd(), overwrite_existing);
            }
            catch
            {
            }
    }

    public void LoadLanguagePackFromYAMLFile(FileInfo file, bool overwrite_existing = true)
    {
        string path = Path.GetFullPath(file.FullName);

        LoadLanguagePackFromYAML(path, File.ReadAllText(path), overwrite_existing);
    }

    private void LoadLanguagePackFromYAML(string path, string yaml, bool overwrite_existing = true)
    {
        try
        {
            LanguagePack lang = LanguagePack.FromYAML(yaml);
            string code = lang.LanguageCode;

            lang.FilePath = path;

            if (CurrentLanguage is null && _packs.Count == 0)
                CurrentLanguage = lang;

            if (overwrite_existing || !_packs.ContainsKey(code))
                _packs[code] = lang;
        }
        catch
        {
        }
    }

    public void HasLanguagePack(string code) => _packs.ContainsKey(code);

    public bool TryGetLanguagePack(string code, out LanguagePack? pack) => _packs.TryGetValue(code, out pack);

    public bool TrySetCurrentLanguagePack(string code)
    {
        bool res = TryGetLanguagePack(code, out LanguagePack? pack);

        if (res)
            CurrentLanguage = pack;

        return res;
    }

    public void LoadLanguagePacksFromDirectory(DirectoryInfo directory, bool overwrite_existing = true)
    {
        foreach (FileInfo file in directory.EnumerateFiles())
            LoadLanguagePackFromYAMLFile(file, overwrite_existing);
    }
}

public sealed partial class LanguagePack
{
    private static readonly Regex REGEX_YAML = new(@"^(?<indent> *)(?<quote>""|)(?<key>[^"":]+)\k<quote> *: *(?<value>""(?<string>.*)""|true|false|[+\-]\d+|[+\-]0x[0-9a-f]+|null)? *(#.*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex REGEX_ESCAPE = new(@"\\(?<esc>[rntve0baf\\""]|[xu][0-9a-f]{1,4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

#warning todo: check if nonbacktracking is really necessary or if it is leading to bugs
    private static readonly Regex REGEX_NON_CSFMT_STRING = new(@"\{[^:\}\{][^:\}]*:\d*[^\d\}].*?\}", RegexOptions.Compiled);

    private readonly IDictionary<string, string> _strings;


    public string this[string key, params object?[] args] => TryGetString(key, args, out string? s) ? s! : $"[{key.ToUpper()}]";

    internal string FilePath { get; set; } = "";

    public string LanguageCode => _strings["meta.code"];

    public string LanguageName => _strings["meta.name"];

    public bool IsBeta => !bool.TryParse(_strings["meta.beta"], out bool beta) || beta;

    public string Author => (_strings.TryGetValue("meta.author", out string? auth) ? auth : null) ?? "unknown6656";


    private LanguagePack(IDictionary<string, string> strings) => _strings = strings;

    private bool TryGetString(string key, object?[] args, out string? formatted)
    {
        formatted = null;
        key = "strings." + key;

        if (_strings?.FirstOrDefault(kvp => kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value is string fmt_str)
        {
            int argc = FormatStringRegex()
                            .Matches(fmt_str)
                            .Cast<Match>()
                            .Select(m => byte.TryParse(m.Groups["num"].Value, out byte b) ? b + 1 : 0)
                            .Append(0)
                            .Max();

            if (args.Length < argc)
                Array.Resize(ref args, argc);

            formatted = fmt_str;

            if (fmt_str.Contains('{'))
            {
                fmt_str = REGEX_NON_CSFMT_STRING.Replace(fmt_str, match => match.Value.Contains('|') ? $"{{{match.Value}}}" : match.Value);
                formatted = string.Format(fmt_str, args);
            }

            return true;
        }
        else
            return false;
    }

    public override string ToString() => $"\"{LanguageCode} - {LanguageName}\" by {Author}{(IsBeta ? " (beta)" : "")}";

    public static LanguagePack FromYAML(string yaml)
    {
        string[] lines = yaml.Replace("\t", "    ")
                             .Replace("\r\n", "\n")
                             .Replace("\v", "")
                             .Replace("\0", "")
                             .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        List<(int level, string path)> scope = [];
        Dictionary<string, string> dict = [];

        foreach (string line in lines)
            if (REGEX_YAML.Match(line) is { Success: true, Groups: GroupCollection groups })
            {
                int indent = groups["indent"].Length;
                string key = groups["key"].Value;

                for (int i = scope.Count; i-- > 0;)
                    if (indent > scope[i].level)
                        break;
                    else
                        scope.RemoveAt(i);

                if (groups["value"].Success)
                {
                    string value = groups["string"].Success ? ParseString(groups["string"].Value) : groups["value"].Value;

                    key = string.Join(".", scope.Select(s => s.path).Append(key));
                    dict[key] = value;
                }
                else
                    scope.Add((indent, key));
            }

        return new LanguagePack(dict);
    }

    private static string ParseString(string value)
    {
        StringBuilder sb = new();

        while (value.Length > 0)
            if (REGEX_ESCAPE.Match(value) is { Success: true } m_esc)
            {
                int i = m_esc.Index;
                string esc = m_esc.Groups["esc"].Value;

                sb.Append(value[..i]);
                sb.Append(esc[0] switch
                {
                    'x' or 'u' when short.TryParse(esc[1..], NumberStyles.HexNumber, null, out short s) => (char)s,
                    'r' => '\r',
                    'n' => '\n',
                    't' => '\t',
                    'v' => '\v',
                    '0' => '\0',
                    'b' => '\b',
                    'a' => '\a',
                    'e' => '\e',
                    'f' => '\f',
                    '\\' => '\\',
                    '"' => '"',
                    _ => m_esc.ToString()
                });

                value = value[(i + m_esc.Length)..];
            }
            else
            {
                sb.Append(value);

                break;
            }

        return sb.ToString();
    }


    [GeneratedRegex(@"\{(?<num>\d+)\}", RegexOptions.Compiled | RegexOptions.NonBacktracking)]
    private static partial Regex FormatStringRegex();
}
