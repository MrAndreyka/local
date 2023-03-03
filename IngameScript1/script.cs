/*
 * --------------------------------------------------------------------------      
 *      AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com 
 *      When using and disseminating information about the authorship is obligatory 
 *      При использовании и распространении информация об авторстве обязательна 
 *  --------------------------------------------------------------------------
 */

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
			case "panels"://Добавление панелей с параметрами в CD панели
				{
					if (parms.Argument(1) == null) { Echo("Ожидается маска для поиска панелей"); return; }
					string param = null; int i = 1;
					while ((param = parms.Argument(i++)) != null)
					{
						var L = new List<IMyTerminalBlock>();
						new Selection(param).FindBlocks(GridTerminalSystem, L, x => x is IMyTextSurfaceProvider && !string.IsNullOrWhiteSpace(x.CustomData));

						L.ForEach(L1 =>
						{
							param = L1.CustomData;
							//arg = EndCut(ref param, "\n");
							if (string.IsNullOrWhiteSpace(arg)) SetPanel(param.ComLine(), L1 as IMyTextSurfaceProvider);
							else if (!SetPanel($"{param}:{arg.Replace("\n", "")}".ComLine())) Echo(L1.CustomName);
						});
					}
				}
				break;
			case "panel":
				SetPanel(parms);
				break;
			case "panel-":
				{
					var sl = new Selection(parms.Argument(1));
					FindFlat fp; TextPanel ap; Abs_Flat tmp; TextFlat tp;
					var fl = false; var sb = new StringBuilder("Удалены:\n");
					while (Out.FindPanel(x => sl.Complies(x.CustomName), out fp, out ap))
					{
						fl = true;
						tmp = ap; tp = ap.Owner as TextFlat;
						while (true)
						{
							if (tp == null) { sb.AppendLine(tmp.Owner.ToString()); Out.Remove(tmp.Owner as FindFlat); break; }
							if (tp.Flats.Count > 1) { sb.AppendLine(tmp.ToString()); tp.Flats.Remove(tmp); break; }
							tp = (tmp = tp).Owner as TextFlat;
						}
					}
					Echo(fl ? sb.ToString() : $"Панели по маске \"{sl.Value}\" не найдены");
				}
				break;
			case "panel+":
				{
					if (!Between(parms.ArgumentCount, 3, 4))
					{ Echo("Ожидается 2-3 параметра: <Индекс шаблона или Название панели>:" +
						"<Маска поиска добавляемых блоков>:Горизонтальная группа"); return; }

					var L = new List<IMyTextSurfaceProvider>();
					new Selection(parms.Argument(2)).FindBlocks(GridTerminalSystem, L);
					if (L.Count == 0) { Echo($"Панели по шаблону \"{parms.Argument(2)}\" не найдены"); return; }
					L.Sort(delegate (IMyTextSurfaceProvider x, IMyTextSurfaceProvider y) { return (x as IMyTerminalBlock).CustomName.CompareTo((y as IMyTerminalBlock).CustomName); });

					FindFlat tc;
					TextPanel tp;
					int ind = -1;
					int.TryParse(parms.Argument(1), out ind);

					if (ind >= 0) { tc = Out[ind]; tp = tc.Flat.Find(x => true); }
					else if (!Out.FindPanel(x => x.CustomName == parms.Argument(1), out tc, out tp)) { Echo($"Панель \"{parms.Argument(1)}\" еще не установлена"); return; }

					TextFlat TecPan;
					if (tp.Owner == tc)
					{
						Out.Remove(tc);
						tc = new FindFlat(tc, TecPan = new TextFlat(parms.Argument(3) != "0", tp));
						Out.Add(tc);
					}
					else TecPan = tp.Owner as TextFlat;
					for (var i = 0; i < L.Count; i++) { TecPan.Add(new TextPanel(L[i])); Echo("+ " + ((IMyTerminalBlock)L[i]).CustomName); }
				}
				break;
			case "reload":
			case "rl":
				{
					var L = new List<IMyTerminalBlock>();
					if (parms.ArgumentCount < 2) GridTerminalSystem.GetBlocks(L);
					else new Selection(parms.Argument(1), parms.Switch("^")? null: Me.CubeGrid).FindBlocks(GridTerminalSystem, L);
					inv.Clear();
					asm.Clear();
					L.ForEach(x => AddBloc(x));
					inv.Sort(delegate (InvData x, InvData y) { return y.key - x.key; });
					asm.Sort((x, y) => x.CustomName.CompareTo(y.CustomName));

					var u = asm.FindIndex(x => !x.CooperativeMode);
					if (u > 0) asm.Move(u, 0);
					Echo($"Инвентарей: {inv.Count} Cборщиков: {asm.Count}");
					SetAtributes("?:bloc,ass");
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
						if (stor.Find(y => y.Inv.Equals(x.Inv)) == null)
						{
							if (x.key != 5) return;
							var tm = x.Inv.Owner as IMyTerminalBlock;
							x.key = (byte)(tm != null && InvData.IsSpecBloc(tm) ? 2 : 0);
						}
						else if (x.key != 5) x.key = 5;
					});
					SetAtributes("?:storage");

				}
				break;
			case "?":
				{
					bool First = true;
					var buf = new StringBuilder();
					var all = parms.ArgumentCount == 1;
					if (all || parms.Argument(1).Contains("storage"))
					{
						if (!First) buf.AppendLine(); buf.AppendLine("Склады:"); First = false;
						buf.AppendLine(string.Join("\r\n", stor));
					}
					if (all || parms.Argument(1).Contains("limit"))
					{
						if (!First) buf.AppendLine(); buf.AppendLine("Лимиты:"); First = false;
						buf.AppendLine(string.Join("\r\n", Limits));
					}
					if (all || parms.Argument(1).Contains("bloc"))
					{
						if (!First) buf.AppendLine(); buf.AppendLine("Инвентари:"); First = false;
						inv.ForEach(x =>
						{
							if (x.key == 1) return;
							else if (x.key == 2) buf.AppendLine("#" + x);
							else buf.AppendLine(x.ToString());
						});
					}
					if (all || parms.Argument(1).Contains("ass"))
					{
						if (!First) buf.AppendLine(); buf.AppendLine("Сборщики:"); First = false;
						asm.ForEach(x => buf.AppendLine(x.CustomName + (x.CooperativeMode ? "" : " >> основной")));
					}
					if (all || parms.Argument(1).Contains("panel"))
					{
						if (!First) buf.AppendLine(); buf.AppendLine("Панели:"); First = false;
						Out.ForEach(x => buf.AppendLine(x.ToString()));
					}
					if (all) buf.AppendLine($"\nАвтовыполнение: {TM}");
					Echo(buf.ToString());
				}
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
					var ar = Me.CustomData.Split('\n');
					foreach (var str in ar) SetAtributes(str);
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

					var Dil = new Dictionary<InvDT, List<MyInvItem>>();
					l.ForEach(x => Moved(new InvDT(x, (byte)(x.InventoryCount - 1)), ref Dil));
					ShowMoved(Dil);
					if (Out.Uncown.Length > 0) { Echo(Out.Uncown.ToString()); Out.Uncown.Clear(); }
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

	if (Storage.StartsWith("AutoBuild_2") && Me.TerminalRunArgument != "null")
		try
		{
			Restore(Storage, 2);
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
}

