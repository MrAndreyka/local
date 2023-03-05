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

        public interface ISaved { string ToString(bool isSave); }
        /*  public interface ITxt_null
          {
              string ToString(bool isSave);
              bool AddLine(string str, bool addAlways); //return false если панель полная
              void Restore();
          }*/
        public abstract class Txt_null : ISaved
        {
            public abstract string ToString(bool isSave);
            public abstract bool AddLine(string str, bool addAlways); //return false если панель полная
            public abstract void Restore();
        }

        class Txt_Surface : Txt_null
        {
            public readonly IMyTextSurface Surface;
            public readonly IMyTerminalBlock OwnerBloc;
            public readonly int index = 0;

            public Txt_Surface(IMyTextSurfaceProvider block, int index = 0)
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

        class Txt_Surface_ : Txt_null
        {
            public readonly MySurface Surface;
            public readonly string OwnerBloc;
            public readonly int index = 0;

            public Txt_Surface_(string block, int lines, int index = 0)
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

        public class Txt_Panel : Txt_null
        {
            List<Txt_null> flats { get; } = new List<Txt_null>();
            public List<Txt_null> Flats => flats;
            public bool hor;
            int _tec = 0;
            public Txt_Panel(bool horizontal, List<Txt_null> list = null)
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
                    if (!(f = flats[_tec].AddLine(str, false)))
                        f = Next(true);
                }
                if (!f && addAlways)
                    flats[flats.Count - 1].AddLine(str, true);
                return f;
            }

            bool AddLineVert(string str, bool addAlways)
            {
                bool f1 = false, fl = addAlways && IsEnd();
                while (!(f1 = flats[_tec].AddLine(str, fl) || !IsEnd()))
                    fl = Next(false) && addAlways;

                return f1;
            }
            public override void Restore() { _tec = 0; flats.ForEach(x => x.Restore()); }

            public delegate void FindPanel(List<Txt_null> list, string pan, int index);

            delegate bool skip(char x);
            public static string TryParse(Txt_Panel res, String val, ref int pos, FindPanel activ)
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
                        var p = new Txt_Panel(false);
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

        public class selected<T> : ISaved
        {
            T val;
            public selected(T v) { val = v; }
            public virtual string ToString(bool isSave)=>val.ToString();

            public static implicit operator T(selected<T> v)=>v.val;
            public static implicit operator selected<T>(T v) => new selected<T>(v);

        }
    
        public class Selected_Panel<T,T2>: Txt_Panel
        {
            public selected<T> Select;
            public Selected_Panel(T sel):base(false) {Select = sel; }
            //public void SetPanel(ITxt_null txt_) => txt = txt_;

            public bool AddLine(T2 sel, string str)
            {
                var fl = Select.Equals(sel);
                if (fl) AddLine(str, false);
                return fl;
            }
            public override string ToString(bool isSave = false)
            => $"{Select.ToString(isSave).ToParam('{')}{base.ToString(isSave)}";
        }

        public class TextOut<T, T2> : List<Selected_Panel<T, T2>>
        {
            public StringBuilder DefList = new StringBuilder();
            public void AddLine(T2 sel, string str, bool skipped = false)
            {
                if (FindIndex(x => x.AddLine(sel, str) == true) < 0 && !skipped)
                    DefList.AppendLine(str);
            }

            public void Restore() { DefList.Clear(); ForEach(x => x.Restore()); }
            public string ToString(bool isSave = false)
            {
                List<string> res = new List<string>(Count);
                ForEach(x => res.Add(x.ToString(isSave)));
                return string.Join(",", res);
            }
            public string TryParse(string val, ref int pos, Func<string, T> Parse, Txt_Panel.FindPanel findPanel)
            {
                int start = pos - 1;
                var list = new List<Selected_Panel<T, T2>>();
                do
                {
                    start++;
                    var tm = new Selected_Panel<T, T2>(Parse(val.GetParam(ref start, '{')));
                    if (FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в списке" + tm.Select.ToString(); }
                    if (list.FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в шаблоне " + tm.Select.ToString(); }

                    if (val.Skip(start).First(x => x != ' ' || ++start == 0) != '{')
                    { pos = start; return "Ожидается '{' в начале"; }

                    var er = Txt_Panel.TryParse(tm, val, ref start, findPanel);
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

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        static void Main2(string[] args)
        {


        }

        public abstract class Re_ : ISaved { public abstract string ToString(bool fl); }

        public class ReS : Re_
        {
            string value;
            public ReS(string v) { value = v; }
            public ReS() { }
            public override string ToString(bool fl)
            {
                return fl? value.ToUpper() : value;
            }
            public override string ToString()=> ToString(false);

            /*public static implicit operator T(Re<T> v) => v.value;
            public static implicit operator Re<T>(T v) => new Re<T>(v);*/
        }
        static void Main(string[] args)
        {
            string value = @"Ajdabwd f
C:\Program Files\dotnet\dotnet.exe (процесс 15700) завершил работу с кодом 0.
Чтобы автоматически закрывать консоль при остановке отладки, включите параметр 
""Сервис"" ->""Параметры"" ->""Отладка"" -> Автоматически закрыть консоль при остановке отладки
Нажмите любую клавишу, чтобы закрыть это окно…
        }";

            int i = 2, g = 0;
            value.First(x=>{ g++; return x == '\n' && (--i == 0); });
           // value.Take((x, a) => { Console.WriteLine($"{a} {x}");  return false; });
            //{ g++; return x != '\n' || (--i > 0); });

            Console.WriteLine(i);
            Console.WriteLine(g);
            Console.WriteLine("\"" + value.Substring(0,g)+ "\"");

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