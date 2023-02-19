class Text
        {
            public readonly IMyTextSurfaceProvider owner;
            public readonly int ind;

            private Text(IMyTextSurfaceProvider own, int index) { owner = own; ind = index; }
            public IMyTextSurface Surface() => owner.GetSurface(ind);
            public static Text New(IMyTextSurfaceProvider own, int index)
            => (index < 0 || own == null || own.SurfaceCount < index - 1) ? null : new Text(own, index);
        }

        const string tag_id = "A_Mes";
        string NexMes = null;
        readonly IMyMessageProvider MesProv;
        readonly Text txt = null;
        readonly IMyRadioAntenna Ant=null;

        Program()
        {
            
            try
            {
                MesProv = IGC.RegisterBroadcastListener(tag_id);
                MesProv.SetMessageCallback(tag_id);

                string s = null;
                if (Storage.StartsWith("Antena\n") && Me.TerminalRunArgument != "null")
                    s = Storage.Substring(7);
                else if (!string.IsNullOrWhiteSpace(Me.CustomData))
                    s = Me.CustomData;
                else if (string.IsNullOrWhiteSpace(Me.CustomData))
                    Me.CustomData += "[LCD]\nName=\nIndex=0\n[Antena]\nName=";

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
                }

                if (txt == null) txt = Text.New(Me, 0);
                if (txt == null) Echo("Что? Нет панели в програмном блоке!");
                else txt.Surface().ContentType = ContentType.TEXT_AND_IMAGE;
                Echo("Инициализация завершена " + (txt?.Surface().Name ?? ""));
                Echo(Ant?.CustomName ?? "");
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }

            if (!string.IsNullOrWhiteSpace(Me.CustomData) && !Me.CustomData.EndsWith("---\n"))
                Me.CustomData += "\n---\n";
        }

        void Save()
        {
            var _ini = new MyIni();
            _ini.AddSection("LCD");
            _ini.Set("LCD", "Name", (txt?.owner as IMyTerminalBlock)?.CustomName ?? "");
            _ini.Set("LCD", "Index", txt?.ind ?? 0);
            
            _ini.AddSection("Antena");
            _ini.Set("Antena", "Name",  (Ant as IMyTerminalBlock)?.CustomName ?? "");

            Storage = $"Antena\n"+ _ini.ToString();
        }

        // ---------------------------------- MAIN ---------------------------------------------
        void Main(string argument, UpdateType tpu)
        {
            try
            {
                switch (tpu)
                {
                    case UpdateType.IGC:
                        GetMessage();
                        break;
                    case UpdateType.Once:
                        SentMessage(null);
                        break;
                    default:
                        {
                            var prs = argument.Split(':');
                            if (prs[0] == "#send_pos")
                                SentMessage(MyGPS.GPS(Me.CubeGrid.CustomName, Me.GetPosition()));
                            else if (argument.StartsWith("send:"))
                                SentMessage(prs.Length> 1? prs[1]: "");
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }
        }

        void GetMessage()
        {
            var mes_ = MesProv.AcceptMessage();
            string mes = $"{mes_.Tag}:  {mes_.As<string>()} << {mes_.Source}";
            SetTxt("<<" + mes);
        }

        void SentMessage(string mes)
        {
            if (Ant != null && !Ant.Enabled) 
            {
                Ant.Enabled = true;
                Runtime.UpdateFrequency = UpdateFrequency.Once;
                NexMes = mes;
                return;
            }
            IGC.SendBroadcastMessage(tag_id, mes ?? NexMes, TransmissionDistance.AntennaRelay);
            if (mes == null) Ant.Enabled = true;
            SetTxt(">>" + mes);
        }

        void SetTxt(string mes)
        {
            Me.CustomData += mes;
            if (txt == null) return;

            var txs = txt.Surface();

            StringBuilder nw = new StringBuilder();
            txs.ReadText(nw, true);

            var sz = txs.MeasureStringInPixels(nw, txs.Font, txs.FontSize);
            if (sz.Y <= txs.SurfaceSize.Y + txs.TextPadding) txs.WriteText("\n" + mes, true);
            else txs.WriteText(mes);
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