readonly Timer TM;
static readonly Translate LangDic = new Translate();
private static readonly TextsOut Out = new TextsOut();
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
		Out.ClearText();
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
		for (var i = 0; i < ListCount.Count; i++)
		{
			var t = ListCount[i];
			Echo(t.Lnk.ToString() + " " + t.Lnk.Type.ToString());
			var xi = Limits.FindIndex(y => y.Lnk.Equals(t.Lnk));
			if (xi < 0 || asm_ == null || asm_.Mode == MyAssemblerMode.Disassembly) { Out.AddText(t); continue; }

			var s = new List<string>
{"{1}: {2}", t.ShowName, t.count.ToString("#,##0.##", SYS)};
			if (a[xi] > 0) s[2] += a[xi].ToString("+#,##0.##", SYS);
			var x = Limits[xi];
			s.Add(x.count.ToString());
			s[0] += " =>{3}";
			xi = (int)(x.count - a[xi] - t.count);
			if (xi <= 0) { Out.AddText(t.Lnk, s.ToArray()); continue; }
			s[2] += xi.ToString("+(#,##0)", SYS);
			Out.AddText(t.Lnk, s.ToArray());
			asm_.AddQueueItem(x.IDDef, (decimal)xi);
		}

		Out.ShowText();
		if (Out.Uncown.Length > 0)
		{
			Me.GetSurface(1).ContentType = ContentType.TEXT_AND_IMAGE;
			Me.GetSurface(1).WriteText(Out.Uncown.ToString());
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
	inv.ForEach(delegate (InvData x)
	{
		if (moved && stor.Count > 0 && (x.key == 5 || x.key == 0)) Moved(x, ref Dil);//Делаем перемещения если нужно
		if (x.key == 5) return;
		x.Inv.GetItems(list2);//Добавляем содержимое после перемещения
	});
	if (moved) ShowMoved(Dil);
	stor.ForEach(x => x.Inv.Inv.GetItems(list2));//Добавляем содержимое хранилищ

	list2.ForEach(delegate (MyInventoryItem y)//Считаем содержимое
	{
		var np = MyInvIt.Get(y.Type);
		var ind = ListCount.Find(z => z.Lnk.Equals(np));
		if (ind == null) ListCount.Add(new MyInvItem(np, (double)y.Amount));
		else ind.count += (double)y.Amount;
	});
}

