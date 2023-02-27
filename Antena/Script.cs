/*
 *  *  --------------------------------------------------------------------------      
 *  *     AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com 
 *  *     When using and disseminating information about the authorship is obligatory 
 *  *     При использовании и распространении информация об авторстве обязательна 
 *  *  --------------------------------------------------------------------------
 */



const string tag_id = "A_Mes";
MyIGCMessage? NexMes = null;
readonly IMyMessageProvider MesBProv;
readonly IMyMessageProvider MesUProv;
readonly Text txt = null;
readonly IMyRadioAntenna Ant = null;
public Dictionary<String, com_> Coms = new Dictionary<String, com_>();
double tic = 0;

// ---------------------------------- MAIN ---------------------------------------------
void Main(string argument, UpdateType tpu)
{
    try
    {
        if ((tpu & (UpdateType.Trigger | UpdateType.Terminal)) > 0
        || (tpu & (UpdateType.Mod)) > 0
        || (tpu & (UpdateType.Script)) > 0)
        {
            var fparam = argument.COmLine();
            if (fparam == null || fparam.ArgumentCount == 0)
            { Echo("Нет параметров"); return; }
            switch (fparam.Argument(0).ToLower())
            {
                case "#send_pos":
                    Go("send_pos", "", 0);
                    break;
                case "send":
                    SendMessage(fparam.Argument(1));
                    break;
                case "com+":
                    {
                        var p = fparam.Argument(1);
                        var ind = fparam.Switch("");
                        com_ x = null;
                        if (Coms.ContainsKey(p))
                            x = Coms[p];
                        else Coms.Add(p, x = new com_());
                        x.command = fparam.Argument(2);
                        x.brod = fparam.Switch("B");

                        Echo($"{p} [{(x.brod ? "B" : "U")}] {x.command}");
                    }
                    break;
                case "com-":
                    if (fparam.Argument(1) == null)
                    { Coms.Clear(); Echo("Очищены все команды"); }
                    else if (Coms.ContainsKey(fparam.Argument(1)))
                    {
                        Coms.Remove(fparam.Argument(1));
                        Echo("Удалена реакция на команду " + fparam.Argument(1));
                    }
                    break;
                case "com?":
                    foreach (var x in Coms) Echo($"{x.Key} [{(x.Value.brod ? "B" : "U")}] {x.Value.command}");
                    break;
                case "send_to":
                    {
                        if (!fparam.Argument(1).AsLong(x => SendMessage(fparam.Argument(2), x)))
                        Echo("Неверный формат числа");
                    }
                    break;
                case "save_ini":
                    {
                        IMyTerminalBlock bl;
                        if (fparam.ArgumentCount == 1) bl = Me;
                        else bl = GridTerminalSystem.GetBlockWithName(fparam.Argument(1)) as IMyTerminalBlock;
                        if (bl == null) { Echo("Не наден блок: " + fparam.Argument(1));return;}
                        bl.CustomData = Save_();
                    }
                    break;
                default:
                    Echo("Uncown command: " + fparam.Argument(0));
                    break;
            }
        }

        if ((tpu & UpdateType.IGC) > 0) GetMessage();

        if ((tpu & UpdateType.Update10) > 0)
        {
            tic += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (tic < 300) return;
            tic = 0;
            if (NexMes != null)
                { SendMessage(NexMes.Value.As<string>(), NexMes.Value.Source); NexMes = null; return; }
            Runtime.UpdateFrequency = UpdateFrequency.None;
            Ant.Enabled = false;
        }
    }
    catch (Exception e)
    {
        var s = e.ToString();
        Echo(s);
        Me.CustomData += "\n" + s;
    }
}

void SendMessage(string mes, long address = 0)
{
    if (Ant != null && !Ant.Enabled)
    {
        Ant.Enabled = true;
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        NexMes = new MyIGCMessage(mes, tag_id, address);
        return;
    }
    if (address == 0) IGC.SendBroadcastMessage(tag_id, mes);
    else if (!IGC.IsEndpointReachable(address))
    { SetTxt("! Не достижима: " + address); return; }
    else IGC.SendUnicastMessage(address, tag_id, mes);
    SetTxt((address == 0 ? ">B:" : ">U:") + mes);
}

