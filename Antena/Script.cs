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
        MyIGCMessage? NexMes = null;
        readonly IMyMessageProvider MesBProv;
        readonly IMyMessageProvider MesUProv;
        readonly Text txt = null;
        readonly IMyRadioAntenna Ant = null;

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
                    Me.CustomData += "[LCD]\nName=\nIndex=0\n\n[Antena]\nName=\n---\n\n" +
@"send - Отправить BC:Текст
send_to - Отправить UC:Адресат:Текст
#send_pos - Отправить BC свою позицию
auto - Ответить на BC 1 раз:Текст
auto_all-Отвечать на BC постоянно:Текст";

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
                else {
                    txt.Surface().ContentType = ContentType.TEXT_AND_IMAGE;
                    txt.Surface().WriteText(string.Empty);
                }
                Echo("Инициализация завершена " + (txt?.Surface().Name ?? ""));
                Echo(Ant?.CustomName ?? "");
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }
        }

        void Save()
        {
            var _ini = new MyIni();
            _ini.AddSection("LCD");
            _ini.Set("LCD", "Name", (txt?.owner as IMyTerminalBlock)?.CustomName ?? "");
            _ini.Set("LCD", "Index", txt?.ind ?? 0);

            _ini.AddSection("Antena");
            _ini.Set("Antena", "Name", (Ant as IMyTerminalBlock)?.CustomName ?? "");

            Storage = $"Antena\n" + _ini.ToString();
        }

        // ---------------------------------- MAIN ---------------------------------------------
        void Main(string argument, UpdateType tpu)
        {
            try
            {
                if ((tpu & (UpdateType.Trigger | UpdateType.Terminal)) > 0
                || (tpu & (UpdateType.Mod)) > 0
                || (tpu & (UpdateType.Script)) > 0)
                {
                    var fparam = getToNext(ref argument, ":");
                    switch (fparam.ToLower())
                    {
                        case "#send_pos":
                            SentMessage(MyGPS.GPS(Me.CubeGrid.CustomName, Me.GetPosition()));
                            break;
                        case "send":
                            SentMessage(argument);
                            break;
                        case "send_to":
                            {
                                fparam = getToNext(ref argument, ":");
                                long i;
                                if (!long.TryParse(fparam, out i))
                                { Echo("Неверный формат числа"); return; }
                                SentMessage(argument, i);
                            }
                            break;
                        case "auto":
                            NexMes = new MyIGCMessage(argument, "wait", 0);
                            break;
                        case "auto_all":
                            NexMes = new MyIGCMessage(argument, "wait", 1);
                            break;
                        default:
                            Echo("Uncown command: " + fparam);
                            break;
                    }
                }

                if ((tpu & UpdateType.IGC) > 0) GetMessage();

                if ((tpu & UpdateType.Update10) >0 )
                {
                    SentMessage(NexMes.Value.As<string>(), NexMes.Value.Source);
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    NexMes = null;
                    Ant.Enabled = false;
                }
                
            }
            catch (Exception e)
            {
                Echo(e.ToString());
            }
        }

        void GetMessage()
        {
            while (MesBProv.HasPendingMessage)
            {
                var mes_ = MesBProv.AcceptMessage();
                string mes = $"{mes_.As<string>()}:{mes_.Source}/{mes_.Tag}\t;";
                SetTxt("<B:" + mes);
                if (NexMes.HasValue && NexMes.Value.Tag == "wait")
                    if (!IGC.IsEndpointReachable(mes_.Source))
                        SetTxt("!!Не достижима: " + mes_.Source);
                    else
                    {
                        SentMessage(NexMes.Value.As<string>(), mes_.Source);
                        if (NexMes.Value.Source == 0) NexMes = null;
                    }
                     
            }
            while (MesUProv.HasPendingMessage)
            {
                var mes_ = MesUProv.AcceptMessage();
                string mes = $"{mes_.As<string>()}:{mes_.Source}/{mes_.Tag}\t;";
                SetTxt("<U:" + mes);
            }
        }

        void SentMessage(string mes, long address = 0)
        {
            if (Ant != null && !Ant.Enabled)
            {
                Ant.Enabled = true;
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                NexMes = new MyIGCMessage(mes, tag_id, address);
                return;
            }
            if(address == 0) IGC.SendBroadcastMessage(tag_id, mes);
            else IGC.SendUnicastMessage(address, tag_id, mes);
            SetTxt((address == 0? ">B:": ">U:") + mes);
        }

        void SetTxt(string mes)
        {
            if (txt == null) return;

            var txs = txt.Surface();
            StringBuilder nw = new StringBuilder();
            txs.ReadText(nw, false);

            var sz = txs.MeasureStringInPixels(nw, txs.Font, txs.FontSize);
            if (nw.Length > 0 && sz.Y <= txs.SurfaceSize.Y + txs.TextPadding) 
                txs.WriteText("\n" + mes, true);
            else txs.WriteText(mes);
        }

        string getToNext(ref string Str, string val) 
        {
            string fparam = null;
            var ind = Str.IndexOf(val);
            if (ind < 0) { fparam = Str; Str = null; }
            else
            {
                fparam = Str.Substring(0, ind);
                Str = Str.Remove(0, ind + 1);
            }
            return fparam;
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