void Moved(InvDT Inv, ref Dictionary<InvDT, List<MyInvItem>> tp)
{
	if (stor.FindIndex(x => x.Inv.Equals(Inv) && x.Count == 0) >= 0) return;
	MyInventoryItem p;
	List<MyInventoryItem> inv = new List<MyInventoryItem>();

	Inv.Inv.GetItems(inv);
	if (inv.Count == 0) return;

	bool res = false;
	List<MyInvItem> TecL;
	for (int j = inv.Count - 1; j >= 0; j--)
	{
		if ((p = inv[j]).Amount == 0) continue;
		MyInvItem Pi = new MyInvItem(p);
		int i = 0;

		while (stor.GetInv_Move(Pi.Lnk, Inv.Inv, ref i) >= 0 && p != null)
		{
			res = stor[i].Inv.Inv.TransferItemFrom(Inv.Inv, j);
			var f = Inv.Inv.GetItemByID(inv[j].ItemId);

			double cou = Pi.count - (f.HasValue ? (double)f.Value.Amount : 0);
			Pi.count -= cou;

			if (!tp.TryGetValue(stor[i].Inv, out TecL)) tp.Add(stor[i].Inv, new List<MyInvItem>() { Pi.Clone(cou) });
			else
			{
				var tm = TecL.Find(x => x.Lnk.Name == Pi.Lnk.Name);
				if (tm == null) TecL.Add(Pi.Clone(cou));
				else tm.count += cou;
			}
			i++;
		}
	}
}

void ShowMoved(Dictionary<InvDT, List<MyInvItem>> Dil)
{
	if (Dil.Count == 0) return;
	StringBuilder s = new StringBuilder("Перемещение...>>>>\n");
	for (var i = 0; i < Dil.Count; i++) { var x = Dil.ElementAt(i); s.AppendLine($"в {x.Key}:\n{string.Join("\n", x.Value)}"); }
	s.Append("<<<<<<<<<<");
	Out.AddText(s.ToString());
}