void GetMessage()
{
    while (MesBProv.HasPendingMessage)
    {
        var mes_ = MesBProv.AcceptMessage();
        var mes = mes_.As<string>();
        SetTxt($"<B:{mes}:{mes_.Source}/{mes_.Tag}\t;");
        checkCommand(mes, true, mes_.Source);
    }

    while (MesUProv.HasPendingMessage)
    {
        var mes_ = MesUProv.AcceptMessage();
        var mes = mes_.As<string>();
        SetTxt($"<U:{mes}/{mes_.Source}/{mes_.Tag}\t;");
        checkCommand(mes, false, mes_.Source);
    }
}
void checkCommand(string val, bool brod, long adress)
{
    if (string.IsNullOrWhiteSpace(val)) return;
    com_ tmp;
    if (!Coms.TryGetValue(val, out tmp) || tmp.brod != brod) return;

    var i = tmp.command.IndexOf(' ');
    if(i<0) Go(tmp.command, null, adress);
    else    Go(tmp.command.Substring(0, i), tmp.command.Substring(Math.Min(i + 1, tmp.command.Length)), adress);
}

bool Go(string com, string mes, long adress)
{
    switch (com.ToLower())
    {
        case "send_pos":
            SendMessage(MyGPS.GPS(Me.CubeGrid.CustomName, Me.GetPosition()), adress);
            break;
        case "send":
            SendMessage(mes, 0);
            break;
        case "send_to":
            SendMessage(mes, adress);
            break;
        case "antena_on":
            { Ant.Enabled = true; SetTxt("Антена вкл"); }
            break;
        case "antena_off":
            { Ant.Enabled = false; SetTxt("Антена вкл"); }
            break;
        default: Echo("Uncown action: " + com); return false;
    }
    return true;
}

void SetTxt(string mes)
{
    if (txt == null) return;

    var txs = txt.Surface();
    StringBuilder nw = new StringBuilder();
    txs.ReadText(nw, false);
    nw.Append(mes);

    mes += "\n";

    var sz = txs.MeasureStringInPixels(nw, txs.Font, txs.FontSize);
    if (sz.Y <= txs.SurfaceSize.Y * (1f - txs.TextPadding / 100))
        txs.WriteText(mes, true);
    else txs.WriteText(mes);

    //Me.CustomData += $"\n{sz} - {txs.SurfaceSize * (1f - txs.TextPadding / 100)}, {txs.FontSize}\n{nw}";
}

