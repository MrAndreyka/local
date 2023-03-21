#region сборка VRage.Game, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// D:\My Doc's\Space Engineers\Bin64\VRage.Game.dll
#endregion

using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace ConsoleApp_sharp
{
    partial class Program
    {

        public delegate string GetNames(string val);
        public class InvDT
        {
            public readonly IMyInventory Inventory;
            public readonly IMyTerminalBlock Owner;
            public InvDT(IMyTerminalBlock Bl, byte Index = byte.MaxValue)
            { Owner = Bl; Inventory = Index == byte.MaxValue ? Bl.GetInventory() : Bl.GetInventory(Index); }
            public bool Equals(InvData obj) => Inventory.Equals(obj.Inventory);
            public override string ToString() => Owner.CustomName;
        }
        public class InvData : InvDT
        {
            public byte key;
            public InvData(IMyTerminalBlock tb, byte InvInd, byte key_) : base(tb, InvInd) { key = key_; }
            public static bool IsSpecBloc(IMyTerminalBlock tb) { return ((tb is IMyReactor) || (tb is IMyGasGenerator) || (tb is IMyGasTank) || (tb is IMyLargeTurretBase)); }
        }

        public interface ITextSurf
        {
            IMyTerminalBlock OwnerBloc { get; }
            int Index { get; }
        }

        public class MySurface
        {
            public int total_lines, cur_lines = 0;
            private StringBuilder res = new StringBuilder();

            public void ReadText(StringBuilder rs) { rs.Append(res); }

            public MySurface(int count) { total_lines = count; }
            public bool WriteText(string text, bool added = true)
            {
                if (added) { res.Append(text); cur_lines++; }
                else { res = new StringBuilder(text); if (!string.IsNullOrWhiteSpace(text)) cur_lines = 1; }
                return true;
            }
            public override string ToString() => $"{cur_lines}:{res}";

            public Vector2 MeasureStringInPixels(StringBuilder text, string font, float scale)
            { Vector2 res; res.X = 500f; res.Y = (1 + text.ToString().Count(x => x == '\n')) * 15f; return res; }

            public string Font => "default";
            public float FontSize => 1f;
            public float TextPadding => 0f;
            Vector2 SurfaceSize_() { Vector2 res; res.X = 500f; res.Y = total_lines * 15f; return res; }
            public Vector2 SurfaceSize { get => SurfaceSize_(); }
        }

        public interface ISaving { MyTree Save(MyTree val = null); }
        public interface ITxt_null : ISaving
        {
            Txt_Panel Owner { get; set; }
            string ToString();
            bool AddLine(string str, bool addAlways); //return false если панель полная
            void Restore();
            Txt_Surface FindPanel(Func<ITextSurf, bool> f);
        }

        public class Txt_Surface : ITxt_null, ITextSurf
        {
            public readonly IMyTextSurface Surface;
            private readonly IMyTerminalBlock ownerBloc;
            private readonly int index = 0;
            public Txt_Panel Owner { get; set; }

            public IMyTerminalBlock OwnerBloc => ownerBloc;
            public int Index => index;
            public Txt_Surface(IMyTextSurfaceProvider block, int index = 0)
            {
                if (block == null) return;
                if (block.SurfaceCount == 0) throw new Exception("The block does not contain surface");
                ownerBloc = (IMyTerminalBlock)block;
                Surface = block.GetSurface(this.index = index);
                Surface.ContentType = ContentType.TEXT_AND_IMAGE;
            }
            public override string ToString() => $"{ownerBloc.CustomName} /{index}";
            public bool AddLine(string str, bool addAlways = true)
            {
                if (addAlways) return Surface.WriteText(str + "\n", true);

                var nw = new StringBuilder();
                Surface.ReadText(nw);
                nw.Append(str);

                var sz = Surface.MeasureStringInPixels(nw, Surface.Font, Surface.FontSize);
                if (Surface.SurfaceSize.Y - Surface.TextPadding - sz.Y < 0) return false;
                Surface.WriteText(str + "\n");
                return true;
            }
            public void Restore() { Surface.WriteText(string.Empty); }
            public Txt_Surface FindPanel(Func<ITextSurf, bool> f) => f(this) ? this : null;
            public virtual MyTree Save(MyTree res = null)
            {
                if (res == null) res = new MyTree();
                res.Name = ownerBloc.EntityId.ToString();
                res.Param = index.ToString();
                return res;
            }
        }

        public class Txt_Surface_ : Txt_Surface
        {
            public new readonly MySurface Surface;
            public new readonly string OwnerBloc;
            public readonly int index = 0;

            public Txt_Surface_(string block, int lines, int index = 0) : base(null, 0)
            {
                OwnerBloc = block;
                Surface = new MySurface(lines);
                this.index = index;
            }
            public override string ToString() => $"{OwnerBloc} [{index}]\n{Surface}";

            public new bool AddLine(string str, bool addAlways = true)
            {
                if (addAlways) return Surface.WriteText(str + "\n", true);

                var nw = new StringBuilder();
                Surface.ReadText(nw);
                nw.Append(str);

                var sz = Surface.MeasureStringInPixels(nw, Surface.Font, Surface.FontSize);
                if (Surface.SurfaceSize.Y - Surface.TextPadding - sz.Y < 0) return false;
                Surface.WriteText(str + "\n");
                return true;
            }
            public new void Restore() { Surface.WriteText(string.Empty); }
            public override MyTree Save(MyTree res = null)
            {
                if (res == null) res = new MyTree();
                res.Name = OwnerBloc;
                res.Param = index.ToString();
                return res;
            }
        }
        public class Txt_Panel : List<ITxt_null>, ITxt_null
        {
            public bool hor;
            int _tec = 0;
            public Txt_Panel Owner { get; set; }
            public Txt_Panel(bool horizontal, List<ITxt_null> list = null)
            { hor = horizontal; if (list != null) base.AddRange(list); }
            public override string ToString()
            {
                var tmp = new List<string>(Count);
                ForEach(x => tmp.Add(x.ToString()));
                return $"{{{string.Join(", ", tmp)}:{(hor ? "Horizontal" : "Vertical")}}}";
            }
            public new void Add(ITxt_null val) { val.Owner = this; base.Add(val); }

            public bool IsEnd() => _tec > Count;
            //Сдвигается к следующей позиции, возвращает истина если оказывается на последней позиции
            bool Next(bool to_round = false)
            => (!IsEnd() || to_round) && (++_tec < Count) || ((_tec = 0) == 0);

            public bool AddLine(string str, bool addAlways = true)
            {
                if (Count == 0) return false;
                return hor ? AddLineHor(str, addAlways) : AddLineVert(str, addAlways);
            }

            bool AddLineHor(string str, bool addAlways)
            {
                var t = Count;
                bool f = false;
                while (t-- > 0 && !f)
                {
                    if (!(f = this[_tec].AddLine(str, false)))
                        f = Next(true);
                }
                if (!f && addAlways)
                    this[Count - 1].AddLine(str, true);
                return f;
            }

            bool AddLineVert(string str, bool addAlways)
            {
                bool f1, fl = addAlways && IsEnd();
                while (!(f1 = this[_tec].AddLine(str, fl) || !IsEnd()))
                    fl = Next(false) && addAlways;

                return f1;
            }

            public void Restore() { _tec = 0; ForEach(x => x.Restore()); }

            public Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                FindIndex(x => (p = x.FindPanel(f)) != null);
                return p;
            }
            public MyTree Save(MyTree res = null)
            {
                if (res == null) res = new MyTree();
                res.Param = hor.ToString();
                ForEach(x => res.Add(x.Save()));
                return res;
            }
            public static string TryParse(Txt_Panel res, MyTree val, Action<List<ITxt_null>, string, int> GetSurf)
            {
                var p = new Txt_Panel(false);
                if (!bool.TryParse(val.Param, out p.hor)) return "Ошибка преобразования логического значения";

                string er;
                foreach (var x in val)
                {
                    if (x.Count > 0)
                    {
                        er = Txt_Panel.TryParse(p, x, GetSurf);
                        if (er != null) return er;
                        continue;
                    }
                    GetSurf(p, x.Name, int.Parse(x.Param));
                }
                res.Add(p);
                return null;
            }
        }
        public class Selected_Panel<T, T2> : Txt_Panel
        {
            public T Select;
            public Selected_Panel(T sel) : base(false) { Select = sel; }

            public bool AddLine(T2 sel, string str)
            {
                var fl = Select.Equals(sel);
                if (fl) base.AddLine(str);
                return fl;
            }
            public override string ToString() => $"{Select}{base.ToString()}";

            public static Selected_Panel<T, T2> Parse(MyTree val, Func<MyTree, T> GetSel, Action<List<ITxt_null>, string, int> GetTxt)
            {
                var tmp = val.FirstOrDefault(x => x.Name.Equals("Select", StringComparison.OrdinalIgnoreCase));
                if (tmp == null) throw new Exception("Не найден сектор Select");

                var sl = GetSel(tmp);
                if (sl == null) throw new Exception("Не удалось восстановить Select в Selected_Panel");

                tmp = val.FirstOrDefault(x => x.Name.Equals("Surface", StringComparison.OrdinalIgnoreCase));
                if (tmp == null) throw new Exception("Не найден сектор Surface");

                var res = new Selected_Panel<T, T2>(sl);
                var er = Txt_Panel.TryParse(res, tmp, GetTxt);
                if (er != null) throw new Exception(er);
                return res;
            }
        }
        public class TextOut<T, T2> : List<Selected_Panel<T, T2>>
        {
            public StringBuilder DefList = new StringBuilder();
            public void AddLine(T2 sel, string str, bool skipped = false)
            {
                if (FindIndex(x => x.AddLine(sel, str) == true) < 0 && !skipped)
                    DefList.AppendLine(str);
            }
            public void AddLine(string Val) => DefList.Append(Val);
            public bool IsSpec(T2 sel) => FindIndex(x => x.Select.Equals(sel)) >= 0;
            public void Restore() { DefList.Clear(); ForEach(x => x.Restore()); }
            public Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                FindIndex(x => (p = x.FindPanel(f)) != null);
                return p;
            }
            public MyTree Save(MyTree res = null)
            {
                if (res == null) res = new MyTree();
                ForEach(x => res.Add(x.Save()));
                return res;
            }
            public bool Load(MyTree res, Func<MyTree, T> GetSel, Action<List<ITxt_null>, string, int> GetPan)
            {
                Selected_Panel<T, T2> tm;
                res.ForEach(x => {
                    if ((tm = Selected_Panel<T, T2>.Parse(x, GetSel, GetPan)) != null)
                        Add(tm);
                });
                return true;
            }
            public string ToString(bool isSave = false)
            {
                List<string> res = new List<string>(Count);
                ForEach(x => res.Add(x.ToString()));
                return string.Join(isSave ? "," : "\n", res);
            }
        }

        public class Item_sel
        {
            public byte Type;
            public string Name;

            public Item_sel(byte Type, string Name = null) { this.Type = Type; this.Name = Name; }

            public bool Equals(Item_sel obj) => Type == obj.Type && Name == obj.Name;
        }

        public class Sel_Item : Item_sel
        {
            public bool invert;
            public Sel_Item(byte Type, string Name = null, bool invert = false) : base(Type, Name)
            { this.invert = invert; }
            public bool Equals(Sel_Item obj) => base.Equals(obj) && invert == obj.invert;
            public bool Include(Item_sel obj)
            => (Type > 0 ? (Type == obj.Type) : base.Equals(obj)) != invert;
            public override string ToString()
            => $"{(invert ? "!" : "")}{(Name)}";
            public string ToString(bool isSave)
             => isSave ? $"{(invert ? "!" : "+")}{Name}{Type:000}" : ToString();
        }

        public class Sel_Group : List<Sel_Item>, ISaving
        {
            public Sel_Group() { }
            public Sel_Group(IEnumerable<Sel_Item> collection) : base(collection) { Sort_(); }
            public new void Add(Sel_Item val)
            {
                if (FindIndex(x => x.Equals(val)) < 0)
                { base.Add(val); Sort_(); }
            }
            void Sort_() => Sort(delegate (Sel_Item a, Sel_Item b)
            { return a.invert == b.invert ? (a.Type == b.Type && a.Type == 0 ? 0 : a.Type == 0 ? -1 : 1) : a.invert ? -1 : 1; });
            public void AddNonExisting(Sel_Group val)
            { AddRange(val.FindAll(x => FindIndex(y => y.Equals(x)) >= 0)); Sort_(); }
            public bool In(Sel_Item val) => Find(x => x.Equals(val)) != null;
            public bool In(Item_sel val) => Find(x => x.Include(val)) != null;
            public bool Equals(Sel_Group val) => Count == val.Count && Count > 0 && FindIndex(x => val.FindIndex(y => x.Equals(y)) < 0) < 0;
            public override bool Equals(Object obj)
            {
                if (obj is Item_sel) return In(obj as Item_sel);
                if (obj is Sel_Group) return Equals(obj as Sel_Group);
                return obj is object &&
                       EqualityComparer<Object>.Default.Equals(this, obj as object);
            }
            public override string ToString() => ToString(false);
            public string ToString(bool isSave)
            {
                var s = new List<string>(Count);
                base.ForEach(x => s.Add(x.ToString(isSave)));
                return string.Join(isSave ? "," : ", ", s);
            }
            public MyTree Save(MyTree res)
            {
                if (res == null) res = new MyTree();
                ForEach(x => res.Add(new MyTree(x.ToString(true))));
                return res;
            }
            public static Sel_Group Parse(MyTree val)
            {
                var res = new Sel_Group();
                foreach (var v in val)
                    res.Add(FromStr(v.Param));
                return res;
            }
            public static Sel_Group Parse(string val)
            {
                var res = new Sel_Group();
                var t = val.Split(',');
                foreach (var v in t)
                    res.Add(FromStr(v));
                return res;
            }
            static Sel_Item FromStr(string v) => new Sel_Item(byte.Parse(v.Remove(0, v.Length - 3)), v.Substring(1, v.Length - 4), v[0] == '!');
            public override int GetHashCode()=> HashCode.Combine(Capacity, Count);
        }


        public class MyTree : List<MyTree>
        {
            public MyTree Owner = null;
            public string Name = null;
            public string Param = null;
            public string GetValue() => Name + (string.IsNullOrEmpty(Param) ? null : "=" + Param);
            public MyTree() { }
            public MyTree(string param) { Param = param; }
            public MyTree(string value, bool isName)
            {
                if (isName) { Name = value; return; }
                var i = value.IndexOf('=');
                if (i >= 0)
                {
                    Name = value.Substring(0, i);
                    Param = value.Substring(i + 1);
                }
                else Name = value;
            }

            public new void Add(MyTree tree) { base.Add(tree); tree.Owner = this; }
            public void Add(string name, MyTree tree)
            { tree.Name = name; Add(tree); }
            public void Parse(string value, char sym_break = '\t')
            {
                MyTree tec = this;
                int t_lev = 1, i = 0, b = 0;
                while (i >= 0)
                {
                    i = value.IndexOf('\n', b);
                    var str = value.Substring(b, (i < 0 ? value.Length : i) - b);
                    b = i + 1;
                    LoadPath(this, ref tec, str.TrimEnd(), ref t_lev, sym_break);
                }
            }
            static void LoadPath(MyTree NulLevObj, ref MyTree tec, string sel, ref int level, char sym)
            {
                int n = -1;
                sel.First(c => (++n) >= 0 && c != sym);

                if (n > level)
                    throw new Exception("Level error in name: " + sel);
                if (n == 0) { level = 0; tec = NulLevObj; }
                while (n < level) { tec = tec.Owner; level--; }

                string name;
                if (sel[n] == '[' && sel.EndsWith(']'))
                    name = sel.Substring(n + 1, sel.Length - n - 2);
                else name = n == 0 ? sel : sel.Remove(0, n);

                tec.Add(tec = new MyTree(name, false));
                level++;
            }
            public void ForLoop(Action<MyTree, int> Act, int s_lev = 0)
            {
                foreach (var t in this)
                {
                    Act(t, s_lev);
                    t.ForLoop(Act, s_lev + 1);
                }
            }
            public string ToString(char chapter_sym = '\t')
            {
                StringBuilder res = new StringBuilder();
                ForLoop((x, l) =>
                {
                    if (x.Count <= 0)
                        res.Append(new string(chapter_sym, l) + x.GetValue());
                    else
                        res.Append(new string(chapter_sym, l)).Append('[').Append(x.GetValue()).Append(']');
                    res.AppendLine();
                });
                return res.ToString();
            }
        }

        static void Main(string[] args)
        {
            var value = @"[PANELS]
 []
  [Select]
   =+Группа1001
  [Surface=True]
   L1=0
   [=False]
    L2=0
    L3=0
 []
  [Select]
   =!ГруппаНе5005
  [Surface=False]
   T1=0
   [=True]
    T2=0
    T3=0
 []
  [Select]
   =+Только7007
  [Surface=True]
   1=0
   2=0";

            /*MyIni i = new MyIni();
            MyIniParseResult re;
            if (!i.TryParse(value, out re)) { Console.WriteLine("Ошибка преобразования: " + re.ToString()); return; }
            /*
            var lk = new List<MyIniKey>();

            i.GetKeys(lk);
            lk.ForEach(x => Console.WriteLine($"{x.ToString()}: {i.Get(x).ToString()}"));*/
            Console.WriteLine(value + "\n****-************************");

            var t = new MyTree();
            t.Parse(value, ' ');
            TextOut<Sel_Group, Item_sel> a = new TextOut<Sel_Group, Item_sel>();

            var tmp = t.FirstOrDefault(x => x.Name.Equals("PaNELS", StringComparison.OrdinalIgnoreCase));
            if (tmp == null) { Console.WriteLine("Нет сектора PANELS"); return; }

            a.Load(tmp, Sel_Group.Parse, (l, n, i) => l.Add(new Txt_Surface_(n, 10, i)));

            Console.WriteLine(a.Save().ToString(' '));

            byte key = 12;
            Console.WriteLine(key.ToString("0"));
        }


        static void Main2(string[] args)
        {

            //Console.WriteLine((a.Save() as TreeINI).ToIniString());

        }

    }

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