/**--------------------------------------------------------------------------       
*   AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: andrey.ck.ua@gmail.com        
*   When using and disseminating information about the authorship is obligatory
*   При использовании и распространении информация об авторстве обязательна       
*----------------------------------------------------------------------*/

//Const
//Коэффициент Kv, характеризующий пропорциональную зависимость между разностью требуемой и текущей высот и необходимой вертикальной скоростью
readonly double Kv = 0.5;
//Коэффициент Ka, характеризующий пропорциональную зависимость между разностью требуемой и текущей верт. скоростей и желаемым ускорением
readonly double Ka = 2.5;
readonly MyPlanetElevation LevelHeight = MyPlanetElevation.Surface; // Sealevel or Surface

readonly IMyShipController Controller;
readonly List<IMyThrust> ThrusterList;
readonly List<IMyGyro> GyroList;
readonly IMyTextSurface LCD;
//Radar myRadar;
//static IMyGridTerminalSystem gts;

//переменные для поддержания высоты полета над поверхностью
double HoverHeight = 0;
double CurrentHeight = 0;
double ForwardSpeed = 0;
double ForwardVelocityOld = 0;
double Faccel = 0;
bool OnlyUp = false;

readonly Matrix LocM;
Matrix GyroM;

public Program()
{
    Controller = new Selection(Me.CustomData).FindBlock<IMyShipController>(GridTerminalSystem);
    if (Controller != null)
    {
        if (Controller is IMyTextSurfaceProvider)LCD = (Controller as IMyTextSurfaceProvider).GetSurface(0);
        else
        {
            var tmp = new Selection(null).FindBlock<IMyTextSurfaceProvider>(GridTerminalSystem);
            if (tmp == null) Echo("Не найден блок для вывода информации");
            else
            {
                Echo("LCD: " + (tmp as IMyTerminalBlock).CustomName);
                LCD = tmp.GetSurface(0);
            }
        }
        if(LCD!=null) LCD.ContentType = ContentType.TEXT_AND_IMAGE;
        Controller.Orientation.GetMatrix(out LocM);
        LocM = Matrix.Transpose(LocM);
    }
    ThrusterList = new List<IMyThrust>();
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(ThrusterList);
    GyroList = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(GyroList);
    //gts = GridTerminalSystem;
    //myRadar = new Radar();

    if (Me.TerminalRunArgument != "null" && !String.IsNullOrEmpty(Storage) && Storage.StartsWith("Hover"))
        Restore(Storage.Split('\n'));
    else
        Controller.TryGetPlanetElevation(LevelHeight, out HoverHeight);
    Echo($"Controller: {Controller.CustomName}\nТрастеры:{ThrusterList.Count} Гироскопы:{GyroList.Count}");
}

public void Main(string argument, UpdateType uType)
{
    if (uType == UpdateType.Update1)
    {
        KeepHorizon();
        /*myRadar.Update(HoverHeight);
                for (int cnt = 0; cnt < myRadar.DepthMap.Length; cnt++)
                {
                    Echo("" + myRadar.DepthMap[cnt]);
                }*/
    }
    else
        switch (argument.ToLower())
        {
            case "on":
                {
                    //Controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out HoverHeight);
                    Controller.DampenersOverride = false;
                    GyroOver(true);
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                    KeepHorizon();
                }break;
            case "off":
                {
                    GyroOver(false);
                    SetThrust(0f);
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    Controller.DampenersOverride = true;
                }
                break;
            case "on/off":
                Main((Runtime.UpdateFrequency == UpdateFrequency.None) ?"on":"off", UpdateType.None);
                break;
            case "stop":
                ForwardSpeed = 0;
                break;
            case "inline":
                OnlyUp = !OnlyUp;
                break;
        }
}


