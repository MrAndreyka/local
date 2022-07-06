/*----------------------------------------------------------------------
         AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com
         When using and disseminating information about the authorship is obligatory
         При использовании и распространении информация об авторстве обязательна
         ----------------------------------------------------------------------*/
		Program()
		{
			Init();
		}

		public class PID : List<IMyGyro>
		{
			[Flags] public enum DIR { None, NoRules, NoGrav, IgnorTarget = 4, Forw = 8, Back = 16, Up = 32 };
			public DIR Dir = DIR.None;
			public BoundingBoxD Target { get; protected set; }
			public bool IsTarget() => !Vector3D.IsZero(Target.Max);
			public Vector3D Center { get { return Target.Center; } }
			public BoundingSphereD SphereD() => BoundingSphereD.CreateFromBoundingBox(Target);
			public double Radius { get; protected set; }
			public Vector3D PrevDirectVec = Vector3D.Zero;

			int coof = 0;
			public int KeyWait = 0;
			public int Coof { get { return coof; } }
			public double Distance()=> Target.Distance(RemCon.GetPosition());
			
			public int CalculatePower() { return coof = this.Sum(x => !x.Enabled?0:x.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 33600 : 448); }
			public void SetTarget(BoundingBoxD val) { Target = val; Radius = Target.Size.Length() / 2; }
			public void SetTarget(Vector3D val, double dist = 0)
			{
				if (Vector3D.IsZero(val)) { Target = new BoundingBoxD(val, val); Radius = 0; }
				else SetTarget(BoundingBoxD.CreateFromSphere(new BoundingSphereD(val, Math.Max(dist, 0.01))));
			}


			/*public Vector3 Rules()//(DIR Dir = DIR.None)
			{
				//if (Dir == DIR.None) Dir = this.Dir;
				Vector3 Vdr = Vector3D.Zero;
				var isGr = !Vector3D.IsZero(VGr);
				var istrg = IsTarget() && (Dir & DIR.IgnorTarget) != DIR.IgnorTarget;
				if (!isGr && !istrg) return Vdr;

				if ((Dir & DIR.Forw) == DIR.Forw)
				{
					Vector3D vNap = istrg ? Center - RemCon.GetPosition() : -VGr;
					if (isGr && (Dir & DIR.NoGrav) != DIR.NoGrav) vNap = Vector3D.Reject(vNap, Vector3D.Normalize(VGr));

					Vdr.X = GetAngel(RemCon.WorldMatrix.Down, RemCon.WorldMatrix.Forward, vNap);
					Vdr.Y = GetAngel(RemCon.WorldMatrix.Right, RemCon.WorldMatrix.Forward, vNap);
					if (Math.Abs(Vdr.Y) > MathHelper.PiOver2) Vdr.Y -= Vdr.Y > 0 ? MathHelper.Pi : -MathHelper.Pi;
					if (isGr && istrg) Vdr.Z = GetAngel(RemCon.WorldMatrix.Forward, RemCon.WorldMatrix.Down, VGr);
				}
				else if ((Dir & DIR.Up) == DIR.Up)
				{
					Vector3D vNap = istrg ? Center - RemCon.GetPosition() : -VGr;

					Vdr.Z = GetAngel(RemCon.WorldMatrix.Forward, RemCon.WorldMatrix.Down, VGr);
					Vdr.Y = GetAngel(RemCon.WorldMatrix.Right, RemCon.WorldMatrix.Up, vNap);
				}

				if ((Dir & DIR.NoRules) != DIR.NoRules)
				{
					var abs = Vector3D.Abs(Vdr);
					if (KeyWait != 0 && abs.X < 0.35 && abs.Y < 0.35 && abs.Z < 0.35)
					{
						if (KeyWait != int.MaxValue) CP = KeyWait;
						//else if (Scan(Center) > 0) CP = 9;
						else CP = 8;
						KeyWait = 0;
					}

					if (abs.X < 0.004f) Vdr.X = 0;
					if (abs.Y < 0.004f) Vdr.Y = 0;
					if (abs.Z < 0.004f) Vdr.Z = 0;

					if (Vdr.X == 0 && Vdr.Y == 0 && Vdr.Z == 0)
					{
						ForEach(x => x.GyroOverride = false);
						PrevDirectVec = Vector3D.Zero;
						return Vdr;
					}
					Drive(Vdr.X, Vdr.Y, Vdr.Z, RemCon.WorldMatrix);
				}
				return Vdr;
			}
			*/
			static double CorrectAngel(double new_val, double old_val) => (old_val == new_val ? 0.0d : new_val / (old_val - new_val) * old_val);
			public void Drive(double yaw_speed, double pitch_speed, double roll_speed, MatrixD shipMatrix) => Drive(new Vector3D(-pitch_speed, yaw_speed, roll_speed), shipMatrix);
			public void Drive(Vector3D Pows, MatrixD shipMatrix)
			{
				if (Count == 0) return;
				var relativeRotationVec = Vector3D.TransformNormal(Pows, shipMatrix);
				float cof = (130 - coof) / 100;
				foreach (var thisGyro in this)
				{
					var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
					thisGyro.GyroOverride = true;
					thisGyro.Pitch = (float)transformedRotationVec.X;
					thisGyro.Yaw = (float)transformedRotationVec.Y;
					thisGyro.Roll = (float)transformedRotationVec.Z;
					/*thisGyro.Pitch = (float)transformedRotationVec.X - (transformedRotationVec.X > 0.5 ? thisGyro.Pitch / 3 : 0);
					thisGyro.Yaw = (float)transformedRotationVec.Y - (transformedRotationVec.Y > 0.5 ? thisGyro.Yaw / 3 : 0);
					thisGyro.Roll = (float)transformedRotationVec.Z - (transformedRotationVec.Z > 0.5 ? thisGyro.Roll / 3 : 0);*/
				}
			}

			/// <summary>
			///  Расчитывает угол поворота
			/// </summary>
			/// <param name="Pl">Плоскость</param>
			/// <param name="VDirect">Вектор поворота</param>
			/// <param name="Targ">Вектор цели</param>
			public static float GetAngel(Vector3D Pl, Vector3D VDirect, Vector3D Targ)
			{
				var tm = Vector3D.Reject(Targ, Pl);
				var u = Math.Acos(VDirect.Dot(tm) / (VDirect.Length() * tm.Length()));
				return (float)(MyMath.AngleBetween(tm, Pl.Cross(VDirect)) > MathHelper.PiOver2 ? -u : u);
			}

			public string ToSave() => string.Format("{0},{1},{2}", KeyWait, Dir, string.Join(";", Target.GetCorners()));
			public void Load(string val)
			{
				var vl = val.Split(',');
				KeyWait = int.Parse(vl[0]);
				Rul.Dir = (PID.DIR)Enum.Parse(typeof(PID.DIR), vl[1]);
				vl = vl[2].Split(';');
				var lst = new List<Vector3D>(vl.Length);
				for (var i = 0; i < vl.Length; i++)
				{ Vector3D tm; Vector3D.TryParse(vl[i], out tm); lst.Add(tm); }
				SetTarget(BoundingBoxD.CreateFromPoints(lst));
			}

		}

		static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
		IMyTextSurface TP;

		static IMyShipController RemCon = null;

		static readonly PID Rul = new PID();

		static double Mass = 0;
		static Vector3D speed = Vector3D.Zero;

		static bool aded = false;
		StringBuilder sb = new StringBuilder();

		// ---------------------------------- MAIN ---------------------------------------------
		void Main(string argument, UpdateType tpu)
		{
			try
			{	
				sb.AppendFormat(SYS, "\n{0}	Out: {1}", argument, Runtime.TimeSinceLastRun.TotalMilliseconds);
				if (tpu < UpdateType.Update1 && !string.IsNullOrEmpty(argument))
				{ Echo_("", false); if(!SetAtributes(argument)) return; }

				if (RemCon == null) { Echo("Необходима инициализация"); return; }

				Vector3D temp;
				Mass = RemCon.CalculateShipMass().TotalMass;
				
				sb.AppendFormat(SYS, "\nМасса: {2} \nГироскопы: {0}	Кооф: {1:0.00}", Rul.Coof, Mass / Rul.Coof, Mass);

				temp = RemCon.GetShipVelocities().LinearVelocity;
				GetVec_S(temp, out speed);
				sb.AppendFormat(SYS, "\nСкор_: {0}\nСкор: {1:0.00}g.", temp.ToString("0.#"), speed.ToString("0.#"));

				temp = RemCon.GetShipVelocities().AngularVelocity;
				GetVec_S(temp, out speed);
				sb.AppendFormat(SYS, "\nНаклоны_: {0}\nНаклоны: {1}g.", temp.ToString("0.00"), speed.ToString("0.00"));

				Echo_(sb.ToString(), false);
				if (aded) RemCon.CustomData += "\n\n" + sb.ToString();
				sb.Clear();
			}
			catch (Exception e) 
			{ 
				Me.CustomData += e.ToString();
				Echo_("ОШИБКА"); 
			}
		}
		// ---------------------------------- end MAIN ---------------------------------------------

        public bool SetAtributes(string Arg)
        {
            try
            {
                int pos = Arg.IndexOf(':');
                if (pos < 0) pos = Arg.Length;
                string Right;
                if (Arg.Length != pos)
                {
                    Right = Arg.Substring(pos + 1);
                    Arg = Arg.Remove(pos);
                }
                else
                    Right = "";
                Arg = Arg.ToLower();
                switch (Arg)
                {
                    case "hor":
                        {
                            if (!int.TryParse(Right, out pos)) return false;
                            Rul.Drive(MathHelper.ToRadians(pos), 0, 0, RemCon.WorldMatrix);
							Rul.ForEach(x => x.GyroOverride = true);
							return true;
                        }
                    case "ver":
                        {
                            if (!int.TryParse(Right, out pos)) return false;
                            Rul.Drive(0, MathHelper.ToRadians(pos), 0, RemCon.WorldMatrix);
                            Rul.ForEach(x => x.GyroOverride = true);
                            return true;
                        }
                    case "rou":
                        {
                            if (!int.TryParse(Right, out pos)) return false;
                            Rul.Drive(0, 0, MathHelper.ToRadians(pos), RemCon.WorldMatrix);
                            Rul.ForEach(x => x.GyroOverride = true);
                            return true;
                        }
                    case "stop":
						{ 
                            Rul.Drive(0, 0, 0, RemCon.WorldMatrix);
                            Rul.ForEach(x => x.GyroOverride = false);
                            return true;
                        }
                    case "targ":// Включение
                        {
                            Vector3D v;
                            if (!MyGPS.TryParseVers(Right, out v))
                                ShowAndStop("Не верный формат цели");
                            Rul.SetTarget(v);
                        }
                        break;
                    case "show":// вывод инфы и автопилот
                        {
                            if (RemCon == null /*|| !Rul.IsTarget()*/) return false; //Не инициализирован
                                                                                     //RemCon.DampenersOverride = true;
                            Runtime.UpdateFrequency = UpdateFrequency.Update100;
                            Rul.Dir = PID.DIR.Forw | PID.DIR.NoRules;
                            //Rul.ForEach(x => x.GyroOverride = false);
                        }
                        break;
                    case "-":
                    case "off": // Отключение
                        Stoped();
                        break;
                    case "hist": { if (TP != null && (aded = !aded)) RemCon.CustomData = string.Empty; } break;
                    case "?":
                        Echo_(">>>>>>>>>>>>");

                        if (Rul.IsTarget()) Echo_(MyGPS.GPS("Target " + Rul.Radius, Rul.Center));
                        ShowAndStop("Инфа выведена");
                        break;
                    case "test":
                        {
                            Rul.ForEach(x => x.GyroOverride = false);
                            SetAtributes("show"); Rul.Dir = PID.DIR.NoRules | PID.DIR.Up;
                        }
                        break;
                    case "init": Init(); break;
                    default:
                        {
                            ShowAndStop("Левая команда: " + Arg); break;
                        }
                }

            }
            catch (Exception e) { Echo_(e.ToString()); }
            return false;
        }

        public static void GetVec_S(Vector3D sp, out Vector3D ouv)
		{
			var rd = Vector3D.Reflect(sp, RemCon.WorldMatrix.Down);
			ouv.X = Vector3D.Reflect(rd, RemCon.WorldMatrix.Left).Dot(RemCon.WorldMatrix.Forward);
			ouv.Y = Vector3D.Reflect(rd, RemCon.WorldMatrix.Forward).Dot(RemCon.WorldMatrix.Right);
			ouv.Z = Vector3D.Reflect(Vector3D.Reflect(sp, RemCon.WorldMatrix.Left), RemCon.WorldMatrix.Forward).Dot(RemCon.WorldMatrix.Up);
		}

		public void ShowAndStop(string text) { if (Runtime.UpdateFrequency != UpdateFrequency.None) Stoped(); Echo_(text); }
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
				if (p.GetLength(0) == 7)
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

		void Init()
		{
			RemCon = Sel("").FindBlock<IMyShipController>(GridTerminalSystem);
			if (RemCon == null) { Echo("Блок управления не найден"); return; }
			
			GridTerminalSystem.GetBlocksOfType<IMyGyro>(Rul);

			if ((RemCon as IMyTextSurfaceProvider).SurfaceCount > 0)
			{
				TP = (RemCon as IMyTextSurfaceProvider).GetSurface(0);
				TP.ContentType = ContentType.TEXT_AND_IMAGE;
				TP.WriteText("Инициализация завершена");
			}

			Echo("Найдено " + Rul.Count + " гироскопов / " + Rul.CalculatePower());
			Echo("Инициализация завершена");
		}

		void Stoped()
		{
			Runtime.UpdateFrequency = UpdateFrequency.None;
			Rul.Dir = PID.DIR.None;
			Rul.ForEach(x => x.GyroOverride = false);
		}

		void Echo_(string text, bool append = true) { if (TP == null) Echo(text); else TP.WriteText(text + "\n", append); }

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

		public static Selection Sel(string val, IMyCubeGrid grid = null) => new Selection(val, grid);
		