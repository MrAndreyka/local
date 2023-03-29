/*
 *  --------------------------------------------------------------------------      
 *     AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com 
 *     When using and disseminating information about the authorship is obligatory 
 *     При использовании и распространении информация об авторстве обязательна 
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
					string param; int i = 1;bool isOne = parms.Switch("inline");

					while ((param = parms.Argument(i++)) != null)
					{
						var L = new List<IMyTerminalBlock>();
						new Selection(param).FindBlocks(GridTerminalSystem, L, x => x is IMyTextSurfaceProvider);
						if (L.Count == 0) { Echo("Не найдены панели по запросу"); return; }

						//if(isOne)
							L.Sort((x, y) => x.CustomData.CompareTo(y.CustomData));

						var fp = L.Find(x => !string.IsNullOrWhiteSpace(x.CustomData));
						if(fp==null) { Echo("Не найдены параметры в CustomData"); return; }

						var mask = ParseMask(fp.CustomData);
						if(mask.Count==0) { Echo("Не верная маска в CustomData"); return; }
						var tmp = new Selected_Panel<Sel_Group, Item_sel>(mask);

						L.ForEach(x => tmp.Flats.Add(new Txt_Surface(x as IMyTextSurfaceProvider)));
						var tecSel = Out.Find(x => x.Equals(tmp.Select));//Поиск с такими же настройками
						if (tecSel != null) Out.Remove(tecSel);

						Echo("Установлены панели: " + tmp.ToString());
						Out.Add(tmp);
					}
				}
				break;
			case "panel":
				SetPanel(parms);
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
					Txt_null tp;
					var sb = new StringBuilder("Удалены:\n");
					while ((tp = Out.FindPanel(x => sl.Complies(x.OwnerBloc.CustomName))) != null)
					{
						sb.AppendLine((tp as Txt_Surface).OwnerBloc.ToString());
						Txt_Panel ow = tp.Owner;
						while (ow.Owner != null && ow.Flats.Count == 1)
						{ tp = ow; ow = ow.Owner; }

						if (ow.Flats.Count == 1)
							Out.Remove(ow as Selected_Panel<Sel_Group, Item_sel>);
						else ow.Flats.Remove(tp);
					}
					Echo(tp != null ? sb.ToString() : $"Панели по маске \"{sl.Value}\" не найдены");
				}
                break;
            case "panel+":
                {
                    if (!Between(parms.ArgumentCount, 3, 4)) {
						Echo("Ожидается 2-3 параметра: <Индекс шаблона или Название панели> " +
						  "<Маска поиска добавляемых блоков> Горизонтальная группа"); return; }

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
					L.ForEach(x => tp.Flats.Add(new Txt_Surface(x)));
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
				{
					bool First = true;
					var buf = new StringBuilder();
					var all = parms.ArgumentCount == 1;
					if (all || parms.Argument(1).Contains("storage"))
					{
						if (!First) buf.AppendLine().AppendLine("Склады:"); First = false;
						buf.AppendLine(string.Join("\r\n", stor));
					}
					if (all || parms.Argument(1).Contains("limit"))
					{
						if (!First) buf.AppendLine().AppendLine("Лимиты:"); First = false;
						buf.AppendLine(string.Join("\r\n", Limits));
					}
					if (all || parms.Argument(1).Contains("ass"))
					{
						if (!First) buf.AppendLine().AppendLine("Сборщики:"); First = false;
						asm.ForEach(x => buf.AppendLine(x.CustomName + (x.CooperativeMode ? "" : " >> основной")));
					}
					if (all || parms.Argument(1).Contains("panel"))
					{
						if (!First) buf.AppendLine().AppendLine("Панели:"); First = false;
						buf.AppendLine(Out.ToString());
					}
					if (all || parms.Argument(1).Contains("bloc"))
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
	Me.CustomData = Storage;
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
	inv.ForEach(delegate (InvData x)
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

public bool SetPanel(MyCommandLine param)
{
	if (param.ArgumentCount < 3)
		{ Echo("Ожидается 2 параметра: Что {Куда1,Куда2:hor}"); return false; }
	var tmp = new Selected_Panel<Sel_Group, Item_sel>(ParseMask(param.Argument(1)));

	var sl = new Selection(null);int v = 0;
	var er = Txt_Panel.TryParse(tmp, param.Argument(2), ref v, (name, ind)
	=> sl.Change(name).FindBlocks<IMyTextSurfaceProvider>(GridTerminalSystem, x => x.SurfaceCount > ind));

	var tecSel = Out.Find(x => x.Equals(tmp.Select));//Поиск с такими же настройками
	if (tecSel != null) Out.Remove(tecSel);

	Echo("Установлены панели: " + tmp.ToString());
	Out.Add(tmp);
	return true;
}
bool AddBloc(IMyTerminalBlock X)
{
	if (X is IMyAssembler) asm.Add(X as IMyAssembler);
	if (!X.HasInventory || inv.FindIndex(x => x.Inventory.Owner == X) >= 0) return false;
	byte key = 0;
	if (stor.Find(x => x.Inv.Owner == X) != null) key = 5;
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
		string param = msk.Begin(':');
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

void Restore(string value, int i = 0)
{
	inv.Clear();
	Out.Clear();
	asm.Clear();
	stor.Clear();
	Limits.Clear();
	Echo("Восстановление настроек...");
	int g = 0;

	if (i > 0) { var k = i; value.First(x => { g++; return x == '\n' && (--k == 0); }); }

	i++; var sl = new Selection(null);
	var l = new List<IMyTextSurfaceProvider>(1);
	var res = Out.TryParse(value, ref g, Sel_Group.Parse, (n, ind) =>
	{
		l.Clear();
		l.Add(GridTerminalSystem.GetBlockWithId(n.AsLong()) as IMyTextSurfaceProvider); return l;
	});

	var val = value.Substring(g).Split('\n');
	var cou = val.Length;

	Echo($"1 {cou}   {i}");

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		stor.Add(MyRef.Parse(GridTerminalSystem, val[i]));
	}
	Echo($"2 {cou}   {i}");

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		var tb = GridTerminalSystem.GetBlockWithId(long.Parse(val[i]));
		if (tb != null) AddBloc(tb);
	}
	Echo($"3 {cou}   {i}");

	for (i++; i < cou; i++)
	{
		if (string.IsNullOrWhiteSpace(val[i])) break;
		Limits.Add(MyBuild_Item.Parse(val[i]));
	}
	Echo($"4 {cou}   {i}");

	var u = asm.FindIndex(x => !x.CooperativeMode);
	if (u > 0) asm.Move(u, 0);
	Echo("Восстановление завершено удачно!");
}

string ToSave()
{
	var bs = new StringBuilder();
	bs.AppendLine(Out.ToString(true));

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