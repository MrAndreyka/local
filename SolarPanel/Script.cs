/**--------------------------------------------------------------------------       
*   AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com        
*   When using and disseminating information about the authorship is obligatory
*   При использовании и распространении информация об авторстве обязательна       
*----------------------------------------------------------------------*/

/* set,init:мотор горизонт: мотор верт(:текст. панель(:индекс экрана)) - инициализация
start/stop - запуск/остановка скрипта
panels,solars:маска - маска имени солнечных панелей
day:минуты - установка длительности дня
? - информация
+,-(:сек) - Коректировка текущего угла
sunpath(:угол) - вывод точек координат солнца
`:номер строки - выполнить команду в строке CustomData
sunhere:ИмяБлока - Производится замер положения солнца, передом блока или прицелом турели
После минимум 2 -ух замеров можно вычислить ось солнца
sunaxisbloc:ИмяБлока - Расчитывается блок солнца по данным блока которым производились замеры
sunaxis:вектор - установка вектора оси солнца
sunaxis:вектор,вектор2 - расчет вектора оси солнца по двум векторам направления
angelx:угол - коректировка вектора земли, при неправельной установке ротора
invz - Инвертирует поворот по вертикали
=:Текущий угол солнца*/

Program()
{
    if (Storage.StartsWith("SunRound") && Me.TerminalRunArgument != "null")
        try
        {
            var ids = Storage.Split('\n');
            var cou = ids.Length;
            int i = 2;
            TecSec = int.Parse(ids[i++]);
            MinInDay = int.Parse(ids[i++]);

            string s;
            if (!string.IsNullOrWhiteSpace(ids[i]))
            {
                MotorX = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i++])) as IMyMotorStator;
                MotorY = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i++])) as IMyMotorStator;
                s = ids[i++];
                if (!string.IsNullOrEmpty(s))
                {
                    var prm = s.Split(':');
                    Panel.SetSurface(GridTerminalSystem.GetBlockWithId(long.Parse(prm[0])) as IMyTextSurfaceProvider, prm.GetLength(0) > 1 ? byte.Parse(prm[1]) : (byte)0);
                }
                Data = new SunRound(ids, i);
                i = 13;

            }
            else i = 5;
            for (; i < cou; i++)
                if (!string.IsNullOrEmpty(ids[i]))
                {
                    var sp = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i])) as IMySolarPanel;
                    if (sp != null) SolarPanels.Add(sp);
                }
            if (bool.Parse(ids[1])) Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Echo("Состояние востановлено");
        }
        catch (Exception e) { Echo("Error"); Me.CustomData += "\n\n" + e + "\n" + Storage; }
    else if (!string.IsNullOrWhiteSpace(Me.CustomData)) SetAtributes(Me.CustomData.Split('\n'));
}

void Save()
{
    StringBuilder res = new StringBuilder();
    res.AppendLine((Runtime.UpdateFrequency != UpdateFrequency.None).ToString());
    res.AppendLine(TecSec.ToString());
    res.AppendLine(MinInDay.ToString());
    if (MotorX != null)
    {
        res.AppendLine(MotorX.EntityId.ToString());
        res.AppendLine(MotorY.EntityId.ToString());
        res.AppendLine(Panel.Surface != null ? Panel.Bloc.EntityId.ToString() : "");
        res.AppendLine(Data.Gr_Aix.ToString());
        res.AppendLine(Data.Gr_Zero.ToString());
        res.AppendLine(Data.Sun_Aix.ToString());
        res.AppendLine(Data.Sun_Zero.ToString());
        res.AppendLine(Data.InvZ.ToString());
    }
    else res.AppendLine(" ");
    SolarPanels.ForEach(x => res.AppendLine(x.EntityId.ToString()));
    Storage = "SunRound\n" + res.ToString();
}

static SunRound Data = new SunRound();
int TecMS = 0, TecSec = 0, MinInDay = 120, Del;

static IMyMotorStator MotorX, MotorY;
static readonly MyTextSurface Panel = new MyTextSurface();
readonly List<IMySolarPanel> SolarPanels = new List<IMySolarPanel>();



