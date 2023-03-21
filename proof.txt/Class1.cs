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
        public static byte AsByte(this string val, byte def = 0)
        { byte res; return byte.TryParse(val, out res) ? res : def; }
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
        public static string Part(this string msk, char ch, bool After = false, bool NotNull = true)
        {
            var i = (msk == null) ? -1 : msk.IndexOf(ch);
            return i < 0 ? (NotNull ? msk : null) : After ? msk.Substring(i + 1) : msk.Substring(0, i);
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