public bool SetPanel(MyCommandLine param, IMyTextSurfaceProvider txt = null)
{
	if (txt == null && param.ArgumentCount < 3) { Echo("Ожидается 2 параметра: Что:{2>Куда1;Куда2}"); return false; }
    var FL2 = ParseMask(param.Argument(1));

    Abs_Flat tmp;
	var sl = new Selection(null);
	if (param.ArgumentCount < 3) tmp = new TextPanel(txt);
	else
	{
		int tp = Abs_Flat.TryParse(x => sl.Change(x).FindBlock<IMyTextSurfaceProvider>(GridTerminalSystem), param.Argument(2), out tmp);
		if (tp > -10) { Echo(tp == -1 ? $"Не найдена панель: \"{sl.Value}\"" : $"Не верный формат шаблона ({tp + 1}) {param.Argument(2)}"); return false; }
	}

	FL2.Find(z => Out.Find(x => x.In(z)) != null);

	var tecSel = Out.Find(x => x.Equals(FL2));//Поиск с такими же настройками
	if (tecSel != null) Out.Remove(tecSel);

	tecSel = new FindFlat(FL2, tmp);
	Echo("Установлены панели: " + tecSel.ToString());
	Out.Add(tecSel);
	return true;
}
bool AddBloc(IMyTerminalBlock X)
{
	if (X is IMyAssembler) asm.Add(X as IMyAssembler);
	if (!X.HasInventory || inv.FindIndex(x => x.Inv.Owner == X) >= 0) return false;
	byte key = 0;
	if (stor.Find(x => x.Inv.Owner == X) != null) key = 5;
	else if (InvData.IsSpecBloc(X)) key = 2;

	for (byte k = 0; k < X.InventoryCount; k++)
		inv.Add(new InvData(X, k, X.InventoryCount > 1 && k == 0 ? (byte)1 : key));
	return true;
}
FindList ParseMask(string masks)
{
	var msks = masks.Split(',');
	var FL = new FindList();

	for (var i = 0; i < msks.Length; i++)
	{
		var p2 = msks[i]; var inv = p2.StartsWith("!");
		if (inv) p2 = p2.Remove(0, 1);
		if (p2.Length == 0) continue;
		var tr = LangDic.Find(x => x.StartsWith(p2, true, null));
		if (string.IsNullOrEmpty(tr.Key)) { Echo($"Не найдено значение отбора: {p2}"); continue; }
		FL.Add(new FindItem(tr.Value.StartsWith("*") ? MyInvIt.GetType(tr.Key) : (byte)0, tr.Key, inv));
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
		var tl = Limits.Find(y => y.IDDef.Equals(x.BlueprintId));
		if (tl == null) Limits.Add(tl = new MyBuild_Item(x));
		else tl.count += (double)x.Amount;
		Echo("->" + tl.ToString());
	});
}
public void AddStorage(string masks)
{
	var maski = masks.Split(',');
	for (var e = 0; e < maski.Length; e++)
	{
		var msk = maski[e];
		if (msk.Length == 0) { Echo("Пропуск пустой маски"); continue; }
		string param = msk.Begin(':');
		var L = new List<IMyTerminalBlock>();
		new Selection(msk).FindBlocks(GridTerminalSystem, L, x => x.HasInventory);
		if (L.Count == 0) { Echo($"Склады по запросу \"{msk}\" не найдены"); continue; }

		bool aded = param.StartsWith("+");
		if (aded) param = param.Substring(1);
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

			p = stor.FindIndex(x => x.Inv.Inv.Equals(Inv));
			MyRef b = p < 0 ? b = new MyRef(L[j]) : stor[p];
			if (aded) b.AddNonExisting(a);
			else { b.Clear(); b.AddList(a); }
			if (p < 0) stor.Add(b); else stor[p] = b;
		}
	}
}

/*static string EndCut(ref string val, string tc, int cou = 1)
		{
			int pos = -1;
			while (--cou >= 0 && ++pos >= 0) if ((pos = val.IndexOf(tc, pos)) < 0) break;
			if (pos < 0) return string.Empty;
			var Result = val.Substring(pos + 1);
			val = val.Remove(pos);
			return Result;
		}*/
string ToSave()
{
	var bs = new StringBuilder();
	Out.ForEach(x => bs.AppendLine(x.ToSave()));

	bs.AppendLine();
	var ls = new List<string>();
	stor.ForEach(x => bs.AppendLine(x.ToSave()));

	bs.AppendLine();
	inv.ForEach(delegate (InvData x)
	{ if (x.key != 1) bs.AppendLine(x.Owner.EntityId.ToString()); });

	bs.AppendLine();
	ls.Clear();
	Limits.ForEach(x => bs.AppendLine(x.ToSave()));

	return bs.ToString();
}
void Restore(string value, int i = 0)
{
	inv.Clear();
	Out.Clear();
	asm.Clear();
	stor.Clear();
	Limits.Clear();
	Echo("Восстановление настроек...");
	var val = value.Split('\n');
	var cou = val.Length;
	while (i < cou && !string.IsNullOrWhiteSpace(val[i]))
	{
		var st = val[i++].TrimEnd('\r').Split('-');
		Abs_Flat tmp;
		int tp = Abs_Flat.TryParse(x => GridTerminalSystem.GetBlockWithId(long.Parse(x)) as IMyTextSurfaceProvider, st[1], out tmp);
		if (tp == -10) Out.Add(new FindFlat(FindList.Parse(st[0]), tmp));
	}

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		stor.Add(MyRef.Parse(GridTerminalSystem, val[i]));
	}

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		var tb = GridTerminalSystem.GetBlockWithId(long.Parse(val[i]));
		if (tb != null) AddBloc(tb);
	}

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		Limits.Add(MyBuild_Item.Parse(val[i]));
	}

	var u = asm.FindIndex(x => !x.CooperativeMode);
	if (u > 0) asm.Move(u, 0);
	Echo("Восстановление завершено удачно!");
}

