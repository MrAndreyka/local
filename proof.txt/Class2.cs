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
    partial class Program
    {
        bool Between(int val, int min, int max) => val >= min && val <= max;

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
            IMyTerminalBlock OwnerBloc {get;}
            int Index {get;}
        }

        public interface ISaving { string ToString(bool isSave); }
        public abstract class Txt_null:ISaving
        {
            public Txt_Panel Owner;
            public abstract string ToString(bool isSave);
            public abstract bool AddLine(string str, bool addAlways); //return false если панель полная
            public abstract void Restore();
            public abstract Txt_Surface FindPanel(Func<ITextSurf, bool> f);
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

            public override Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                flats.FindIndex(x => (p=x.FindPanel(f)) != null);
                return p;
            }
            public static string TryParse(Txt_Panel res, String val, ref int pos, Func<string, int, List<IMyTextSurfaceProvider>>f)
            {
                int ind = 0, start = pos;
                if (val.Skip(start).First(x => x != ' ' || ++start == 0) != '{')
                { pos = start; return "Ожидается '{' в начале"; }

                do
                {
                    start++;
                    if (val.Skip(start).First(x => x != ' ' || ++start == 0) == '{')
                    {
                        var pn = new Txt_Panel(false);
                        var r = TryParse(pn, val, ref start, f);
                        pos = ++start;
                        if (pn == null) return r;
                        pn.Owner = res;
                        res.flats.Add(pn);
                        continue;
                    }

                    var name = val.GetParam(ref start, ',', '[', ':');
                    if (string.IsNullOrEmpty(name)) { pos = start; return "Ожидается название панели"; }

                    if (val[start] == '[')
                    { start++; ind = val.GetParam(ref start, ']').AsInt(0); start++; }

                    var p = f(name, ind);
                    p.ForEach(x => { res.flats.Add(new Txt_Surface(x, ind) { Owner = res }); });
                } while (start < val.Length && val.Skip(start).First(x => x != ' ' || ++start == 0) == ',');

                ind = start;
                start = val.IndexOf('}', start);
                if (start < 0) { pos = ind; return "Ожидается '}'"; }

                if (val[ind] == ':')
                    res.hor = val.Skip(++ind).First(x => x != ' ' || ++start == 0) == '+';

                pos = start;
                return null;
            }
        }

  
        public class Selected_Panel<T, T2 >: Txt_Panel
        {
            public T Select;
            public Selected_Panel(T sel) :base(false) 
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
            => $"{(Select as ISaving).ToString(isSave).ToParam('{')}{base.ToString(isSave)}";
        }

        public class TextOut<T, T2> : List<Selected_Panel<T, T2>>
        {
            public StringBuilder DefList = new StringBuilder();
            public void AddLine(T2 sel, string str, bool skipped = false)
            {
                if (FindIndex(x => x.AddLine(sel, str) == true) < 0 && !skipped)
                    DefList.AppendLine(str);
            }
            public void AddLine(string Val)=>DefList.Append(Val);
            public bool IsSpec(T2 sel) => FindIndex(x=>x.Select.Equals(sel)) >= 0;
            public void Restore() { DefList.Clear(); ForEach(x => x.Restore()); }
            public string ToString(bool isSave = false)
            {
                List<string> res = new List<string>(Count);
                ForEach(x => res.Add(x.ToString(isSave)));
                return string.Join(isSave?",":"\n", res);
            }
            public Txt_Surface FindPanel(Func<ITextSurf, bool> f)
            {
                Txt_Surface p = null;
                FindIndex(x => (p = x.FindPanel(f)) != null);
                return p;
            }
            public string TryParse(string val, ref int pos, Func<string, T> fs, Func<string, int, List<IMyTextSurfaceProvider>> fp)
            {
                int start = pos - 1;
                var list = new List<Selected_Panel<T, T2>>();
                do
                {
                    start++;
                    var tm = new Selected_Panel<T, T2>(fs(val.GetParam(ref start, '{')));
                    if (FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в списке" + tm.Select.ToString(); }
                    if (list.FindIndex(x => x.Equals(tm.Select)) >= 0)
                    { pos = start; return "Дублирование отбора в шаблоне " + tm.Select.ToString(); }

                    if (val.Skip(start).First(x => x != ' ' || ++start == 0) != '{')
                    { pos = start; return "Ожидается '{' в начале"; }

                    var er = Txt_Panel.TryParse(tm, val, ref start, fp);
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
            public bool invert;
            public Sel_Item(byte Type, string Name = null, bool invert = false) : base(Type, Name) 
            {this.invert = invert;}
            public bool Equals(Sel_Item obj) => base.Equals(obj) && invert == obj.invert;
            public bool Include(Item_sel obj)
            => (Type>0 ? (Type == obj.Type) : base.Equals(obj)) != invert;
            public override string ToString()
            => $"{(invert ? "!" : "")}{LangDic.GetName(Name)}";
            public string ToString(bool isSave)
             => isSave? $"{(invert ? "+" : "-")}{Type:000}{Name}" : ToString();
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
            { return a.invert == b.invert ? (a.Type == b.Type && a.Type == 0 ? 0 : a.Type==0 ? -1 : 1) : a.invert ? -1 : 1; });
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
                return string.Join(isSave?",":", ", s);
            }
            public static Sel_Group Parse(string val)
            {
                var res = new Sel_Group();
                var t = val.Split(',');
                foreach (var v in t)
                {
                    var t2 = new Sel_Item(byte.Parse(v.Substring(1, 3)), v.Substring(4, v.Length - 4));
                    t2.invert = v[0] == '+';
                    res.Add(t2);
                }
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

        public class MyInvIt : Item_sel
        {
            public string ShowName { get; protected set; }
            static readonly Dictionary<MyItemType, MyInvIt> Names = new Dictionary<MyItemType, MyInvIt>();

            public MyInvIt(string Name, bool SetShow, byte Type = 0) : base(Type, Name) { if (SetShow) SetShowName(); }
            public static MyInvIt Get(MyItemType val)
            {
                MyInvIt v;
                if (Names.TryGetValue(val, out v)) return v;

                string s = val.TypeId.ToString(), name = val.SubtypeId.ToString();

                if (s.EndsWith("Ore")) name += "Ore";
                else if (s.EndsWith("Ingot")) name += "Ingot";
                else if (s.EndsWith("GunObject")) name = name.Replace("Item", "");
                s = s.Substring(16);
                v = new MyInvIt(name, true, GetType(s));
                if (v.Type == 7) v.ShowName += "_" + s;
                Names.Add(val, v);
                return v;
            }

            public static byte GetType(string val)
            {
                byte tp;
                switch (val)
                {
                    case "Component": tp = 2; break;
                    case "PhysicalGunObject": tp = 3; break;
                    case "AmmoMagazine": tp = 4; break;
                    case "Ore": tp = 5; break;
                    case "Ingot": tp = 6; break;
                    case "Components": tp = 2; break;
					case "HandTool": tp = 3; break;
					case "Ammo": tp = 4; break;
                    default: if (val.EndsWith("ContainerObject")) tp = 3; else tp = 7; break;
                }
                return tp;
            }
            void SetShowName() => ShowName = LangDic.GetName(Name);//+"|"+Type+"|"+Name; 
            public override string ToString() => ShowName;
        }
        public class MyInvItem
        {
            public readonly MyInvIt Lnk;
            public double count;
            public string ShowName { get { return Lnk.ShowName; } }

            public MyInvItem(MyInvIt val, double Cou = 0) { Lnk = val; count = Cou; }
            public MyInvItem(MyInventoryItem val)
            {
                Lnk = MyInvIt.Get(val.Type);
                count = (double)val.Amount;
            }
            protected MyInvItem(string value) : this(new MyInvIt(value, true, MyInvIt.GetType(LangDic.GetParentByKey(x => x.Equals(value))))) { }

            public override string ToString() => $"{Lnk.ShowName}: {count.ToString("#,##0.##", SYS)}";
            public string ToString(string msk) => string.Format(msk, Lnk.Name, Lnk.ShowName, count, Lnk.Type);
            public MyInvItem Clone(double Count = double.NaN) => new MyInvItem(Lnk, double.IsNaN(Count) ? this.count : Count);
            public static MyInvItem Parse(string val)
            { MyInvItem res; if (!TryParse(val, out res)) throw new Exception("Error in parse"); return res; }
            public static bool TryParse(string val, out MyInvItem res)
            {
                res = null;
                var ss = val.Split('|');
                byte tp; double cou;
                if (!byte.TryParse(ss[1], out tp) || !double.TryParse(ss[2], out cou)) return false;
                res = new MyInvItem(new MyInvIt(ss[0], true, tp)) { count = cou };
                return true;
            }
        }

        public class MyBuild_Item : MyInvItem
        {
            public readonly MyDefinitionId IDDef;

            public MyBuild_Item(MyProductionItem val) : this(val.BlueprintId) { count = (double)val.Amount; }
            MyBuild_Item(MyDefinitionId val) : base(GetName(val)) { IDDef = val; }
            static string GetName(MyDefinitionId val)
            {
                var s = val.SubtypeName;
                if (s.EndsWith("Component")) s = s.Replace("Component", "");
                else if (s.EndsWith("Magazine")) s = s.Replace("Magazine", "");
                else if (s.EndsWith("ConsumableItem")) s = s.Replace("ConsumableItem", "");
                else if (s.EndsWith("PhysicalObject")) s = s.Replace("PhysicalObject", "");
                return s;
            }
            public string ToSave() => IDDef.ToString() + ToString("|{3}|{2}");
            public new static MyBuild_Item Parse(string val)
            { MyBuild_Item res; if (!TryParse(val, out res)) throw new Exception("Error in parse"); return res; }
            public static bool TryParse(string val, out MyBuild_Item res)
            {
                res = null;
                var ss = val.Split('|');
                byte tp; double cou;
                if (!byte.TryParse(ss[1], out tp) || !double.TryParse(ss[2], out cou)) return false;
                MyDefinitionId b = MyDefinitionId.Parse(ss[0]);
                res = new MyBuild_Item(b) { count = cou };
                return true;
            }
        }

        public class MyRef : Sel_Group
        {
            public InvDT Inv { get; }
            public MyRef(IMyTerminalBlock Bloc) { Inv = new InvDT(Bloc); }
            public override string ToString()
            {
                var ls = new List<string>();
                ForEach(x => ls.Add(x.ToString()));
                return Inv + ": " + string.Join(" ", ls);
            }

            public static MyRef Parse(IMyGridTerminalSystem Gp, string val)
            {
                var m = val.Split(';');
                var bl = Gp.GetBlockWithId(long.Parse(m[0]));
                var res = new MyRef(bl);
                for (var i = 1; i < m.Length && !string.IsNullOrWhiteSpace(m[i]); i++) res.AddList(Parse(m[i]));
                return res;
            }

            public string ToSave()
            {
                var bl = (Inv.Owner as IMyTerminalBlock);
                if (bl == null) return string.Empty;
                return bl.EntityId.ToString() + ";" + string.Join(";", this);
            }
        }
        public class Sklads : List<MyRef>
        {
            public int GetInv_Move(Item_sel It, IMyInventory Inv, ref int i)
            {
                int beg = i;
                if (beg == 0)
                {
                    i = FindIndex(x => x.Inv.Inventory == Inv);
                    if (i >= 0 && this[i].In(It))
                        return i = -1;
                }

                for (i = beg; i < Count; i++)
                {
                    if (this[i].Inv.Inventory != Inv
                        && Inv.IsConnectedTo(this[i].Inv.Inventory)
                        && this[i].In(It)
                        && (this[i].Inv.Inventory.MaxVolume - this[i].Inv.Inventory.CurrentVolume).RawValue > 100)
                        return i;
                }
                return i = -1;// beg < Count ? beg : -1; 
            }
        }
        public class Selection
        {
            byte key = 0; string Val;
            public bool inv; public IMyCubeGrid GR = null;
            public string Value { get { return Val; } set { SetSel(value); } }

            public Selection(string val, IMyCubeGrid grid = null) { Value = val; GR = grid; }
            public Selection Change(string val) { Value = val; return this; }
            void SetSel(string val)
            {
                Val = ""; key = 0;
                if (string.IsNullOrEmpty(val)) return;
                inv = val.StartsWith("!"); if (inv) val = val.Remove(0, 1);
                if (string.IsNullOrEmpty(val)) return;
                int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
                if (Pos == 0) Pos = 0;
                else if (Pos == 1) Pos = 1;
                else if (Pos == val.Length) Pos = 2;
                else Pos = Pos < 4 ? 1 : 3;
                if (Pos != 0)
                {
                    if (Pos != 2) val = val.Remove(0, 1);
                    if (Pos != 1) val = val.Remove(val.Length - 1, 1);
                }
                Val = val; key = (byte)Pos;
            }
            public override string ToString() { return inv ? "!" : ""; }
            public bool Complies(IMyTerminalBlock val)
            {
                if (GR != null && val.CubeGrid != GR) return false;
                if (!Complies(val.CustomName)) return false;
                return true;
            }
            public bool Complies(string str)
            {
                if (string.IsNullOrEmpty(Val)) return !inv;
                switch (key)
                {
                    case 0: return str == Val != inv;
                    case 1: return str.EndsWith(Val) != inv;
                    case 2: return str.StartsWith(Val) != inv;
                    case 3: return str.Contains(Val) != inv;
                }
                return false;
            }

            public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>(); bool fs = false;
                TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))));
                return res.Count == 0 ? null : res[0];
            }
            public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
            {
                TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x) && (Fp == null || Fp(x)));
            }
            public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
            { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))); }
            public List<Type> FindBlocks<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
            { var res = new List<Type>(); FindBlocks(TB, res, Fp); return res; }
        }
        class Timer
        {
            readonly MyGridProgram GP;
            int Int;
            public bool zeroing;
            public int TC { get; protected set; }
            public Timer(MyGridProgram Owner, int Inter = 0, int tc = 0, bool zer = false)
            {
                GP = Owner;
                if (Inter == 0) zeroing = zer;
                else
                {
                    var P = new Point(Inter, 0);
                    CallFrequency(ref P);
                    SetInterval(P, zer);
                }
                TC = tc;
            }
            public int Interval
            {
                get { return Int; }
                set { SetInterval(RoundInt(value), zeroing); }
            }
            public void Stop() { GP.Runtime.UpdateFrequency = UpdateFrequency.None; Int = 0; }
            public static Point RoundInt(int value)
            {
                if (value == 0) return new Point(0, 0);
                Point v = new Point(value, 0);
                var del = CallFrequency(ref v);
                v.X = value % del;
                if (v.X > del / 2) value += del;
                v.X = value - v.X;
                return v;
            }
            public static int CallFrequency(ref Point res)
            {
                int del;
                if (res.X <= 960) { del = 16; res.Y = 1; }
                else if (res.X < 4000) { del = 160; res.Y = 2; }
                else { del = 1600; res.Y = 4; }
                return del;
            }
            public void SetInterval(int value, UpdateFrequency updateFreq, bool zeroing)
            { this.zeroing = zeroing; Int = value; GP.Runtime.UpdateFrequency = updateFreq; TC = 0; }
            public void SetInterval(Point val, bool zeroing)
            { this.zeroing = zeroing; Int = val.X; GP.Runtime.UpdateFrequency = (UpdateFrequency)val.Y; TC = 0; }
            public void SetInterval(int value, bool zeroing) { this.zeroing = zeroing; Interval = value; }
            public int Run()
            {
                if (Int == 0) return 1;
                TC += (int)GP.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (TC < Int) return 0;
                int res = TC;
                TC = zeroing ? 0 : TC % Int;
                return res;
            }
            public override string ToString() { return Int == 0 ? "отключено" : (Int + "мс:" + GP.Runtime.UpdateFrequency); }
            public double GetInterval(int okr = 1000)
            {
                if (Int == 0) return 0;
                if (!zeroing) return (double)Int / okr;
                int i;
                switch (GP.Runtime.UpdateFrequency)
                {
                    case UpdateFrequency.Update1: i = 16; break;
                    case UpdateFrequency.Update10: i = 160; break;
                    case UpdateFrequency.Update100: i = 1600; break;
                    default: return -1;
                }
                var b = Int % i;
                b = b == 0 ? Int : (Int / i + 1) * i;
                return ((double)b / okr);
            }
            public string ToSave() => $"{TC}@{zeroing}@{Int}";
            public static Timer Parse(string sv, MyGridProgram gp)
            {
                var s = sv.Split('@');
                return new Timer(gp, int.Parse(s[2]), int.Parse(s[0]), bool.Parse(s[1]));
            }
        }


    }
}
