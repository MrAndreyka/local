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
	partial class Program : MyGridProgram
	{
		readonly static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
		class Translate : Dictionary<String, string>
		{

			public delegate bool myPredicate(string myInt);
			public Translate()
			{
				var a = ("Components,*компоненты*,PowerCell,Энергоячейка,Canvas,Холст,SmallTube,Малая труба,Girder,Балка,SteelPlate,Стальная плаcтина,LargeTube,Большая труба," +
			   "MetalGrid,Металлическая решетка,SolarCell,Солнечная батарея,BulletproofGlass,Бронированное стекло,Motor,Мотор,Computer,Компьютер,Display,Экран," +
			   "Reactor,Реактор,Construction,Строительный компонент,Detector,Компоненты детектора,GravityGenerator,Компоненты гравитационного генератора," +
			   "InteriorPlate,Пластина,Medical,Медицинские компоненты,SmallSteelTube,Маленькая стальная трубка,Thrust,Детали ускорителя,Superconductor,Сверхпроводник," +
			   "RadioCommunication,Комплектующие для радио-связи,Ingot,*слитки*,CobaltIngot,Кобальтовый слиток,GoldIngot,Золотой слиток,StoneIngot,Гравий," +
			   "IronIngot,Железный слиток,MagnesiumIngot,Магниевый слиток,NickelIngot,Никелевый слиток,PlatinumIngot,Платиновый слиток,SiliconIngot,Кремниевая пластина," +
			   "SilverIngot,Серебряный слиток,UraniumIngot,Урановый слиток,Ore,*руда*,CobaltOre,Кобальтовая руда,PlatinumOre,Платиновая руда,NickelOre,Никелевая руда," +
			   "GoldOre,Золотая руда,StoneOre,Камень,IceOre,Лёд,IronOre,Железная руда,ScrapOre,Металлолом,MagnesiumOre,Магниевая руда,Potassium,Калий," +
			   "SiliconOre,Кремниевая руда,SilverOre,Серебряная руда,UraniumOre,Урановая руда,Ammo,*боеприпасы*,NATO_25x184mm,Контейнер боеприпасов 25x184 мм НАТО," +
			   "NATO_5p56x45mm,Магазин 5.56x45мм НАТО,Missile200mm,Контейнер 200мм ракет,Explosives,Взрывчатка,SemiAutoPistolMagazine,S-10," +
			   "Medkit,Аптечка,Powerkit,Внешний аккумулятор,SpaceCredit,Космокредит,HandTool,*инструменты*," +
			   "OxygenBottle,Кислородный баллон,HydrogenBottle,Водородный баллон,Welder,Сварщик,Welder2,Сварщик улучшенный," +
			   "Welder3,Сварщик продвинутый,Welder4,Сварщик элитный,AngleGrinder,Болгарка,AngleGrinder2,Болгарка улучшенная,AngleGrinder3,Болгарка продвинутая," +
			   "AngleGrinder4,Болгарка элитная,HandDrill,Ручной бур,HandDrill2,Ручной бур улучшенный,HandDrill3,Ручной бур продвинутый,HandDrill4,Ручной бур элитный," +
			   "AutomaticRifle,Автоматическая винтовка,PreciseAutomaticRifle,Точная винтовка,RapidFireAutomaticRifle,Скорострельная винтовка," +
			   "UltimateAutomaticRifle,Автоматическая винтовка элитная,SemiAutoPistol,Пистолет S-10").Split(',');
				var sz = a.Length - 1;
				for (var i = 0; i < sz; i += 2) Add(a[i], a[i + 1]);
			}

			public KeyValuePair<string, string> Find(myPredicate Pred)
			{
				foreach (var recordOfDictionary in this) if (Pred(recordOfDictionary.Value) || Pred(recordOfDictionary.Key)) return recordOfDictionary;
				return new KeyValuePair<string, string>();
			}

			public string GetParentByKey(myPredicate Pred)
			{
				var res = string.Empty;
				foreach (var recordOfDictionary in this)
				{
					if (recordOfDictionary.Value.StartsWith("*")) res = recordOfDictionary.Key;
					if (Pred(recordOfDictionary.Key)) return res;
				}
				return string.Empty;
			}

			public string GetName(string clas)
			{ string res; if (!TryGetValue(clas, out res)) res = clas; return res; }
		}

		void SetAtributes(string arg)
		{
			try
			{
				var parms = arg.ComLine();
				if (parms == null || (arg = parms.Argument(0)) == null) return;
				switch (arg.ToLower())
				{
					case "panels"://Добавление панелей с отбором в CD панели 
						if(parms.Switch("ini"))
						{
							var tr = new MyTree();
							tr.Parse((new Selection(parms.Argument(1)).FindBlock<IMyTerminalBlock>(GridTerminalSystem) ?? Me).CustomData, ' ');
							tr = tr.GetSection("PANELS");
							if (tr == null) { Echo("Не найдена область PANELS"); return; }
							Out.Clear();

							var fp = new Selection(null);
							Out.Load(tr, Sel_Group.Parse, (l, name, ind) =>
							{
								var lst = fp.Change(name).FindBlocks<IMyTextSurfaceProvider>(GridTerminalSystem);
								lst.ForEach(b => l.Add(new Txt_Surface(b, ind)));
							}, s => s.StartsWith("hor", StringComparison.OrdinalIgnoreCase) ||
							s.StartsWith("-") || s.Equals("true", StringComparison.OrdinalIgnoreCase));
							SetAtributes("? panels");
						} else
						{

							if (parms.Argument(1) == null) { Echo("Ожидается маска для поиска панелей"); return; }
							string param; int i = 1;

							while ((param = parms.Argument(i++)) != null)
							{
								var L = new List<IMyTerminalBlock>();
								new Selection(param).FindBlocks(GridTerminalSystem, L, x => x is IMyTextSurfaceProvider);
								if (L.Count == 0) { Echo("Не найдены панели по запросу"); return; }

								var fp = L.Find(x => !string.IsNullOrWhiteSpace(x.CustomData));
								if(fp==null) { Echo("Не найдены параметры в " + param + ".CustomData"); return; }

								var mask = ParseMask(fp.CustomData);
								if(mask.Count==0) { Echo("Не верная маска в CustomData"); return; }
								var tmp = new Selected_Panel<Sel_Group, Item_sel>(mask);
								
								L.ForEach(x => tmp.Add(new Txt_Surface(x as IMyTextSurfaceProvider)));
								var tecSel = Out.Find(x => x.Equals(tmp.Select));//Поиск с такими же настройками 
								if (tecSel != null) Out.Remove(tecSel);

								Echo("Установлены панели: " + tmp.ToString());
								Out.Add(tmp);
							}
						}
						break;
					case "panel-":
						if (parms.Switch("index"))
						{
							var i = parms.Argument(1).AsInt(null);
							if (!i.HasValue){ Echo(parms.Argument(1) + " не является числом!"); return; }
							if(!Between(i.Value, 0, Out.Count)) { Echo("Число не в диапазоне!"); return; }
							Out.RemoveAt(i.Value);
						}
						else
						{
							var sl = new Selection(parms.Argument(1));
							ITxt_null tp;
							var sb = new StringBuilder("Удалены:\n");
							while ((tp = Out.FindPanel(x => sl.Complies(x.OwnerBloc.CustomName))) != null)
							{
								sb.AppendLine((tp as Txt_Surface).OwnerBloc.ToString());
								Txt_Panel ow = tp.Owner;
								while (ow.Owner != null && ow.Count == 1)
								{ tp = ow; ow = ow.Owner; }

								if (ow.Count == 1)
									Out.Remove(ow as Selected_Panel<Sel_Group, Item_sel>);
								else ow.Remove(tp);
							}
							Echo(tp != null ? sb.ToString() : $"Панели по маске \"{sl.Value}\" не найдены");
						}
                        break;
                    case "panel+":
                        {
                            if (!Between(parms.ArgumentCount, 3, 4)) {
								Echo("Ожидается 2 параметра: <Индекс шаблона или Название панели> " +
								  "<Маска поиска добавляемых блоков>"); return; }

							var L = new List<IMyTextSurfaceProvider>();
							new Selection(parms.Argument(2)).FindBlocks(GridTerminalSystem, L);
							if (L.Count == 0) { Echo($"Панели по шаблону \"{parms.Argument(2)}\" не найдены"); return; }
							L.Sort(delegate (IMyTextSurfaceProvider x, IMyTextSurfaceProvider y) 
							{ return (x as IMyTerminalBlock).CustomName.CompareTo((y as IMyTerminalBlock).CustomName); });

							var ind = parms.Argument(1).AsInt(null);
							Txt_Panel tp = null;

							if (ind.HasValue)
							{
								if (!Between(ind.Value, 0, Out.Count - 1)) { Echo($"Выход за пределы множества"); return; }
								tp = Out[ind.Value];
							}
							else {
								var ts = Out.FindPanel(x => x.OwnerBloc.CustomName == parms.Argument(1));
								if(ts==null){ Echo($"Панель \"{parms.Argument(1)}\" еще не установлена"); return; }
								tp = ts.Owner;
							}
							L.ForEach(x => tp.Add(new Txt_Surface(x)));
						}
						break;
					case "reload":
					case "rl":
						{
							var L = new List<IMyTerminalBlock>();
							if (parms.ArgumentCount < 2) GridTerminalSystem.GetBlocks(L);
							else new Selection(parms.Argument(1), parms.Switch("^") ? null : Me.CubeGrid).FindBlocks(GridTerminalSystem, L);
							inv.Clear();
							asm.Clear();
							L.ForEach(x => AddBloc(x));
							inv.Sort(delegate (InvData x, InvData y) { return y.key - x.key; });
							asm.Sort((x, y) => x.CustomName.CompareTo(y.CustomName));

							var u = asm.FindIndex(x => !x.CooperativeMode);
							if (u > 0) asm.Move(u, 0);
							Echo($"Инвентарей: {inv.Count} Cборщиков: {asm.Count}");
							SetAtributes("? bloc,ass");
						}
						break;
					case "asm":
						{
							int pos;
							if (!int.TryParse(parms.Argument(1), out pos)) { Echo("Требует числовой параметр"); return; }
							if (pos >= asm.Count) { Echo($"Слишком большое значние, всего {asm.Count} сборщиков"); return; }
							if (pos == 0) { return; }
							asm.Move(pos, 0);
							Echo(asm[0].CustomName + " установлен основным");
						}
						break;
					case ">":
						{
							if (parms.Switch("-"))
							{
								var tc = stor.Count;
								var ss = new Selection(parms.Argument(1));
								stor.RemoveAll(x => ss.Complies(x.Inv.ToString()));
								tc -= stor.Count;
								Echo(tc == 0 ? "Не найдены блоки по запросу" : $"Удалено {tc} блоков");
							}
							else AddStorage(parms.Argument(1));

							// Исправляем если нужно типы инвентарей 
							inv.ForEach(delegate (InvData x)
							{
								if (x.key == 1) return;
								if (stor.Find(y => y.Inv.Equals(x.Inventory)) == null)
								{
									if (x.key != 5) return;
									var tm = x.Inventory.Owner as IMyTerminalBlock;
									x.key = (byte)(tm != null && InvData.IsSpecBloc(tm) ? 2 : 0);
								}
								else if (x.key != 5) x.key = 5;
							});
							SetAtributes("? storage");

						}
						break;
					case "?":
						Echo(GetInfo(parms.Argument(1)).ToString());
						break;
					case "limit+":
						{
							if (parms.Items.Count < 3) { Echo("Ожидается 2 параметра: что сколько добавить <-?>"); return; }
							int ind, cou;
							var ts = parms.Argument(1);

							var name = LangDic.Find(x => x.StartsWith(ts)).Key;

							if (name != null) ind = Limits.FindIndex(x => x.Lnk.Name == name);
							else if (!int.TryParse(ts, out ind))
							{ Echo("Первым параметром ожидается имя элемента или номер в списке лимитов.\nНе удалось опознать: " + ts); return; }

							if (ind >= Limits.Count || ind < 0)
							{
								Echo($"Не верно указан элемент {ts}" +
									"\nИзменить количество возможно только в установленных ранее лимитах, для добавления новых " +
									"воспользуйтесь сборщиком. Имя которого передайте в команду \"limit\""); return;
							}

							name = parms.ArgumentCount > 3 ? parms.Argument(2) :
							parms.Switches.Any(x => x != "?") ? "-" + parms.Switches.First(x => x != "?") : null;

							if (!int.TryParse(name, out cou)) { Echo("Не верное значения количества"); return; }
							var t = Limits[ind];
							t.count += cou;
							if (parms.Switch("?"))
							{ Echo($"Для {t.ShowName} будет установлен новый лимит: {t.count}"); return; }

							Limits[ind] = t;
							Echo($"Для {t.ShowName} установлен новый лимит: {t.count}");
						}
						break;
					case "limit":
						{
							if (parms.ArgumentCount == 1) Limits.Clear();
							else SetLimit(parms.Argument(1));
						}
						break;
					case "init":
                        {
                            Out.Clear(); stor.Clear(); Limits.Clear();
                            foreach (var str in Me.CustomData.Split('\n')) SetAtributes(str);
                        }
                        break;
					case "save"://Записывает сосояние 
						{
							var bl = GridTerminalSystem.GetBlockWithName(parms.Argument(1));
							bl.CustomData = ToSave();
							Echo($"Сохранено в {bl.CustomName}.CustomData");
						}
						break;
					case "load"://Считывает сосояние
						{
							var bl = GridTerminalSystem.GetBlockWithName(parms.Argument(1));
							if (bl == null || string.IsNullOrEmpty(bl.CustomData))
							{ Echo("Не найден блок с параметрами в CustomData: " + parms.Argument(1)); return; }
							Restore(bl.CustomData);
						}
						break;
					case "unload":
					case "ul":
						{
							List<IMyTerminalBlock> l = new List<IMyTerminalBlock>();
							new Selection(parms.Argument(1)).FindBlocks(GridTerminalSystem, l, x => x.HasInventory && (!InvData.IsSpecBloc(x) || !x.IsWorking));
							if (l.Count == 0) { Echo($"Не найдены блоки по запросу: {parms.Argument(1)}"); return; }

							l.ForEach(x => Moved(new InvDT(x, (byte)(x.InventoryCount - 1))));							
						}
						break;
					case "replay":
						{
							int i;
							if (!int.TryParse(parms.Argument(1), out i)) { Echo("Неверно указан интервал"); return; }
							TM.SetInterval(i * 1000, false);
							Echo(TM.GetInterval().ToString());
						}
						break;
					default: Echo("Команда не опознана: " + arg); break;
				}
			}
			catch (Exception e)
			{
				Me.CustomData = "/n" + e.ToString();
				Echo("Exception: info in CustomData");
			}
		}

		Program()
		{
			MyIni _ini = new MyIni();

			if (Storage.StartsWith("AutoBuild_3") && Me.TerminalRunArgument != "null")
				try
				{
					Restore(Storage.Substring(Storage.IndexOf("\n", 13)+1));
					TM = Timer.Parse(Storage.Substring(12, Storage.IndexOf("\n", 13) - 12), this);
				}
				catch (Exception e)
				{
					Echo(e.ToString() + "/nВ 'CustomData' 'Storage' для анализа");
					Me.CustomData += "/n" + Storage;
				}
			else if (string.IsNullOrEmpty(Me.CustomData))
				Me.CustomData = @"panels,panel,panel+,panel-,?,>,limit,limit+,replay,unload,init";
			if (TM == null) TM = new Timer(this);
		}

		void Save()
		{
			Storage = ToSave();
			if (!string.IsNullOrWhiteSpace(Storage))
				Storage = $"AutoBuild_2\n{TM.ToSave()}\n{Storage}";
			Me.CustomData = Storage;
		}

		string GetInfo(string arg) 
		{
			bool First = true;
			var buf = new StringBuilder();
			var all = string.IsNullOrWhiteSpace(arg);
			if (all || arg.Contains("storage"))
			{
				if (!First) buf.AppendLine().AppendLine("Склады:"); First = false;
				buf.AppendLine(string.Join("\r\n", stor));
			}
			if (all || arg.Contains("limit"))
			{
				if (!First) buf.AppendLine().AppendLine("Лимиты:"); First = false;
				buf.AppendLine(string.Join("\r\n", Limits));
			}
			if (all || arg.Contains("ass"))
			{
				if (!First) buf.AppendLine().AppendLine("Сборщики:"); First = false;
				asm.ForEach(x => buf.AppendLine(x.CustomName + (x.CooperativeMode ? "" : " >> основной")));
			}
			if (all || arg.Contains("panel"))
			{
				if (!First) buf.AppendLine().AppendLine("Панели:"); First = false;
				buf.AppendLine(Out.ToString());
			}
			if (all || arg.Contains("bloc"))
			{
				if (!First) buf.AppendLine().AppendLine("Инвентари:"); First = false;
				inv.ForEach(x =>
				{
					if (x.key == 1) return;
					else if (x.key == 2) buf.AppendLine("#" + x);
					else buf.AppendLine(x.ToString());
				});
			}
			if (all) buf.AppendLine($"\nАвтовыполнение: {TM}");
			return buf.ToString();
		}

		readonly Timer TM;
		static readonly Translate LangDic = new Translate();
		private static readonly TextOut<Sel_Group, Item_sel> Out = new TextOut<Sel_Group, Item_sel>();
		static readonly Sklads stor = new Sklads();
		static readonly List<MyBuild_Item> Limits = new List<MyBuild_Item>();
		static readonly List<InvData> inv = new List<InvData>();
		static readonly List<IMyAssembler> asm = new List<IMyAssembler>();

		void Main(string arg, UpdateType UT)
		{
			if (UT < UpdateType.Update1 && !string.IsNullOrWhiteSpace(arg)) { SetAtributes(arg); return; }
			if (TM.Run() == 0) return;
			if (inv.Count == 0) SetAtributes("reload");
			try
			{
				Out.Restore();
				double[] a = new double[Limits.Count];//Для подсчета очереди строительства 
				{
					List<MyProductionItem> lst = new List<MyProductionItem>();
					asm.ForEach(delegate (IMyAssembler A)
					{
						A.GetQueue(lst);
						lst.ForEach(delegate (MyProductionItem x)
						{
							var u = Limits.FindIndex(y => y.IDDef == x.BlueprintId);
							if (u >= 0) a[u] += (double)x.Amount;
						});
					});// подсчет очереди строительства 
				}

				List<MyInvItem> ListCount = new List<MyInvItem>();
				Limits.ForEach(x => ListCount.Add(x.Clone(0)));
				CalAndMove(ListCount);

				ListCount.Sort((x, y) => x.Lnk.Type == y.Lnk.Type ? x.Lnk.ShowName.CompareTo(y.Lnk.ShowName) : x.Lnk.Type - y.Lnk.Type);

				//заказ недостающих компонент и вывод количества 
				var asm_ = asm.Count == 0 ? null : asm[0];
				foreach(var t in ListCount)
                {
                    var xi = Limits.FindIndex(y => y.Lnk.Equals(t.Lnk));
					Echo(t.Lnk.ToString());
                    if (xi < 0 || asm_ == null || asm_.Mode == MyAssemblerMode.Disassembly)
					{ Out.AddLine(t.Lnk, t.ToString()); continue; }
					var s = new StringBuilder(t.ShowName);
					s.Append(": ").Append(t.count.ToString("#,##0.##", SYS));
                    if (a[xi] > 0) s.Append(a[xi].ToString("+#,##0.##", SYS));
                    var x = Limits[xi];
					s.Append(" =>").Append(x.count.ToString("#,##0.##", SYS));
                    xi = (int)(x.count - a[xi] - t.count);
					if (xi > 0)
					{
						asm_.AddQueueItem(x.IDDef, (decimal)xi);
						s.Append(" (").Append(xi.ToString("+(#,##0)", SYS)).Append(")");
					}
	                Out.AddLine(t.Lnk, s.ToString());
                }

                if (Out.DefList.Length > 0)
                {
                    Me.GetSurface(1).ContentType = ContentType.TEXT_AND_IMAGE;
					Me.GetSurface(1).WriteText(Out.DefList);
				}
			}
			catch (Exception e)
			{
				Me.CustomData = "/n" + e.ToString();
				Echo("Exception: info in CustomData");
				/*Echo(debug.ToString());debug.Clear();*/
			}
		}

		void CalAndMove(List<MyInvItem> ListCount, bool moved = true)
		{
			List<MyInventoryItem> list2 = new List<MyInventoryItem>();
			var Dil = moved ? new Dictionary<InvDT, List<MyInvItem>>() : null;
			inv.ForEach(x=>
			{
				if (moved && stor.Count > 0 && (x.key == 5 || x.key == 0)) Moved(x);//Делаем перемещения если нужно 
				if (x.key == 5) return;
				x.Inventory.GetItems(list2);//Добавляем содержимое после перемещения 
			});
			stor.ForEach(x => x.Inv.Inventory.GetItems(list2));//Добавляем содержимое хранилищ 

			list2.ForEach(delegate (MyInventoryItem y)//Считаем содержимое 
			{
				var np = MyInvIt.Get(y.Type);
				var ind = ListCount.Find(z => z.Lnk.Equals(np));
				if (ind == null) ListCount.Add(new MyInvItem(np, (double)y.Amount));
				else ind.count += (double)y.Amount;
			});
		}

		void Moved(InvDT Inv)
		{
			if (stor.FindIndex(x => x.Inv.Equals(Inv) && x.Count == 0) >= 0) return;
			MyInventoryItem p;
			List<MyInventoryItem> inv = new List<MyInventoryItem>();

			Inv.Inventory.GetItems(inv);
			if (inv.Count == 0) return;

			bool res = false;
			for (int j = inv.Count - 1; j >= 0; j--)
			{
				if ((p = inv[j]).Amount == 0) continue;
				MyInvItem Pi = new MyInvItem(p);
				int i = 0;

				while (stor.GetInv_Move(Pi.Lnk, Inv.Inventory, ref i) >= 0 && p != null)
				{
					res = stor[i].Inv.Inventory.TransferItemFrom(Inv.Inventory, j);
					var f = Inv.Inventory.GetItemByID(inv[j].ItemId);

					double cou = Pi.count - (f.HasValue ? (double)f.Value.Amount : 0);
					Pi.count -= cou;
					i++;
				}
			}
		}

		bool AddBloc(IMyTerminalBlock X)
		{
			if (X is IMyAssembler) asm.Add(X as IMyAssembler);
			if (!X.HasInventory || inv.FindIndex(x => x.Inventory.Owner == X) >= 0) return false;
			byte key = 0;
			if (stor.Find(x => x.Inv.Inventory.Owner.Equals(X)) != null) key = 5;
			else if (InvData.IsSpecBloc(X)) key = 2;

			for (byte k = 0; k < X.InventoryCount; k++)
				inv.Add(new InvData(X, k, X.InventoryCount > 1 && k == 0 ? (byte)1 : key));
			return true;
		}
	
		Sel_Group ParseMask(string masks)
		{
			var msks = masks.Split(',');
			var FL = new Sel_Group();

			for (var i = 0; i < msks.Length; i++)
			{
				var p2 = msks[i];
				var inv = p2.StartsWith("!");
				if (inv) p2 = p2.Remove(0, 1);
				if (p2.Length == 0) continue;
				var tr = LangDic.Find(x => x.StartsWith(p2, true, null));
				if (string.IsNullOrEmpty(tr.Key)) { Echo($"Не найдено значение отбора: {p2}"); continue; }
				var s1 = new Sel_Item(tr.Value.StartsWith("*") ? MyInvIt.GetType(tr.Key) : (byte)0, tr.Key, inv);
				FL.Add(s1);
			}
			return FL;
		}
		
		void SetLimit(string name)
		{
			var asm_ = GridTerminalSystem.GetBlockWithName(name) as IMyAssembler;
			if (asm_ == null) { Echo($"Assembler \"{name}\" не найден"); return; }
			List<MyProductionItem> list = new List<MyProductionItem>();
			asm_.GetQueue(list);
			list.ForEach(delegate (MyProductionItem x)
			{
				var mit = (MyItemType)x.BlueprintId;
				var tl = Limits.Find(y => y.IDDef.Equals(x.BlueprintId));
				if (tl == null) Limits.Add(tl = new MyBuild_Item(x));
				else tl.count += (double)x.Amount;
			});
		}
	
		public void AddStorage(string masks)
		{
			var maski = masks.Split(',');
			for (var e = 0; e < maski.Length; e++)
			{
				var msk = maski[e];
				if (msk.Length == 0) { Echo("Пропуск пустой маски"); continue; }
				string param = msk.Part(':');
				var L = new List<IMyTerminalBlock>();
				new Selection(msk).FindBlocks(GridTerminalSystem, L, x => x.HasInventory);
				if (L.Count == 0) { Echo($"Склады по запросу \"{msk}\" не найдены"); continue; }

				bool aded = param.StartsWith("+");
				if (aded) param = param.Remove(0, 1);
				var a = string.IsNullOrEmpty(param) ? null : ParseMask(param);

				int p = 0;
				for (int j = 0; j < L.Count; j++)
				{
					var Inv = L[j].GetInventory();
					if (string.IsNullOrEmpty(param))
					{
						var strorBl = L[j].CustomData;
						aded = strorBl.StartsWith("+");
						if (aded) strorBl = strorBl.Substring(1);
						a = ParseMask(strorBl);
					}

					p = stor.FindIndex(x => x.Inv.Inventory.Equals(Inv));
					MyRef b = p < 0 ? b = new MyRef(L[j]) : stor[p];
					if (aded) b.AddNonExisting(a);
					else { b.Clear(); b.AddList(a); }
					if (p < 0) stor.Add(b); else stor[p] = b;
				}
			}
		}

		void Restore(string value)
		{
			inv.Clear();
			Out.Clear();
			asm.Clear();
			stor.Clear();
			Limits.Clear();
			Echo("Восстановление настроек...");

			var Tr = new MyTree();
			Tr.Parse(value);

            var tmp = Tr.FirstOrDefault(x => x.Name.Equals("PANELS", StringComparison.OrdinalIgnoreCase));
			if (tmp != null)
			{	
				Out.Load(tmp, Sel_Group.Parse, (l, name, ind) => {
					var bl = GridTerminalSystem.GetBlockWithId(name.AsLong()) as IMyTextSurfaceProvider;
					l.Add(new Txt_Surface(bl, ind));
				});
			}

			tmp = Tr.FirstOrDefault(x => x.Name.Equals("STORAGES", StringComparison.OrdinalIgnoreCase));
			if (tmp != null) stor.Load(tmp, n=> GridTerminalSystem.GetBlockWithId(n.AsLong()) as IMyTerminalBlock);

			tmp = Tr.FirstOrDefault(x => x.Name.Equals("LIMITS", StringComparison.OrdinalIgnoreCase));
			if (tmp != null) tmp.ForEach(x => Limits.Add(MyBuild_Item.Parse(x)));

			tmp = Tr.FirstOrDefault(x => x.Name.Equals("BOXES", StringComparison.OrdinalIgnoreCase));
			if (tmp != null)
                tmp.ForEach(x => {
					inv.Add(new InvData(GridTerminalSystem.GetBlockWithId(
						x.Param.Part('/').AsLong()) as IMyTerminalBlock,
						x.Param.Part('/', true, false).AsByte(),
						x.Param.Substring(0, 1).AsByte()));
				});

			Echo("Восстановление завершено удачно!");
		}

		string ToSave()
		{
			var tr = new MyTree();

			if (Out.Count > 0) tr.Add("PANELS", Out.Save());
			if (stor.Count > 0) tr.Add("STORAGES", stor.Save());
			if (Limits.Count > 0)
			{
				var tmp = new MyTree();
				Limits.ForEach(x => tmp.Add(x.Save()));
				tr.Add("LIMITS", tmp);
			}
			if (inv.Count > 0) 
			{
				var tmp = new MyTree();
				inv.ForEach(x => tmp.Add(new MyTree(x.ToSave())));
				tr.Add("BOXES", tmp);
			}
			return tr.ToString();
		}

	}
}