//----------------   Classes
public delegate string GetNames(string val);
public class InvDT
{
	public readonly IMyInventory Inv; public readonly IMyTerminalBlock Owner;
	public InvDT(IMyTerminalBlock Bl, byte Index = byte.MaxValue) { Owner = Bl; Inv = Index == byte.MaxValue ? Bl.GetInventory() : Bl.GetInventory(Index); }
	public bool Equals(InvDT obj) => Inv.Equals(obj.Inv);
	public override string ToString() => Owner.CustomName;
}
public class InvData : InvDT
{
	public byte key;

	public InvData(IMyTerminalBlock tb, byte InvInd, byte key_) : base(tb, InvInd) { key = key_; }
	public static bool IsSpecBloc(IMyTerminalBlock tb) { return ((tb is IMyReactor) || (tb is IMyGasGenerator) || (tb is IMyGasTank) || (tb is IMyLargeTurretBase)); }
}

public class FinderItem
{
	public readonly byte Type; public readonly string Name;
	public FinderItem(byte _Type, string _Name) { Type = _Type; Name = _Name; }
	public bool IsSuitable(FinderItem val) { return Type == 0 ? val.Name == Name : val.Type == Type; }
	public bool Equals(FinderItem obj) => Name.Equals(obj.Name);
	public override int GetHashCode() => Name.GetHashCode();
	public override string ToString() => $"{Type}:{Name}";
	public string ToString(GetNames Ut) { return Ut(Name); }
	public static bool TryParse(string val, out FinderItem res)
	{
		var ta = val.Split(':'); byte t;
		if (ta.Length != 2 || !byte.TryParse(ta[0], out t)) { res = null; return false; }
		res = new FinderItem(t, ta[1]);
		return true;
	}
	public static FinderItem Parse(string val)
	{ FinderItem res; if (TryParse(val, out res)) throw new Exception("Error in parse"); return res; }
}
public class FindItem : FinderItem
{
	public bool inv;
	public FindItem(byte _Type, string _Name, bool _inv = false) : base(_Type, _Name) { inv = _inv; }
	public bool Equals(FindItem obj) { return Type == obj.Type && Name == obj.Name && inv == obj.inv; }
	public override string ToString() => $"{Type}:{Name}:{inv}";
	public new string ToString(GetNames Ut) { return (inv ? "!" : "") + Ut(Name); }
	public new static FindItem Parse(string val)
	{
		var ta = val.Split(':');
		if (ta.Length != 3) return null;
		return new FindItem(byte.Parse(ta[0]), ta[1], bool.Parse(ta[2]));
	}
}
public class FindList : List<FindItem>
{
	public FindList() { }
	public FindList(IEnumerable<FindItem> collection) : base(collection) { }
	public bool Equals(FindList val) => Count == val.Count && Count > 0 && FindIndex(x => val.FindIndex(y => x.Equals(y)) < 0) < 0;
	public new void Add(FindItem val)
	{
		if (FindIndex(x => x.Equals(val)) < 0)
		{
			base.Add(val);
			Sort(delegate (FindItem a, FindItem b) { return a.inv == b.inv ? 0 : (a.inv ? -1 : 1); });
		}
	}
	public void AddNonExisting(FindList val) => AddRange(val.FindAll(x => FindIndex(y => y.Equals(x)) >= 0));
	public bool In(FinderItem val) => Find(x => x.Equals(val)) != null;
	public bool IsInclude(FinderItem val)
	{
		bool all_inv = true;
		for (var i = 0; i < Count; i++)
		{
			if (all_inv && !this[i].inv) all_inv = false;
			if (this[i].IsSuitable(val)) return !this[i].inv;
		}
		return all_inv;
	}
	public override string ToString() => string.Join(",", this);
	public string ToString(GetNames Ut)
	{
		var t = new List<string>();
		ForEach(x => t.Add(x.ToString(Ut)));
		return string.Join(",", t);
	}
	public static FindList Parse(string val)
	{
		FindList res = new FindList();
		if (!string.IsNullOrWhiteSpace(val))
		{
			var sr = val.Split(',');
			for (var i = 0; i < sr.Length; i++) res.Add(FindItem.Parse(sr[i]));
		}
		return res;
	}
}



