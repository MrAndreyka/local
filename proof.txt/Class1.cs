using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
    static class Class1
    {
        public delegate void ShowError(string val);
        public static int AsInt(this string val, int def = 0)
        { int res; return int.TryParse(val, out res) ? res : def; }
        public static int? AsInt(this string val, ShowError func)
        {
            int res;
            if (int.TryParse(val, out res)) return res;
            func?.Invoke(val); return null;
        }
        public static long AsLong(this string val, int def = 0)
        { long res; return long.TryParse(val, out res) ? res : def; }
        public static long? AsLong(this string val, ShowError func)
        {
            long res;
            if (long.TryParse(val, out res)) return res;
            func?.Invoke(val); return null;

        }
        public static string GetParam(this string val, ref int from, params char[] end)
        {
            var ekran = val.IndexOf('"', from, 1) >= 0;
            int j = from;
            from = val.IndexOfAny(end, from);

            if (!ekran)
            { return val.Substring(j, from >= 0 ? from - j : val.Length - j); }

            var i = from + 1;
            while ((i = val.IndexOf('"', i)) >= 0 && val[i - 1] == '\\') { }
            if (i <= 0) return string.Empty;

            from = i + 1;
            return val.Substring(from + 1, j - from - 1);
        }
        public static string ToParam(this string val, params char[] chars)
        {
            if (val.IndexOfAny(chars) >= 0)
                return $"\"{val.Replace("\"", "\\\"")}\"";
            return val;
        }

        public static string Begin(this string msk, char ch, bool cut = false)
        {
            var i = msk.IndexOf(ch);
            string res = i < 0 ? msk : msk.Substring(0, i);
            if (cut) msk = i < 0 ? string.Empty : msk.Substring(i + 1);
            return res;
        }
        public static string Substring(this string val, int ind, string to)
        {
            var e = val.IndexOf(to, ind + 1);
            return e < 0 ? val.Substring(ind) : val.Substring(ind, e - ind);
        }

        public static MyCommandLine ComLine(this string arg)
        { var res = new MyCommandLine(); return res.TryParse(arg) ? res : null; }
    }
}