void Main(string argument, UpdateType Type)
{
    try
    {
        TecMS += (int)Runtime.TimeSinceLastRun.TotalMilliseconds;

        if (Type <= UpdateType.Update1 && !string.IsNullOrWhiteSpace(argument))
        {
            Panel.WriteText("");
            SetAtributes(argument.Split(';'));
            return;
        }


        if (Del > 0) Del -= TecMS;
        TecSec += TecMS / 960;
        TecMS %= 960;

        if (TecSec > MinInDay * 60) TecSec -= MinInDay * 60;
        Panel.WriteText("");

        float X, Z;
        var Tgr = MathHelper.TwoPi / MinInDay * TecSec / 60;
        var vSun = Data.GetToSun(Tgr);
        Data.GetAngels(vSun, out X, out Z);

        MotorX.TargetVelocityRad = ConvertAngel(X - MotorX.Angle);
        MotorY.TargetVelocityRad = ConvertAngel(Z - MotorY.Angle);

        if (Panel != null && Del < 1)
        {
            StringBuilder s = new StringBuilder("Тек: " + TecSec / 60 + "м." + TecSec % 60 + "с. из " + MinInDay + "м.\n");
            s.AppendFormat("{0:0}° = {1:0.00000} рад.\n", MathHelper.ToDegrees(Tgr), Tgr);
            s.AppendFormat("A {0:0}°  Z {1:0}°\n", MathHelper.ToDegrees(X), MathHelper.ToDegrees(Z));
            s.AppendFormat("Тек: X {0:0.0} Z {1:0.0}\n", MathHelper.ToDegrees(MotorX.Angle), MathHelper.ToDegrees(MotorY.Angle));

            if (SolarPanels.Count > 0)
            {
                float Pow = 0, Mpow = 0;
                SolarPanels.ForEach(x => { Pow += x.CurrentOutput; Mpow += x.MaxOutput; });
                s.AppendFormat("Мощность: {1:0.##}кВт * {4} = {0:0.##}мВт\nМакс: {2:0.##}мВт ({3:0.##}кВт)",
                    Pow, SolarPanels[0].CurrentOutput * 1000, Mpow, SolarPanels[0].MaxOutput * 1000, SolarPanels.Count);
            }
            TextOut(s.ToString(), false, false);
        }
    }
    catch (Exception e) { Echo(e.ToString()); }

}

float ConvertAngel(float Vel)
{
    if (float.IsNaN(Vel)) Vel = 0;
    if (Vel < -Math.PI) Vel += MathHelper.TwoPi;
    else if (Vel > Math.PI) Vel -= MathHelper.TwoPi;
    return MathHelper.Clamp(Vel/2, -0.45f, 0.45f);
}
public Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
=>new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Backward));