public class Abs_Flat
{
	public delegate bool AsPanel(IMyTerminalBlock x);
	public delegate TypeOfData GetVal<TypeOfData>(string x);
	public object Owner = null;

	protected Abs_Flat() { }
	public Abs_Flat GetThis(object Own) { Owner = Own; return this; }
	public virtual string ToSave() => string.Empty;
	public virtual bool ShowText(string[] vals) => false;
	public virtual void ToBegin() { }
	public virtual TextPanel Find(AsPanel val) => null;
	public static Abs_Flat Parse(GetVal<IMyTextSurfaceProvider> Gr, string val)
	{ Abs_Flat Par; if (TryParse(Gr, val, out Par) >= 0) throw new Exception(""); return Par; }
	public static int TryParse(GetVal<IMyTextSurfaceProvider> Gr, string val, out Abs_Flat res)
	{
		res = null;
		Abs_Flat tmp = null;
		int b = -1, co = 0;
		char ch;
		string s;
		for (var i = 0; i < val.Length; i++)
		{
			ch = val[i];
			if (ch == '{') { if (co++ == 0) b = i; continue; }
			if (ch == '}')
			{
				if (co-- == 0) return i;
				else if (co == 0)
				{
					s = val.Substring(b + 1, i - b - 1);
					var code = TryParse(Gr, s, out tmp);
					if (code > -10) return b + 1 + code;
					if (res == null)
						if (b > 0)
						{
							byte tpb; if (!byte.TryParse(val.Substring(0, b - 1), out tpb)) return 0;
							if (tpb < 2) res = new TextFlat(tpb == 1, tmp);
							else if (tmp is TextPanel) res = new LineFlat(tmp as TextPanel);
							else return b + 1;
						}
						else res = tmp;
					else if (!(res is TextFlat)) return b + 1;
					else (res as TextFlat).Add(tmp);
					b = ++i;
				}
				continue;
			}
			if (co > 0) continue;
			if (ch == ';')
			{
				s = val.Substring(b + 1, i - b - 1);

				if (s.Length == 0 && val[b] != '}') return b + 1;
				int code = TryOneParse(Gr, s, out tmp);
				if (code > -10) return code + b + 1;
				if (res == null) res = tmp;
				else if (!(res is TextFlat)) return b + 1;
				else (res as TextFlat).Add(tmp);
				b = i;
			}
		}
		if (co > 0) return val.Length - 1;
		co = val.Length - 1;
		if (b < co)
		{
			var code = TryOneParse(Gr, val.Substring(b + 1, co - b), out tmp);
			if (code > -10) return code + b + 1;
			if (res == null) res = tmp;
			else if (!(res is TextFlat)) return b + 1;
			else (res as TextFlat).Add(tmp);
		}
		return -10;
	}
	private static int TryOneParse(GetVal<IMyTextSurfaceProvider> Gr, string val, out Abs_Flat res)
	{
		var arp = val.Split('>');
		res = null;
		switch (arp.Length)
		{
			case 1: { var tp = Gr(arp[0]); if (tp == null) return -1; res = new TextPanel(tp); } break;
			case 2:
				{
					byte b; Abs_Flat tmp = null;
					if (!byte.TryParse(arp[0], out b)) return 0;
					var code = TryOneParse(Gr, arp[1], out tmp);
					if (code > -10) return code + arp[0].Length;
					if (b < 2) res = new TextFlat(b == 1, tmp);
					else if (tmp is TextPanel) res = new LineFlat(tmp as TextPanel);
					else return code + arp[0].Length;
				}
				break;
			default: return 0;
		}
		return -10;
	}
}

