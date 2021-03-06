﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynTool
{
    internal static class Config
    {
        internal static bool CollectClass
        {
            get { return s_CollectClass; }
        }
        internal static bool CollectField
        {
            get { return s_CollectField; }
        }
        internal static HashSet<string> AddClassesConfig
        {
            get { return s_AddClassesConfig; }
        }
        internal static HashSet<string> RemoveClassesConfig
        {
            get { return s_RemoveClassesConfig; }
        }
        internal static bool CanCollect(string className, string baseName, System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> interfaces)
        {
            if (s_CollecterConfigs.Count <= 0)
                return true;
            foreach (var info in s_CollecterConfigs) {
                if (info.Bases.Count > 0 && !info.Bases.Contains(baseName)) {
                    continue;
                }
                if (info.Interfaces.Count > 0) {
                    bool interfaceMatch = true;
                    foreach (var intf in interfaces) {
                        if (!info.Interfaces.Contains(intf.Name)) {
                            interfaceMatch = false;
                            break;
                        }
                    }
                    if (!interfaceMatch)
                        continue;
                }
                if (!info.CachedNotExcepts.Contains(className)) {
                    if (info.Excepts.Contains(className)) {
                        continue;
                    }
                    foreach (var regex in info.ExceptMatches) {
                        if (regex.IsMatch(className)) {
                            info.Excepts.Add(className);
                            continue;
                        }
                    }
                    info.CachedNotExcepts.Add(className);
                }
                if (!info.CachedNotIncludes.Contains(className)) {
                    if (info.Includes.Contains(className)) {
                        return true;
                    }
                    foreach (var regex in info.Matches) {
                        if (regex.IsMatch(className)) {
                            info.Includes.Add(className);
                            return true;
                        }
                    }
                    info.CachedNotIncludes.Add(className);
                }
            }
            return false;
        }
        internal static bool CanMark(string className)
        {
            if (s_MarkerConfigs.Count <= 0)
                return true;
            foreach (var info in s_MarkerConfigs) {
                if (!info.CachedNotExcepts.Contains(className)) {
                    if (info.Excepts.Contains(className)) {
                        continue;
                    }
                    foreach (var regex in info.ExceptMatches) {
                        if (regex.IsMatch(className)) {
                            info.Excepts.Add(className);
                            continue;
                        }
                    }
                    info.CachedNotExcepts.Add(className);
                }
                if (!info.CachedNotIncludes.Contains(className)) {
                    if (info.Includes.Contains(className)) {
                        return true;
                    }
                    foreach (var regex in info.Matches) {
                        if (regex.IsMatch(className)) {
                            info.Includes.Add(className);
                            return true;
                        }
                    }
                    info.CachedNotIncludes.Add(className);
                }
            }
            return false;
        }
        internal static bool NeedLog(string name)
        {
            foreach(var list in s_LogConfigs) {
                bool match = true;
                int start = 0;
                foreach(var key in list) {
                    int ix = name.IndexOf(key, start, StringComparison.CurrentCultureIgnoreCase);
                    if (ix < 0) {
                        match = false;
                        break;
                    }
                    start = ix + key.Length;
                }
                if (match)
                    return true;
            }
            return false;
        }
        internal static void ReadConfig()
        {
            s_ExePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            s_AddClassesConfig.Clear();
            s_RemoveClassesConfig.Clear();
            s_CollecterConfigs.Clear();
            s_MarkerConfigs.Clear();

            var file = Path.Combine(s_ExePath, "unusedclass.dsl");
            var dslFile = new Dsl.DslFile();
            if (dslFile.Load(file, msg => { Console.WriteLine(msg); })) {
                foreach (var info in dslFile.DslInfos) {
                    ReadConfig(info);
                }
            }
        }
        private static void ReadConfig(Dsl.ISyntaxComponent info)
        {
            var f = info as Dsl.FunctionData;
            if (null == f)
                return;
            string id = f.GetId();
            if (id == "collecter") {
                var cfg = new CollecterOrMarkerConfig();
                foreach (var s in f.Params) {
                    ReadCollecterOrMarkerConfig(s, cfg);
                }
                s_CollecterConfigs.Add(cfg);
            }
            else if (id == "marker") {
                var cfg = new CollecterOrMarkerConfig();
                foreach (var s in f.Params) {
                    ReadCollecterOrMarkerConfig(s, cfg);
                }
                s_MarkerConfigs.Add(cfg);
            }
            else if (id == "addclasses") {                
                foreach (var p in f.Params) {
                    var str = p.GetId();
                    var lines = File.ReadAllLines(str);
                    foreach (var line in lines) {
                        var className = line.Trim();
                        if (!s_AddClassesConfig.Contains(className))
                            s_AddClassesConfig.Add(className);
                    }
                }
            }
            else if (id == "removeclasses") {
                foreach (var p in f.Params) {
                    var str = p.GetId();
                    var lines = File.ReadAllLines(str);
                    foreach (var line in lines) {
                        var className = line.Trim();
                        if (!s_RemoveClassesConfig.Contains(className))
                            s_RemoveClassesConfig.Add(className);
                    }
                }
            }
            else if (id == "log") {
                var list = new List<string>();
                foreach (var p in f.Params) {
                    var str = p.GetId();
                    list.Add(str);
                }
                s_LogConfigs.Add(list);
            }
            else if (id == "collect") {
                s_CollectClass = false;
                s_CollectField = false;
                foreach (var p in f.Params) {
                    var str = p.GetId();
                    if (str == "class") {
                        s_CollectClass = true;
                    }
                    else if (str == "field") {
                        s_CollectField = true;
                    }
                }
            }
        }
        private static void ReadCollecterOrMarkerConfig(Dsl.ISyntaxComponent comp, CollecterOrMarkerConfig cfg)
        {
            var fd = comp as Dsl.FunctionData;
            if (null != fd) {
                ReadCollecterOrMarkerConfig(fd, cfg);
            }
            else {
                var sd = comp as Dsl.StatementData;
                if (null != sd) {
                    ReadCollecterOrMarkerConfig(sd, cfg);
                }
            }
        }
        private static string ReadCollecterOrMarkerConfig(Dsl.FunctionData cd, CollecterOrMarkerConfig cfg)
        {
            var id = cd.GetId();
            if (id == "base") {
                foreach (var p in cd.Params) {
                    cfg.Bases.Add(p.GetId());
                }
            }
            else if (id == "interface") {
                foreach (var p in cd.Params) {
                    cfg.Interfaces.Add(p.GetId());
                }
            }
            else if (id == "include") {
                foreach (var p in cd.Params) {
                    cfg.Includes.Add(p.GetId());
                }
            }
            else if (id == "match") {
                foreach (var p in cd.Params) {
                    var str = p.GetId();
                    var regex = new Regex(str, RegexOptions.Compiled);
                    cfg.Matches.Add(regex);
                }
            }
            else if (id == "except") {
                foreach (var p in cd.Params) {
                    cfg.Excepts.Add(p.GetId());
                }
            }
            else if (id == "exceptmatch") {
                foreach (var p in cd.Params) {
                    var str = p.GetId();
                    var regex = new Regex(str, RegexOptions.Compiled);
                    cfg.ExceptMatches.Add(regex);
                }
            }
            return id;
        }
        private static void ReadCollecterOrMarkerConfig(Dsl.StatementData sd, CollecterOrMarkerConfig cfg)
        {
            for (int i = 0; i < sd.GetFunctionNum(); ++i) {
                var fd = sd.GetFunction(i);
                if (null != fd) {
                    ReadCollecterOrMarkerConfig(fd, cfg);
                }
            }
        }

        private class CollecterOrMarkerConfig
        {
            internal HashSet<string> Bases = new HashSet<string>();
            internal HashSet<string> Interfaces = new HashSet<string>();

            internal HashSet<string> Includes = new HashSet<string>();
            internal List<Regex> Matches = new List<Regex>();
            internal HashSet<string> Excepts = new HashSet<string>();
            internal List<Regex> ExceptMatches = new List<Regex>();

            internal HashSet<string> CachedNotIncludes = new HashSet<string>();
            internal HashSet<string> CachedNotExcepts = new HashSet<string>();
        }

        private static string s_ExePath = string.Empty;
        private static HashSet<string> s_AddClassesConfig = new HashSet<string>();
        private static HashSet<string> s_RemoveClassesConfig = new HashSet<string>();
        private static List<CollecterOrMarkerConfig> s_CollecterConfigs = new List<CollecterOrMarkerConfig>();
        private static List<CollecterOrMarkerConfig> s_MarkerConfigs = new List<CollecterOrMarkerConfig>();
        private static List<List<string>> s_LogConfigs = new List<List<string>>();
        private static bool s_CollectClass = true;
        private static bool s_CollectField = true;
    }
}
