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
        public class MySurface //: IMyTextSurface
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

        public abstract class txt_null
        {
            public abstract string ToString(bool isSave);
            public abstract bool AddLine(string str, bool addAlways); //return false если панель полная
            public abstract void Restore();
        }

        class txt_Surface : txt_null
        {
            public readonly IMyTextSurface Surface;
            public readonly IMyTerminalBlock OwnerBloc;
            public readonly int index = 0;

            public txt_Surface(IMyTextSurfaceProvider block, int index = 0)
            {
                if (block.SurfaceCount == 0) throw new Exception("The block does not contain surface");
                OwnerBloc = (IMyTerminalBlock)block;
                Surface = block.GetSurface(index);
                Surface.ContentType = ContentType.TEXT_AND_IMAGE;
            }
            public override string ToString(bool isSave = false)
            => isSave ? $"{OwnerBloc.EntityId}[{index}]" : $"{OwnerBloc.CustomName.ToParam()} [{index}]";
            public override bool AddLine(string str, bool addAlways = true)
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
            public override void Restore() { Surface.WriteText(string.Empty); }
        }

        class txt_Surface_ : txt_null
        {
            public readonly MySurface Surface;
            public readonly string OwnerBloc;
            public readonly int index = 0;

            public txt_Surface_(string block, int lines, int index = 0)
            {
                OwnerBloc = block;
                Surface = new MySurface(lines);
                this.index = index;
            }
            public override string ToString(bool isSave = false)
            {
                if (!isSave) return $"{OwnerBloc} [{index}]\n{Surface.ToString()}";
                return $"{OwnerBloc.ToParam()}[{index}]";
            }
            public override void Restore() { Surface.WriteText(string.Empty); }
            public override bool AddLine(string str, bool addAlways)
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
        }

        public class txt_Panel : txt_null
        {
            List<txt_null> flats { get; } = new List<txt_null>();
            public List<txt_null> Flats => flats;
            public bool hor;
            int _tec = 0;
            public txt_Panel(bool horizontal, List<txt_null> list = null)
            { hor = horizontal; if (list != null) flats = list; }
            public override string ToString(bool isSave = false)
            {
                var res = new StringBuilder("{");
                var tmp = new List<string>(flats.Count);
                flats.ForEach(x => tmp.Add(x.ToString(isSave)));
                res.Append($"{string.Join(", ", tmp)}:{(isSave ? (hor ? "1" : "0") : (hor ? "Horizontal" : "Vertical"))}}}");
                return res.ToString();
            }

            public bool IsEnd() => _tec > flats.Count;
            //Сдвигается к следующей позиции, возвращает истина если оказывается на последней позиции
            bool Next(bool to_round = false)
            => (!IsEnd() || to_round) && (++_tec < flats.Count) || ((_tec = 0) == 0);

            public override bool AddLine(string str, bool addAlways = true)
            {
                if (flats.Count == 0) return false;
                return hor ? AddLineHor(str, addAlways) : AddLineVert(str, addAlways);
            }

            bool AddLineHor(string str, bool addAlways)
            {
                var t = flats.Count;
                bool f = false;
                while (t-- > 0 && !f)
                {
                    f = flats[_tec].AddLine(str, false);
                    Next(true);
                }
                if (!f && addAlways)
                    flats[flats.Count - 1].AddLine(str, true);
                return f;
            }

            bool AddLineVert(string str, bool addAlways)
            {
                bool f1 = false, fl = addAlways && IsEnd();
                while (!(f1 = flats[_tec].AddLine(str, fl)) && !IsEnd())
                    fl = Next(false) && addAlways;

                return f1;
            }
            public override void Restore() { _tec = 0; flats.ForEach(x => x.Restore()); }

            public delegate void FindPanel(List<txt_null> list, string pan, int index);

            delegate bool skip(char x);
            public static string TryParse(txt_Panel res, String val, ref int pos, FindPanel activ)
            {
                int ind = 0, start = pos;


                if (val.Skip(start).First(x => x != ' ' || ++start == 0) != '{')
                { pos = start; return "Ожидается '{' в начале"; }

                //if(res == null) res = new txt_Panel(new List<txt_null>());
                do
                {
                    start++;
                    if (val.Skip(start).First(x => x != ' ' || ++start == 0) == '{')
                    {
                        var p = new txt_Panel(false);
                        var r = TryParse(p, val, ref start, activ);
                        pos = ++start;
                        if (p == null) return r;
                        res.flats.Add(p);
                        continue;
                    }

                    var name = val.GetParam(ref start, ',', '[', ':');
                    if (string.IsNullOrEmpty(name)) { pos = start; return "Ожидается название панели"; }

                    if (val[start] == '[')
                    { start++; ind = val.GetParam(ref start, ']').asInt(0); start++; }

                    activ(res.flats, name, ind);
                } while (start < val.Length && val.Skip(start).First(x => x != ' ' || ++start == 0) == ',');

                ind = start;
                start = val.IndexOf('}', start);
                if (start < 0) { pos = ind; return "Ожидается '}'"; }

                if (val[ind] == ':')
                    res.hor = val.Skip(++ind).First(x => x != ' ' || ++start == 0) == '1';

                pos = start;
                return null;
            }
        }

        public class selected_Panel<T, T2> : txt_Panel
        {
            public T Select;
            public selected_Panel(T sel) : base(false) { Select = sel; }

            public bool AddLine(T2 sel, string str)
            {
                var fl = Select.Equals(sel);
                if (fl)
                    AddLine(str);
                return fl;
            }
            public override string ToString(bool isSave = false)
            => $"{Select.ToString().ToParam('{')}{base.ToString(isSave)}";

            public override int GetHashCode()
            {
                return HashCode.Combine(Select);
            }
        }

        public class TextOut<T, T2> : List<selected_Panel<T, T2>>
        {
            public StringBuilder DefList = new StringBuilder();
            public void AddLine(T2 sel, string str, bool skipped = false)
            {
                if (FindIndex(x => x.AddLine(sel, str) == true) < 0 && !skipped)
                    DefList.AppendLine(str);
            }

            void Restore() { DefList.Clear(); ForEach(x => x.Restore()); }
            public string ToString(bool isSave = false)
            {
                List<string> res = new List<string>(Count);
                ForEach(x => res.Add(x.ToString(isSave)));
                return string.Join(",", res);
            }
            public string TryParse(string val, ref int pos, Func<string, T> Parse, txt_Panel.FindPanel findPanel)
            {
                int start = pos - 1;
                var list = new List<selected_Panel<T, T2>>();
                do
                {
                    start++;
                    var tm = new selected_Panel<T, T2>(Parse(val.GetParam(ref start, '{')));
                    if (FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в списке" + tm.Select.ToString(); }
                    if (list.FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в шаблоне " + tm.Select.ToString(); }

                    if (val.Skip(start).First(x => x != ' ' || ++start == 0) != '{')
                    { pos = start; return "Ожидается '{' в начале"; }

                    var er = txt_Panel.TryParse(tm, val, ref start, findPanel);
                    if (er != null) { pos = start; return er; }
                    list.Add(tm);
                } while (++start < val.Length && val.Skip(start).First(x => x != ' ' || ++start == 0) == ',');

                AddRange(list);
                return null;
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
            public bool invert, isgroup;

            public Sel_Item(byte Type, string Name = null) : base(Type, Name) { }

            public bool Equals(Sel_Item obj) => base.Equals(obj) && invert == obj.invert && isgroup == obj.isgroup;
            public bool Include(Item_sel obj)
            => (isgroup ? (Type == obj.Type) : base.Equals(obj)) != invert;
            public override string ToString()
            => $"{Type}{Name}{(invert ? "+" : "-")}{(isgroup ? "+" : "-")}";

        }

        public class Sel_Group : List<Sel_Item>
        {
            public Sel_Group() { }
            public Sel_Group(IEnumerable<Sel_Item> collection) : base(collection) { }
            public bool Equals(Sel_Group val) => Count == val.Count && Count > 0 && FindIndex(x => val.FindIndex(y => x.Equals(y)) < 0) < 0;
            public new void Add(Sel_Item val)
            {
                if (FindIndex(x => x.Equals(val)) < 0)
                { base.Add(val); Sort_(); }
            }
            void Sort_() => Sort(delegate (Sel_Item a, Sel_Item b) { return a.invert == b.invert ? (a.isgroup == b.isgroup ? 0 : a.isgroup ? -1 : 1) : a.invert ? -1 : 1; });
            public void AddNonExisting(Sel_Item val)
            { AddRange(FindAll(x => FindIndex(y => y.Equals(x)) >= 0)); Sort_(); }
            public bool In(Sel_Item val) => Find(x => x.Equals(val)) != null;
            public bool In(Item_sel val) => Find(x => x.Include(val)) != null;

            public override bool Equals(Object obj)
            {
                if (obj is Item_sel) return In(obj as Item_sel);
                return obj is Object _sel &&
                       EqualityComparer<Object>.Default.Equals(this, _sel);
            }

            public override string ToString()
            => string.Join(";", this);

            public static Sel_Group Parse(string val)
            {
                var res = new Sel_Group();
                var t = val.Split(';');
                foreach (var v in t)
                {
                    var t2 = new Sel_Item(byte.Parse(v.Substring(0, 3)), v.Substring(3, v.Length - 5));
                    t2.invert = v[v.Length - 2] == '+';
                    t2.isgroup = v[v.Length - 1] == '+';
                    res.Add(t2);
                }
                return res;
            }
        }


        public class MyTree : Dictionary<string, MyTree>
        {
            public string Value = null;
            public MyTree Owner = null;
            public MyTree() { }
            public MyTree(MyTree owner) { Owner = owner; }
            public new void Add(string key, MyTree val)
            { base.Add(key, val); if (val != null) val.Owner = this; }
            public override string ToString() => (Value == null ? "" : $"{Value}:") + Count.ToString() + "...";
        }

        public class TreeINI : MyTree
        {
            public TreeINI() { }
            public bool AddINI(string val, char chapter_sym = '_', char name_sym = '\n')
            {
                var pi = new MyIni();
                if (!pi.TryParse(val)) return false;

                var lk = new List<MyIniKey>();
                var ls = new List<string>();
                pi.GetSections(ls);

                int tl, tu = 0;
                MyTree tec = this, tec2;

                foreach (var sel in ls)
                {
                    LoadPath(this, ref tec, sel, ref tu, chapter_sym);
                    pi.GetKeys(sel, lk);

                    tec2 = tec; tl = tu;
                    lk.ForEach(x => { LoadPath(tec, ref tec2, x.Name, ref tl, name_sym); tec2.Value = pi.Get(x).ToString(); });
                }
                return true;
            }

            static void LoadPath(MyTree NulLevObj, ref MyTree tec, string sel, ref int level, char sym)
            {
                string name;
                if (sel.StartsWith(sym))
                {
                    int n = -1;
                    sel.First(c => ++n > 0 && c != sym);
                    if (n > level)
                        throw new Exception("Level error in name: " + sel);
                    while (n < level)
                    {
                        if ((tec = tec.Owner) == null)
                            throw new Exception("Error in algoritm AddINI");
                        level--;
                    }
                    name = sel.Remove(0, n);
                } else { level = 0; tec = NulLevObj; name = sel; }

                tec.Add(name, tec = new MyTree());
                level++;
            }
        }

        static void Main(string[] args)
        {
            var value = @"[Panels]
[_1]
1.1=+002Components
1.2=+003Ores
1=test
1.1.info=info
[__TXT]
1=Lcd[1]
_1=Lcd[1_1]
_2=Lcd[1_2]
2=Lcd2[0]
1.2=Lcd[0]
[___Position]
1.2=True";
            MyIni i = new MyIni();
            if (!i.TryParse(value)) { Console.WriteLine("Ошибка преобразования"); return; }

            var lk = new List<MyIniKey>();

            i.GetKeys(lk);
            lk.ForEach(x => Console.WriteLine($"{x.ToString()}: {i.Get(x).ToString()}"));
            Console.WriteLine("****-************************");

            var t = new TreeINI();
            t.AddINI(value, '_', '_');

            public delegate void write(KeyValuePair<string, MyTree> x);
 
            foreach(var tt in t)
                write(tt);



                Console.WriteLine(tt.ToString());


        }

        static void Main2(string[] args)
        {
            int start = 0;
            TextOut<Sel_Group, Item_sel> a = new TextOut<Sel_Group, Item_sel>();

            var er = a.TryParse("001Группа1-+{ L1[0], { L2[0], L3[0]:0}:1},005ГруппаНе5++{ T1[0], { T2[0], T3[0]:1}:0},007Только7+-{ 1[0], 2[0]:1}",
                ref start, x => Sel_Group.Parse(x), (l, n, i) => l.Add(new txt_Surface_(n, 10, i)));
            if (er != null) { Console.WriteLine(er + "\t" + start.ToString()); return; }
            start++;

            for (var i = 0; i < 50; i++)
                //a.AddLine(new Item_sel((byte)(i % 10 + 1), i.ToString()), $"Str - {i}");//
                a.AddLine(new Item_sel((byte)(i % 10 + 1), i.ToString()));

            Console.WriteLine(a.ToString(false));
            Console.WriteLine("\n=======================================\n");

            Console.WriteLine(a.ToString(true));
        }

    }

    static class Class1
    {
        public static void AddLine(this Program.TextOut<Program.Sel_Group, Program.Item_sel> pan, Program.Item_sel val, bool skipped = false)
        => pan.AddLine(val, $"{val.Name} /{val.Type}", skipped);


        public delegate void ShowError(string val);
        public static int asInt(this string val, int def = 0) => int.TryParse(val, out int res) ? res : def;
        public static int asInt(this string val, ShowError func)
        {
            if (!int.TryParse(val, out int res)) func(val);
            return res;
        }
        public static long asLong(this string val, int def = 0) => long.TryParse(val, out long res) ? res : def;
        public static long asLong(this string val, ShowError func)
        {
            if (!long.TryParse(val, out long res)) func(val);
            return res;
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
    }
}