void SetAtributes(string[] Args)
{
    int Len = Args.GetLength(0);
    Delay(30);
    for (int i = 0; i < Len; i++)
    {
        var Arg = Args[i].Split(':');
        if (Arg.Length == 0) continue;
        switch (Arg[0].ToLower())
        {
            case "`":
                {
                    if (Arg.Length < 2)
                    { SetAtributes(Me.CustomData.Split('\n')); break; }
                    int g;
                    if (Arg.Length < 2 || !int.TryParse(Arg[1], out g))
                    { TextOut("Не верное значение строки \"" + Arg[1] + "\""); continue; }
                    var ms = Me.CustomData.Split('\n');
                    if (ms.GetLength(0) <= g)
                    { TextOut("Строка " + g + " не существует"); continue; }
                    SetAtributes(ms[g].Split(';'));
                }
                break;
            case "sunhere":
                {
                    if (Arg.Length < 2) { TextOut("Ожидается имя блока указывающего на солнце"); return; }
                    var bloc = new Selection(Arg[1]).FindBlock<IMyTerminalBlock>(GridTerminalSystem);
                    if (bloc == null) { TextOut("Не найден блок по запросу:" + Arg[1]); return; }
                    Vector3D Target = bloc.WorldMatrix.Forward;
                    if (bloc is IMyLargeTurretBase) //Любая турель большой сетки
                    {
                        var gt = bloc as IMyLargeTurretBase;
                        var mm = MatrixD.CreateFromAxisAngle(gt.WorldMatrix.Right, gt.Elevation);
                        Vector3D.Transform(ref Target, ref mm, out Target);
                        mm = MatrixD.CreateFromAxisAngle(gt.WorldMatrix.Up, gt.Azimuth);
                        Vector3D.Transform(ref Target, ref mm, out Target);
                    }
                    bloc.CustomData += "\n" + MyGPS.GPS("SunPos", Target);
                }
                break;
            case "sunaxisbloc":
                {
                    if (Arg.Length < 2) { TextOut("Ожидается имя блока"); return; }
                    var bloc = new Selection(Arg[1]).FindBlock<IMyTerminalBlock>(GridTerminalSystem);
                    if (bloc == null)
                    { TextOut("Не найден блок по запросу:" + Arg[1]); return; }
                    else if (string.IsNullOrWhiteSpace(bloc.CustomData))
                    { TextOut("В блоке нет данных по положению солнца"); return; }

                    string Beg = null, End = null;
                    foreach (var str in bloc.CustomData.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Echo(str + str.Length);
                        if (!str.StartsWith("GPS:SunPos")) continue;
                        else if (Beg == null) Beg = str;
                        else End = str;
                    }

                    if (End == null) { TextOut("Указание солнца производилось лишь раз"); return; }
                    SetAtributes(new string[] { $"sunaxis:{Beg},{End}" });
                }
                break;
            case "sunaxis":
                {
                    if (Arg.Length < 2) { TextOut("Ожидается вектор"); return; }
                    Arg = string.Join(":", Arg, 1, Arg.Length - 1).Split(',');
                    Vector3D tmp;
                    if (MyGPS.TryParseVers(Arg[0], out tmp))
                    {
                        Data.Sun_Aix = Vector3D.Normalize(tmp);
                        if (Arg.Length > 1)
                        {
                            TextOut("Расчет по 2 векторам:");
                            if (MyGPS.TryParseVers(Arg[1], out tmp))
                                Data.Sun_Aix = Vector3D.Normalize(Data.Sun_Aix.Cross(Vector3D.Normalize(tmp)));
                            else TextOut($"Ошиба значения вектора: {Arg[1]}");
                        }
                        TextOut("Установлена: " + V3DToStr(Data.Sun_Aix, "Ось солнца", 50000));
                        SetAtributes(new string[] { "sunpath" });
                    }
                    else TextOut($"Ошиба значения вектора: {Arg[0]}");
                }
                break;
            case "start":
                {
                    if (MotorX == null)
                    { TextOut("Сначала необходимо указать названия блоков. Используйте команду \"set\""); continue; }
                    if (SolarPanels.Count == 0) SetAtributes(new string[] { "panels" });
                    if (Len == 1) Delay(0);
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                }
                break;
            case "stop":
                {
                    if (MotorX == null) continue;
                    MotorX.SetValue("Velocity", (Single)0);
                    MotorY.SetValue("Velocity", (Single)0);
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    TextOut("Выполнение остановлено");
                }
                break;
            case "angelx":
                {
                    if (MotorX == null) continue;
                    float delta = 0;
                    if (Arg.Length > 1 && !float.TryParse(Arg[1], out delta)) { TextOut("Ошиба значения угла"); continue; }
                    Data.Gr_Zero = SunRound.TurnVector(Data.Gr_Zero, Data.Gr_Aix, MathHelper.ToRadians(delta));
                    TextOut("Корректировка угла " + delta + "\n" + V3DToStr(Data.Gr_Zero, "Нулевой вектор земли", 10));
                }
                break;
            case "invz": Echo("Инверсия " + ((Data.InvZ = !Data.InvZ) ? "вкл" : "выкл")); break;
            case "?":
                {
                    StringBuilder s = new StringBuilder();
                    s.AppendLine($"Длительность дня {MinInDay} мин.");
                    s.AppendLine(V3DToStr(Data.Gr_Aix, "Ось земли", 10));
                    s.AppendLine(V3DToStr(Data.Gr_Zero, "Нулевой вектор \"земли\"", 10));
                    s.AppendLine(V3DToStr(Data.Sun_Aix, "Ось солнца", 50000));
                    s.AppendLine(V3DToStr(Data.Sun_Zero, "Нулевой вектор солнца", 50000));

                    if (Data.InvZ) s.Append("Инверсия Z");
                    TextOut(s.ToString(), true, true, true);
                }
                break;
            case "day":
                if (Arg.Length < 2) TextOut("Ожидается параметр");
                else TextOut(int.TryParse(Arg[1], out MinInDay) ? $"Установлена длительность дня { MinInDay} мин." :
                    "Ошиба значения длительности дня");
                break;
            case "+":
                {
                    int pos;
                    if (Arg.Length < 2) pos = 60;
                    else if (!int.TryParse(Arg[1], out pos))
                    { TextOut("Ошиба значения корректировки тек. времени"); continue; }
                    TecSec += pos;
                    TextOut(string.Format("Увеличено текущее время на {0} сек.\n{1}сек. ({2:0.#}°)",
                    pos, TecSec, 180f / MinInDay * 2 * TecSec / 60));
                }
                break;
            case "-":
                {
                    int pos;
                    if (Arg.Length < 2) pos = 60;
                    else if (!int.TryParse(Arg[1], out pos))
                    { TextOut("Ошиба значения корректировки тек. времени"); continue; }
                    TecSec -= pos;
                    while (TecSec < 0) TecSec += MinInDay * 60;
                    TextOut(string.Format("Уменьшено текущее время на {0} сек.\n{1}сек. ({2:0.#}°)",
                    pos, TecSec, 180f / MinInDay * 2 * TecSec / 60));
                }
                break;
            case "=":
                {
                    int pos;
                    if (Arg.Length < 2 || !int.TryParse(Arg[1], out pos))
                    { TextOut("Ошиба значения тек. времени"); continue; }
                    var val = MathHelper.ToRadians(pos);
                    TextOut(string.Format("Текущее время установлено {0} сек.", TecSec = (int)(MinInDay * 60 / MathHelper.TwoPi * val)));
                }
                break;
            case "panels":
            case "solars":
                {
                    new Selection(Arg.Length < 2 ? "" : Arg[1]).FindBlocks<IMySolarPanel>(GridTerminalSystem, SolarPanels);
                    var s = new StringBuilder();
                    s.AppendLine("Найдено панелей - " + SolarPanels.Count + " :");
                    SolarPanels.ForEach(x => s.AppendLine(x.CustomName));
                    TextOut(s.ToString());
                }
                break;
            case "panel":
                {
                    var def = Arg.Length < 2 ? "" : Arg[1];
                    byte pos = 0;
                    if (Arg.Length > 2 && !byte.TryParse(Arg[2], out pos)) TextOut("Не верное значение номера панели");
                    if (!Panel.SetSurface(new Selection(def).FindBlock<IMyTextSurfaceProvider>(GridTerminalSystem), pos))
                    { Echo("Не найдена панель: " + def); continue; }
                    TextOut("Панель установлена");
                }
                break;
            case "set":
            case "init":
                {
                    if (Arg.Length < 3 || Arg.Length > 5)
                    {
                        TextOut("При установке требуется указать через \":\" названия следующих блоков:\n" +
                            "МоторГор:МоторВерт{:Название текстовой панели}");
                        continue;
                    }
                    if (Arg.Length > 3) SetAtributes(new string[] { $"panel:{Arg[3] + (Arg.Length > 4 ? ":" + Arg[4] : "")}" });
                    MotorY = GridTerminalSystem.GetBlockWithName(Arg[2]) as IMyMotorStator;
                    if (MotorY == null) { TextOut("Не найден вертикальный мотор - " + Arg[2]); continue; }
                    MotorX = GridTerminalSystem.GetBlockWithName(Arg[1]) as IMyMotorStator;
                    if (MotorX == null) { TextOut("Не найден горизонтальный мотор - " + Arg[1]); continue; }
                    Data.Gr_Aix = MotorX.WorldMatrix.Up;
                    Data.Gr_Zero = MotorX.WorldMatrix.Forward;
                    if (Vector3D.IsZero(Data.Sun_Aix))
                        Data.Sun_Aix = new Vector3D(0, 1, 0); // Для планет
                    TextOut("Инициализация завершена успешно");
                    TecMS = 0;
                }
                break;
            case "sunpath":
                {
                    var ps = Me.GetPosition();
                    if (Vector3D.IsZero(Data.Sun_Aix)) Data.Sun_Aix = new Vector3D(0, 1, 0);// Для планет
                    int shag = 30;
                    if (Arg.Length >= 2) int.TryParse(Arg[1], out shag);
                    var s = new StringBuilder();
                    for (int a = 0; a < 360; a += shag)
                    {
                        var vSun = Data.GetToSun(MathHelper.ToRadians(a));
                        s.AppendLine(MyGPS.GPS("SP " + a + "°", ps + vSun * 100000));
                    }
                    TextOut(s.ToString(), true, false, true);
                }
                break;
            default:
                Echo("Неизвестная команда: \"" + string.Join("", Arg));
                break;
        }
    }
}

