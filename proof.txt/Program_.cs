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

        public interface IStsSave { string ToString(bool isSave); }
        public interface ISaving { MyTree Save(MyTree val = null); }

        public abstract class Txt_null : ISaving
        {
            public Txt_Panel Owner = null;
            public abstract string ToString(bool isSave);
            public abstract bool AddLine(string str, bool addAlways); //return false если панель полная
            public abstract void Restore();
            public abstract Txt_Surface FindPanel(Func<ITextSurf, bool> f);
            public abstract MyTree Save(MyTree res = null);
        }

        public class Txt_Surface : Txt_null, ITextSurf
        {
            public readonly IMyTextSurface Surface;
            private readonly IMyTerminalBlock ownerBloc;
            private readonly int index = 0;

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
            public override string ToString(bool isSave = false)
            => isSave ? $"{ownerBloc.EntityId}[{index}]" : $"{ownerBloc.CustomName.ToParam()} [{index}]";
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
            public override Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            => f(this) ? this : null;

            public override MyTree Save(MyTree res)
            {
                if (res == null) res = new MyTree();
                res.Value = ToString(true);
                return res;
            }
        }

        public class txt_Surface_ : Txt_Surface
        {
            public new readonly MySurface Surface;
            public new readonly string OwnerBloc;
            public readonly int index = 0;

            public int Index => index;

            // IMyTerminalBlock ITextSurf.OwnerBloc => throw new NotImplementedException();

            public txt_Surface_(string block, int lines, int index = 0):base(null, 0)
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
            // public override Txt_Surface FindPanel(Func<ITextSurf, bool> f) => this;
        }

        public class Txt_Panel : Txt_null, ISaving
        {
            List<Txt_null> flats { get; } = new List<Txt_null>();
            public List<Txt_null> Flats => flats;
            public bool hor;
            int _tec = 0;
            public Txt_Panel(bool horizontal, List<Txt_null> list = null)
            { hor = horizontal; if (list != null) flats = list; }
            public override string ToString(bool isSave = false)
            {
                var tmp = new List<string>(flats.Count);
                flats.ForEach(x => tmp.Add(x.ToString(isSave)));
                return $"{{{string.Join(", ", tmp)}:{(isSave ? (hor ? "+" : "-") : (hor ? "Horizontal" : "Vertical"))}}}";
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
            public delegate void FndPanel(List<Txt_null> list, string pan, int index);
            public override Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                flats.FindIndex(x => (p = x.FindPanel(f)) != null);
                return p;
            }
            public override MyTree Save(MyTree res = null)
            {
                if (res == null) res = new MyTree();
                res.Value = hor.ToString();
                int n = 0;
                Flats.ForEach(x => res.Add(x.Save()));
                return res;
            }
            public static string TryParse(Txt_Panel res, String val, ref int pos, FndPanel activ)
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
                    { start++; ind = val.GetParam(ref start, ']').AsInt(0); start++; }

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


        public class Selected_Panel<T, T2> : Txt_Panel, ISaving
        {
            public T Select;
            public Selected_Panel(T sel) : base(false)
            {
                if (!(sel is ISaving))
                    throw new Exception("Недопустимый тип наследования для Selected_Panel");
                Select = sel;
            }

            public bool AddLine(T2 sel, string str)
            {
                var fl = Select.Equals(sel);
                if (fl) base.AddLine(str);
                return fl;
            }
            public override string ToString(bool isSave = false)
            => $"{(Select as ISaving).ToString().ToParam('{')}{base.ToString(isSave)}";
            public new MyTree Save(MyTree res)
            {
                if (res == null) res = new MyTree(); 
                res.Add("Select", (Select as ISaving).Save());
                res.Add("Surfase", new MyTree());
                base.Save(res.Last());
                return res;
            }
        }

        public class TextOut<T, T2> : List<Selected_Panel<T, T2>>, ISaving
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
            public string ToString(bool isSave = false)
            {
                List<string> res = new List<string>(Count);
                ForEach(x => res.Add(x.ToString(isSave)));
                return string.Join(isSave ? "," : "\n", res);
            }
            public Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                FindIndex(x => (p = x.FindPanel(f)) != null);
                return p;
            }
            public string TryParse(string val, ref int pos, Func<string, T> Parse, Txt_Panel.FndPanel findPanel)
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
            public MyTree Save(MyTree res)
            {
                if (res == null) res = new MyTree(); int n = 0;
                ForEach(x => res.Add(n++.ToString(), x.Save(null)));
                return res;
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
             => isSave ? $"{(invert ? "+" : "-")}{Type:000}{Name}" : ToString();
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
                if(res==null) res = new MyTree();
                int n = 0;
                ForEach(x => res.Add(new MyTree(x.ToString(true))));
                return res;                
            }
            public static Sel_Group Parse(string val)
            {
                var res = new Sel_Group();
                var t = val.Split(',');
                foreach (var v in t)
                    res.Add(new Sel_Item(byte.Parse(v.Substring(1, 3)), v.Substring(4, v.Length - 4), v[0] == '+'));
                return res;
            }
            /*  public override int GetHashCode()
              {
                  int hashCode = -1591213915;
                  hashCode = hashCode * -1521134295 + Capacity.GetHashCode();
                  hashCode = hashCode * -1521134295 + Count.GetHashCode();
                  return hashCode;
              }*/
        }



    /*    public class MyTree_ : Dictionary<string, MyTree>
        {
            public string Val = null;
            public MyTree Owner = null;
            public MyTree_() { }
            public MyTree_(MyTree_ owner) { Owner = owner; }
            public new void Add(string key, MyTree_ val)
            {base.Add(key, val); if (val != null) val.Owner = this; }
            public void Add(string key, string val)=>Add(key, new MyTree_() {Val = val });
            public override string ToString() => (Val == null ? "" : $"{Val}:") + Count.ToString() + "...";
            public void ForLoop(Action<KeyValuePair<string, MyTree_>, int, int> Act, int s_lev = 0, int p_lev = 0)
            {
                foreach (var t in this)
                {
                    Act(t, s_lev, p_lev);
                    if (t.Value != null)
                        t.Value.ForLoop(Act, s_lev + 1, t.Value.Val == null?0:p_lev+1);
                }
            }
        }

        public class TreeINI : MyTree_
        {
            public TreeINI() { }
            public bool AddINI(string val, char chapter_sym = ' ', char name_sym = '_')
            {
                var pi = new MyIni();
                if (!pi.TryParse(val)) return false;

                var lk = new List<MyIniKey>();
                var ls = new List<string>();
                pi.GetSections(ls);

                int tl, tu = 0;
                MyTree_ tec = this, tec2;

                foreach (var sel in ls)
                {
                    LoadPath(this, ref tec, sel, ref tu, chapter_sym);
                    pi.GetKeys(sel, lk);

                    tec2 = tec; tl = tu;
                    lk.ForEach(x => { LoadPath(tec, ref tec2, x.Name, ref tl, name_sym); tec2.Val = pi.Get(x).ToString(); });
                }
                return true;
            }
            static void LoadPath(MyTree_ NulLevObj, ref MyTree_ tec, string sel, ref int level, char sym)
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
                }
                else { level = 0; tec = NulLevObj; name = sel; }

                tec.Add(name, tec = new MyTree());
                level++;
            }
            public string ToIniString(char chapter_sym = ' ', char name_sym = '_')
            {
                StringBuilder res = new StringBuilder();
                ForLoop((x, l1, l2) =>
                {
                    if (x.Value.Val == null)
                        res.Append("[" + new string(chapter_sym, l1) + x.Key + "]");
                    else res.Append(new string(name_sym, l2) + x.Key + "=" + x.Value.Val);
                    res.AppendLine();
                });
                return res.ToString();
            }
        }*/

        public class MyTree:List<MyTree>
        {   public string Value = null;
            public MyTree Owner = null;
            public MyTree() { }
            public MyTree(string val) { Value = val; }
            public new void Add(MyTree tree) { base.Add(tree); tree.Owner = this; }
            public void Add(string Val, MyTree tree) 
            { tree.Value = Val; Add(tree); }
            public void Parse(string value, char sym_break = '\t') 
            {
                MyTree tec = this;
                int t_lev = 1, i = 0, b=-1;
                while (b!=i)
                {
                    i = value.IndexOf('\n', b = i);
                    if (i < 0) b = i = value.Length;
                    var str = value.Substring(i, b - i);
                    LoadPath(this, ref tec, str, ref t_lev, sym_break);
                }
            }
            static void LoadPath(MyTree NulLevObj, ref MyTree tec, string sel, ref int level, char sym)
            {
                string name;
                int n = -1;
                sel.First(c => ++n > 0 && c != sym);

                if (n > level)
                    throw new Exception("Level error in name: " + sel);
                if( n==0 ) { level = 0; tec = NulLevObj;}
                while (n < level ) { tec = tec.Owner; level--; }
                if (sel[n] == '[' && sel.EndsWith(']'))
                    name = sel.Substring(n + 1, sel.Length - n - 1);
                else name = n == 0 ? sel : sel.Remove(0, n);

                tec.Add(tec = new MyTree(name));
                level++;
            }
            /*public static void LoadPath2(MyTree2 NulLevObj, ref MyTree2 tec, string name, ref int level, int n)
            {

                if (n > level)
                    throw new Exception("Level error in name: " + name);
                if (n == 0) { level = 0; tec = NulLevObj; }
                while (n < level) { tec = tec.Owner; level--; }
               
                tec.Add(tec = new MyTree2(name));
                level++;
            }*/
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
                    if (x.Count > 0 )
                        res.Append(new string(chapter_sym, l) + "[" + x.Value + "]");
                    else res.Append(new string(chapter_sym, l) + x.Value);
                    res.AppendLine();
                });
                return res.ToString();
            }
        }

        static void Main2(string[] args)
        {
            var value = @"[PANELS]
0=True
_0=L1[0]
_1=False
__0=L2[0]
__1=L3[0]
[  Select]
0=-001Группа1
1=False
_0=T1[0]
_1=True
__0=T2[0]
__1=T3[0]
[  Select]
0=+005ГруппаНе5
2=True
_0=1[0]
_1=2[0]
[  Select]
0=+007Только7";

            MyIni i = new MyIni();
            MyIniParseResult re;
            if (!i.TryParse(value, out re)) { Console.WriteLine("Ошибка преобразования: " + re.ToString()); return; }
            /*
            var lk = new List<MyIniKey>();

            i.GetKeys(lk);
            lk.ForEach(x => Console.WriteLine($"{x.ToString()}: {i.Get(x).ToString()}"));*/
            Console.WriteLine(value + "\n****-************************");

            var t = new MyTree();

            Console.WriteLine(t.ToString());
        }        
       

        static void Main(string[] args)
        {
            int start = 0;
            TextOut<Sel_Group, Item_sel> a = new TextOut<Sel_Group, Item_sel>();


            var er = a.TryParse("-001Группа1{ L1[0], { L2[0], L3[0]:0}:1},+005ГруппаНе5{ T1[0], { T2[0], T3[0]:1}:0},+007Только7{ 1[0], 2[0]:1}",
                ref start, x => Sel_Group.Parse(x), (l, n, i) => l.Add(new txt_Surface_(n, 10, i)));
            if (er != null) { Console.WriteLine(er + "\t" + start.ToString()); return; }
            start++;

            for (var i = 0; i < 50; i++)
                //a.AddLine(new Item_sel((byte)(i % 10 + 1), i.ToString()), $"Str - {i}");//
                a.AddLine(new Item_sel((byte)(i % 10 + 1)), i.ToString());

            Console.WriteLine(a.ToString(false));

            Console.WriteLine("\n=======================================\n");
            Console.WriteLine(a.ToString(true));

            

            var u = new MyTree();
            u.Add("PANELS", a.Save());

            
            Console.WriteLine("\n=======================================\n");
            Console.WriteLine(u.ToString());


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