public class TextPanel : Abs_Flat
{
	public readonly IMyTextSurface Surface;
	public readonly IMyTerminalBlock OwnerBloc;
	int _cou = 0;
	public TextPanel(IMyTextSurfaceProvider block)
	{
		OwnerBloc = (IMyTerminalBlock)block;
		Surface = block.GetSurface(0);
		Surface.ContentType = ContentType.TEXT_AND_IMAGE;
	}
	public override string ToString() => OwnerBloc.CustomName;
	public override string ToSave() => OwnerBloc.EntityId.ToString();
	public override bool ShowText(string[] vals) => ShowText(GetStr(vals));
	public bool ShowText(string val)
	{
		Surface.WriteText(val + "\n", true);

		StringBuilder nw = new StringBuilder();
		Surface.ReadText(nw, false);
		var sz = Surface.MeasureStringInPixels(nw, Surface.Font, Surface.FontSize);
		return (nw.Length > 0 && sz.Y <= Surface.SurfaceSize.Y - Surface.TextPadding);
	}
		//return ++_cou >= (int)(18 / Surface.FontSize); }
	public override void ToBegin() { _cou = 0; Surface.WriteText(String.Empty); }
	public override TextPanel Find(AsPanel val) => val(OwnerBloc) ? this : null;
	public int GetLines() => 0;
	public static string GetStr(string[] vals)
	{
		switch (vals.Length)
		{
			case 0: return string.Empty;
			case 1: return vals[0];
			default: return string.Format(vals[0], vals);
		}
	}
}

public class TextFlat : Abs_Flat
{
	public List<Abs_Flat> Flats { get; } = new List<Abs_Flat>();
	public bool Hor;
	int _Tec = 0;
	public void Add(Abs_Flat val) => Flats.Add(val.GetThis(this));
	public override void ToBegin() { _Tec = 0; Flats.ForEach(x => x.ToBegin()); }
	public TextFlat(bool horis, Abs_Flat First, params Abs_Flat[] other)
	{ Hor = horis; Flats.Add(First.GetThis(this)); for (var i = 0; i < other.Length; i++) Flats.Add(other[i].GetThis(this)); }
	public override string ToString() => string.Format("{{{0}:{1}}}", Hor ? "Horizontal" : "Vertical", string.Join(", ", Flats));
	public override string ToSave()
	{
		var res = new StringBuilder("{" + (Hor ? "1" : "0") + ">");
		var tmp = new List<string>(Flats.Count);
		Flats.ForEach(x => tmp.Add(x.ToSave()));
		res.Append(string.Join(";", tmp) + "}");
		return res.ToString();
	}
	public override bool ShowText(string[] text)
	{
		var Max = Flats[_Tec].ShowText(text);
		if (Flats.Count == 1) return Max;
		if (Hor) { if (++_Tec == Flats.Count) _Tec = 0; else Max = false; }
		else if (Max && Flats.Count - _Tec > 1) _Tec++;
		return Max;
	}
	public override TextPanel Find(AsPanel val)
	{
		TextPanel res = null;
		for (var i = 0; i < Flats.Count && res == null; i++) res = Flats[i].Find(x => val(x));
		return res;
	}
}

public class LineFlat : TextFlat
{
	public void Add(TextPanel val) => base.Add(val);
	public LineFlat(TextPanel First, params TextPanel[] other) : base(false, First, other) { }
	public override string ToString() => $"{{InLine:{string.Join(", ", Flats)}}}";
	public override string ToSave()
	{
		var res = new StringBuilder("{2>");
		var tmp = new List<string>(Flats.Count);
		Flats.ForEach(x => tmp.Add(x.ToSave()));
		res.Append(string.Join(";", tmp) + "}");
		return res.ToString();
	}
	public override bool ShowText(string[] text)
	{
		int ml = text.Length - 1, _mx = Math.Min(Flats.Count - 1, ml), i;
		if (ml < 1) return (Flats[0] as TextPanel).ShowText(text);
		var MAX = false;
		for (i = 0; i < _mx; i++) MAX = (Flats[i] as TextPanel).ShowText(text[i + 1]) || MAX;
		_mx = ml - Flats.Count;
		if (_mx == 0) MAX = (Flats[i] as TextPanel).ShowText(text[i + 1]) || MAX;
		else if (_mx > 0)
		{
			ml = text[0].IndexOf("{" + (i + 1) + "}");
			MAX = (Flats[i] as TextPanel).ShowText(string.Format(text[0].Substring(ml), text)) || MAX;
		}
		return MAX;
	}
}