void TextOut(string Text, bool ToBar = true, bool append = true, bool ToCustomData = false)
{
    if (Panel.Surface != null)
        if (ToCustomData && Panel.Bloc != null) Panel.Bloc.CustomData = Text;
        else Panel.WriteText(Text, append);
    if (ToBar || Panel == null) Echo(Text);
}

string V3DToStr(Vector3D val, string name, int dist)
{ return (MotorX == null) ? name + ": " + val : MyGPS.GPS(name, val, MotorX.GetPosition(), dist); }
void Delay(int ms = 120) { if (Del < ms + TecMS) Del = ms + TecMS; }


public class SunRound
{
    public bool InvZ = false;
    public Vector3D Gr_Aix, Gr_Zero;
    Vector3D Sun_Aix_, Sun_Zero_;

    public SunRound() { }
    public SunRound(string[] ids, int beg)
    {
        Vector3D.TryParse(ids[beg], out Gr_Aix);
        Vector3D.TryParse(ids[beg + 1], out Gr_Zero);
        Vector3D.TryParse(ids[beg + 2], out Sun_Aix_);
        Vector3D.TryParse(ids[beg + 3], out Sun_Zero_);
        InvZ = bool.Parse(ids[beg + 4]);
    }

    public Vector3D Sun_Zero { get { return Sun_Zero_; } }
    public Vector3D Sun_Aix
    {
        get { return Sun_Aix_; }
        set
        {
            Sun_Aix_ = value; Sun_Zero_ = Vector3D.CalculatePerpendicularVector(Sun_Aix_);
        }
    }

