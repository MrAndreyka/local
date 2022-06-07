/**--------------------------------------------------------------------------       
*   AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com        
*   When using and disseminating information about the authorship is obligatory
*   При использовании и распространении информация об авторстве обязательна       
*----------------------------------------------------------------------*/


readonly IMyCameraBlock Camera;
readonly IMySensorBlock Sensor;
readonly IMyTextSurface LCD;


public Program()
{
    Camera = new Selection(Me.CustomData).FindBlock<IMyCameraBlock>(GridTerminalSystem);
    Camera.EnableRaycast = true;
    var cp = new Selection(null).FindBlock<IMyCockpit>(GridTerminalSystem);
    LCD = (cp as IMyTextSurfaceProvider ?? Me as IMyTextSurfaceProvider).GetSurface(0);
    LCD.ContentType = ContentType.TEXT_AND_IMAGE;
    Sensor = new Selection(Me.CustomData).FindBlock<IMySensorBlock>(GridTerminalSystem);

}

public void Main(string argument, UpdateType uType)
{
    LCD.WriteText(MyGPS.GPS("VPos", Camera.WorldMatrix.Forward, Camera.GetPosition(), 20)+"\n");

    MyDetectedEntityInfo res;
    if (Sensor != null)
    {
        res = Sensor.LastDetectedEntity;
        List<MyDetectedEntityInfo> le = new List<MyDetectedEntityInfo>();
        Sensor.DetectedEntities(le);
        if (res.IsEmpty()) LCD.WriteText("Нет цели сенсора" + le.Count, true);
        else
        {
            LCD.WriteText("\nСенсор", true);
            LCD.WriteText($"\nEntityId {res.EntityId}\nName {res.Name}\nType {res.Type}\nVelocity {res.Velocity}\nRelationship {res.Relationship} \nTimeStamp {res.TimeStamp}", true);
            LCD.WriteText(MyGPS.GPS("\nTarget", res.Position), true);
        }
    }

    res = Camera.Raycast(500);
    if (res.IsEmpty()) { LCD.WriteText("\nНет цели: " + Camera.AvailableScanRange, true); return; }
    else if (res.Type == MyDetectedEntityType.Planet) { LCD.WriteText("\nПланета: " + res.Name, true); return; }

    LCD.WriteText($"\nEntityId {res.EntityId}\nName {res.Name}\nType {res.Type}\nVelocity {res.Velocity}\nRelationship {res.Relationship} \nTimeStamp {res.TimeStamp}", true);

    LCD.WriteText(MyGPS.GPS("\nTarget", res.Position), true);

    var sb = new Selection(null).FindBlock<IMySoundBlock>(GridTerminalSystem);
    if (sb != null) sb.Play();
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