public class FindFlat : FindList
{
	public List<string[]> Texts { get; } = new List<string[]>();
	public readonly Abs_Flat Flat;

	public FindFlat(Abs_Flat Tp) { Flat = Tp.GetThis(this); }
	public FindFlat(FindList Fl, Abs_Flat Tp) : base(Fl) { Flat = Tp.GetThis(this); }
	public void ShowText() => Texts.ForEach(x => Flat.ShowText(x));
	public override string ToString() => base.ToString(LangDic.GetName) + "-" + Flat.ToString();
	public string ToSave() => base.ToString() + "-" + Flat.ToSave();
}

public class TextsOut : List<FindFlat>
{
	public StringBuilder Uncown { get; } = new StringBuilder();
	private new void Add(FindFlat val) => Add(val);
	public bool AddText(FinderItem Type, params string[] Val)
	{
		var res = Find(x => x.IsInclude(Type));
		if (res == null) { Uncown.AppendLine(TextPanel.GetStr(Val)); return false; }
		res.Texts.Add(Val);
		return true;
	}
	public bool AddText(MyInvItem Sel) { return AddText(Sel.Lnk, "{1}: {2}", Sel.ShowName, Sel.count.ToString("#,##0.##", SYS)); }
	public bool AddText(params string[] Val)
	{
		var res = Find(x => x.Count == 0);
		if (res == null) { Uncown.AppendLine(TextPanel.GetStr(Val)); return false; }
		res.Texts.Add(Val);
		return true;
	}
	public void ShowText() => ForEach(x => x.ShowText());
	public bool FindPanel(Abs_Flat.AsPanel val, out FindFlat Fp, out TextPanel Tp)
	{
		TextPanel Tp_ = null;
		Fp = Find(fp => { Tp_ = fp.Flat.Find(x => val(x)); return Tp_ != null; });
		return (Tp = Tp_) != null;
	}
	public void ClearText() { ForEach(x => { x.Texts.Clear(); x.Flat.ToBegin(); }); Uncown.Clear(); }
}

public class MyInvIt : FinderItem
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
			/*case "Components": tp = 2; break;
					case "HandTool": tp = 3; break;
					case "Ammo": tp = 4; break;*/
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
	public MyInvItem Clone(double Count = double.NaN) => new MyInvItem(Lnk) { count = double.IsNaN(Count) ? this.count : Count };
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
public class MyRef : FindList
{
	public InvDT Inv { get; }
	public MyRef(IMyTerminalBlock Bloc) { Inv = new InvDT(Bloc); }
	public override string ToString()
	{
		var ls = new List<string>();
		ForEach(x => ls.Add(x.ToString(LangDic.GetName)));
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
	public int GetInv_Move(FinderItem It, IMyInventory Inv, ref int i)
	{
		int beg = i;
		if (beg == 0)
		{
			i = FindIndex(x => x.Inv.Inv == Inv);
			if (i >= 0 && this[i].IsInclude(It))
				return i = -1;
		}

		for (i = beg; i < Count; i++)
		{
			if (this[i].Inv.Inv != Inv
				&& Inv.IsConnectedTo(this[i].Inv.Inv)
				&& this[i].IsInclude(It)
				&& (this[i].Inv.Inv.MaxVolume - this[i].Inv.Inv.CurrentVolume).RawValue > 100)
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
}//ggg

bool Between(int val, int min, int max) => val >= min && val <= max;

}
// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
static class Class1
{
    public delegate void ShowError(string val);
    public static int asInt(this string val, int def = 0)
    {int res; return int.TryParse(val, out res) ? res : def; }
    public static int asInt(this string val, ShowError func)
    {
        int res; if (!int.TryParse(val, out res)) func(val);
        return res;
    }

    public static string Begin(this string msk, char ch, bool cut = false)
    {
        var i = msk.IndexOf(ch);
        string res = i < 0 ? msk : msk.Substring(0, i);
        if (cut) msk = i < 0 ? string.Empty : msk.Substring(i+1);
        return res;
    }
    public static string Substring(this string val, int ind, string to)
    {
        var e = val.IndexOf(to, ind + 1);
        return e < 0 ? val.Substring(ind) : val.Substring(ind, e - ind);
    }

    public static MyCommandLine ComLine(this string arg)
    { var res = new MyCommandLine(); return res.TryParse(arg)? res: null; }