public void KeepHorizon()
{
    HoverHeight += Controller.MoveIndicator.Y / 10;
    float ShipMass = Controller.CalculateShipMass().PhysicalMass;

    Vector3D GravityVector = Controller.GetNaturalGravity();
    Vector3D GravNorm = Vector3D.Normalize(GravityVector);
    Vector3D ForwardVector = Vector3D.Normalize(Vector3D.Reject(Controller.WorldMatrix.Forward, GravNorm));
    Vector3D VelocityCompensator = Vector3D.Reject(Controller.GetShipVelocities().LinearVelocity, ForwardVector);
    if (VelocityCompensator.Length() > 10) VelocityCompensator = Vector3D.Normalize(VelocityCompensator) * 10;
    Vector3D StopVector = VelocityCompensator / 10;


    //float ForwardInput = Controller.MoveIndicator.Z;
    ForwardSpeed += Controller.MoveIndicator.Z * 0.1;
    double ForwardVelocity = -Controller.GetShipVelocities().LinearVelocity.Dot(ForwardVector);
    Faccel = ForwardVelocity - ForwardVelocityOld;
    ForwardVelocityOld = ForwardVelocity;

    double ForwardSpeedFactor = ((ForwardSpeed - ForwardVelocity) * 0.1 - Faccel) * 0.5;

    Controller.TryGetPlanetElevation(LevelHeight, out CurrentHeight);
    /*if (CurrentHeight > myRadar.CritDepth.Y)
            {
                CurrentHeight = myRadar.CritDepth.Y;
            }*/
    double HeightDelta = HoverHeight - CurrentHeight;
    double VerticalSpeed = -Controller.GetShipVelocities().LinearVelocity.Dot(GravNorm);
    if (OnlyUp && HeightDelta < 0) HeightDelta = 0;
    else if(HeightDelta < 0) HeightDelta /= 2;
    else if (HeightDelta > (HoverHeight / 5))
    {
        //ForwardSpeedFactor = ((Math.Min((myRadar.CritDepth.X / 10), ForwardSpeed) - ForwardVelocity) * 0.1 - Faccel) * 0.5;
        ForwardSpeedFactor = (((HoverHeight - HeightDelta) * ForwardVelocity / HoverHeight - ForwardVelocity) * 0.1 - Faccel) * 0.5;
    }


    Vector3D ForwardPart = ForwardVector * ForwardSpeedFactor;
    float YawInput = Controller.MoveIndicator.X;
    StopVector += Controller.WorldMatrix.Left * Controller.RollIndicator * 1.0f;
    StopVector += ForwardPart * 1.2f;
    if (StopVector.Length() > 1) StopVector = Vector3D.Normalize(StopVector);
    StopVector *= 0.5f;
    StopVector += GravNorm;

    float RollInput = (float)StopVector.Dot(Controller.WorldMatrix.Left);
    float PitchInput = (float)StopVector.Dot(Controller.WorldMatrix.Forward);

    SetGyro(new Vector3D(PitchInput, YawInput, RollInput));

    double HoverCorrection = (Math.Max(Math.Min(HeightDelta * Kv, 20), -20) - VerticalSpeed) * Ka;

    float MyThrust = (float)(GravityVector.Length() * ShipMass * (1 + HoverCorrection) / GravNorm.Dot(Controller.WorldMatrix.Down));
    if (MyThrust <= 0)
        MyThrust = 1;
    SetThrust(MyThrust);
    if (LCD != null)
    {
        LCD.WriteText($"Скорость: {-ForwardSpeed:0.00}\nВысота: {CurrentHeight:0.00} / {HoverHeight:0.00}", false);
        if (OnlyUp) LCD.WriteText("\nНе снижаться", true);
    }
}

public void SetGyro(Vector3D rot)
{
    rot = Vector3D.Transform(rot, LocM);
    foreach (IMyGyro gyro in GyroList)
    {
        gyro.Orientation.GetMatrix(out GyroM);
        Vector3D LocRot = Vector3D.Transform(rot, GyroM);
        gyro.Yaw = (float)LocRot.Y;
        gyro.Roll = (float)LocRot.Z;
        gyro.Pitch = -(float)LocRot.X;
    }
}

public void GyroOver(bool over)
{
    foreach (IMyGyro gyro in GyroList)
    {
        gyro.GyroOverride = over;
    }
}

public void SetThrust(float Thr)
{
    foreach (IMyThrust thruster in ThrusterList)
    {
        thruster.ThrustOverride = Thr / ThrusterList.Count;
    }
}

void Restore(string[] Lines)
{
    if (bool.Parse(Lines[1]))
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    HoverHeight = double.Parse(Lines[2]);
    CurrentHeight = double.Parse(Lines[3]);
    ForwardSpeed = double.Parse(Lines[4]);
    ForwardVelocityOld = double.Parse(Lines[5]);
    Faccel = double.Parse(Lines[6]);
}

void Save()
{
    var s = new StringBuilder("Hover\n");
    s.AppendLine((Runtime.UpdateFrequency == UpdateFrequency.Update1).ToString());
    s.AppendLine(HoverHeight.ToString());
    s.AppendLine(CurrentHeight.ToString());
    s.AppendLine(ForwardSpeed.ToString());
    s.AppendLine(ForwardVelocityOld.ToString());
    s.AppendLine(Faccel.ToString());
    Storage = s.ToString();
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