Program()
{
    try
    {
        MesBProv = IGC.RegisterBroadcastListener(tag_id);
        MesBProv.SetMessageCallback(tag_id);
        MesUProv = IGC.UnicastListener;
        MesUProv.SetMessageCallback(tag_id);

        string s = null;
        if (Storage.StartsWith("Antena\n") && Me.TerminalRunArgument != "null")
            s = Storage.Substring(7);
        else if (!string.IsNullOrWhiteSpace(Me.CustomData))
            s = Me.CustomData;
        else if (string.IsNullOrWhiteSpace(Me.CustomData))
            Me.CustomData += "[LCD]\nName=\nIndex=0\n\n[Antena]\nName=\n" +
                "\n[Comands]\n" +
                "//send_pos, send, send_to, antena_on, antena_off:\nsend_to=\nantena_on=\nantena_off=\n---\n\n" +
@"send - Отправить BC:Текст
send_to - Отправить UC:Адресат:Текст
#send_pos - Отправить BC свою позицию
com+/com- - Добавить / удалить команду";
        if (s != null)
        {
            var _ini = new MyIni();
            {
                MyIniParseResult result;
                if (!_ini.TryParse(s, out result))
                    throw new Exception(result.ToString());
            }

            s = _ini.Get("LCD", "Name").ToString();
            var v = _ini.Get("LCD", "Index").ToInt16();
            var sp = (GridTerminalSystem.GetBlockWithName(s) as IMyTextSurfaceProvider);
            if (sp != null) txt = Text.New(sp, v);
            if (txt == null) Echo($"{s} не удалось получить панель {v}");

            s = _ini.Get("Antena", "Name").ToString();
            Ant = GridTerminalSystem.GetBlockWithName(s) as IMyRadioAntenna;

            if (_ini.ContainsSection("Comands"))
            {
                var lst = new List<MyIniKey>();
                _ini.GetKeys("Comands", lst);
                lst.ForEach(x => {
                    var str = _ini.Get(x).ToString();
                    if (string.IsNullOrWhiteSpace(str))
                        return;
                    else if (str.StartsWith("*"))
                        Coms.Add(x.Name, new com_(str.Substring(1), true));
                    else
                        Coms.Add(x.Name, new com_(str, false));
                });
            }
        }

        if (txt == null) txt = Text.New(Me, 0);
        if (txt == null) Echo("Что? Нет панели в програмном блоке!");
        else
        {
            txt.Surface().ContentType = ContentType.TEXT_AND_IMAGE;
            txt.Surface().WriteText(string.Empty);
        }
        Echo("Инициализация завершена\n" + (txt?.Surface().DisplayName ?? ""));
        Echo(Ant?.CustomName ?? "");
    }
    catch (Exception e)
    {
        var s = e.ToString();
        Echo(s);
        Me.CustomData += "\n" + s;
    }
}

void Save() { Storage = $"Antena\n" + Save_(); }
string Save_()
{
    var _ini = new MyIni();
    _ini.AddSection("LCD");
    _ini.Set("LCD", "Name", (txt?.owner as IMyTerminalBlock)?.CustomName ?? "");
    _ini.Set("LCD", "Index", txt?.ind ?? 0);

    _ini.AddSection("Antena");
    _ini.Set("Antena", "Name", (Ant as IMyTerminalBlock)?.CustomName ?? "");

    if (Coms.Count > 0)
    {
        _ini.AddSection("Comands");
        foreach (var x in Coms) //Echo($"{x.Key}:{x.Value.command} - {x.Value.brod}");
            _ini.Set("Comands", x.Key, (x.Value.brod ? "*" : "") + x.Value.command);
    }
    return _ini.ToString();
}

class Text
{
    public readonly IMyTextSurfaceProvider owner;
    public readonly int ind;

    private Text(IMyTextSurfaceProvider own, int index) { owner = own; ind = index; }
    public IMyTextSurface Surface() => owner.GetSurface(ind);
    public static Text New(IMyTextSurfaceProvider own, int index)
    => (index < 0 || own == null || own.SurfaceCount < index - 1) ? null : new Text(own, index);
}

public class com_
{
    public string command = null;
    public bool brod = false;
    public com_() { }
    public com_(string commandRun, bool brodcust)
    { command = commandRun; brod = brodcust; }
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
    public static string TryParse(string vector, out Vector3D res)
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
        return res == Vector3D.Zero ? string.Empty : p[1].Trim();
    }
    public static bool TryParseVers(string vector, out Vector3D res)
    {
        if (vector.StartsWith("{")) return Vector3D.TryParse(vector, out res);
        else return !string.IsNullOrEmpty(TryParse(vector, out res));
    }
}

}
// This template is intended for extension classes. For most purposes you're going to want a normal
// utility class.
// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
static class Class1
{
    static public MyCommandLine COmLine(this string val)
    { var res = new MyCommandLine(); return res.TryParse(val) ? res : null; }

    static public long AsLong(this string val, long def = 0 )
    { long res; return long.TryParse(val, out res) ? res : def; }

    public delegate void ParserDelegate(long val);
    static public bool AsLong(this string val, ParserDelegate Action = null)
    { long res; var f = long.TryParse(val, out res);
     if(f && Action != null) Action(res); return f; }