    public static Vector3D TurnVector(Vector3D val, Vector3D Axis, float angel)
    {
        var M = Matrix.CreateFromAxisAngle(Axis, angel);
        return Vector3D.Normalize(Vector3D.Transform(val, M));
    }
    public Vector3D GetToSun(float angel)
    {
        if (Vector3D.IsZero(Sun_Zero_))
            Sun_Zero_ = Vector3D.CalculatePerpendicularVector(Sun_Aix_);
        return TurnVector(Sun_Zero_, Sun_Aix_, angel);
    }
    public void GetAngels(Vector3D SunPos, out float X, out float Z)
    {
        var PrToGr = Vector3D.Reject(SunPos, Gr_Aix);
        X = MyMath.AngleBetween(Gr_Zero, PrToGr);
        Z = MyMath.AngleBetween(SunPos, PrToGr);
        if (Vector3D.Dot(Gr_Zero.Cross(Gr_Aix), PrToGr) < 0) X = MathHelper.TwoPi - X;
        if (Vector3D.Dot(Sun_Aix_, PrToGr) < 0) Z = MathHelper.TwoPi - Z;
        if (InvZ) Z = -Z;
    }
}
public static class MyGPS
{
    public static string GPS(string Name, Vector3D Val)
    { return string.Format("GPS:{0}:{1:0.##}:{2:0.##}:{3:0.##}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
    public static string GPS(string Name, Vector3D Direct, Vector3D Pos, double dist = 0, string format = "")
    {
        Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
        return string.Format("GPS:{0}:{1}:{2}:{3}:",
            Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format));
    }
    public static string Vec_GPS(string Name, Vector3D Direct, Vector3D Pos, double dist, string format = "")
    {
        Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
        return string.Format("{0}:{4}\nGPS:{0}:{1}:{2}:{3}:",
          Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format), Direct.ToString(format));
    }
    public static bool TryParse(string vector, out Vector3D res)
    {
        var p = vector.Split(':');
        if (p.GetLength(0) == 6)
        {
            float x, y, z;
            if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
                res = new Vector3D(x, y, z);
            else
                res = Vector3D.Zero;
        }
        else res = Vector3D.Zero;
        return res != Vector3D.Zero;
    }
    public static bool TryParseVers(string vector, out Vector3D res)
    {
        if (!vector.StartsWith("{")) return TryParse(vector, out res);
        return Vector3D.TryParse(vector, out res);
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
}

public class MyTextSurface
{
    public IMyTerminalBlock Bloc { get; private set; }
    public IMyTextSurface Surface { get; private set; } = null;
    public byte Index { get; private set; }

    public bool SetSurface(IMyTextSurfaceProvider bloc, byte index = 0)
    {
        if (bloc == null || bloc.SurfaceCount <= Index) return false;
        Bloc = bloc as IMyTerminalBlock;
        Index = index;
        Surface = bloc.GetSurface(index);
        return true;
    }
    public void WriteText(string Text, bool append = false) => Surface?.WriteText(Text, append);
    public override string ToString() => Surface != null ? $"{Bloc.EntityId.ToString()}:{Index}" : "";
}