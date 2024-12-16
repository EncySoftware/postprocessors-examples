namespace DotnetPostprocessing.Post{
    public class CycleState{
        public InpArray<double> Prms;

        public bool Cycle_pocket; 
        
        public bool CycleOn;      // Vklyuchen li czikl

        public bool PolarInterp;     // Polyarnaya interpolyacziya: false-vyklyuchena, true-vklyuchena
        public bool CylindInterp;     // CZilindricheskaya interpolyacziya: false-vyklyuchena, >true-vklyuchena

        public bool IsFirstCycle; // true - esli eto pervyj czikl v operaczii, inache - false

        public string Cyclecompare = "";    // Sravnivaem stroki dlya togo, chtoby znat nuzhno li vyvodit czikl

        public CycleState(){
            Prms = new InpArray<double>();
        }

        public void AddPrm(double value, int i) => Prms[i] = value;
    }

    public class SinumerikCycle{
        // N170 M8 F200
        // N180 MCALL CYCLE81(10,-99.994,1,-135.774)
        // N190 X240 Y-240

        NCFile nc;

        Postprocessor post;

        public CycleState State;

        #region Parametrs

        public bool CycleOn => State.CycleOn;
        public bool Cycle_pocket => State.Cycle_pocket;
        public bool IsFirstCycle => State.IsFirstCycle;

        public bool PolarInterp => State.PolarInterp; 

        public InpArray<double> Prms => State.Prms;

        #endregion

        public SinumerikCycle(Postprocessor post){
            this.post = post;
            nc = post.nc;
            State = new CycleState();
        }

        public void AddPrm(double value, int i) => State.AddPrm(value, i);

        public void Cycle800SwitchOff(){
            nc.GInterp.Hide();
            nc.Block.Out();
            nc.WriteLineWithBlockN($"CYCLE800()");
            nc.GInterp.UpdateState();
        }

        public void Cycle800(int FR, string TC, int ST, int MODE, double X0, 
            double Y0, double Z0, double A, double B, double C, 
            double X1, double Y1, double Z1, int DIR, double FR_I)
        {
            nc.WriteLineWithBlockN($"CYCLE800({FR},\"{TC}\",{ST},{MODE},{X0},{Y0},{Z0},{A},{B},{C},{X1},{Y1},{Z1},{DIR},{FR_I},0)");
        }

        public void OutCycle(string ACycleID, string CycleGeomName){
            int i, n;
            string sss; 

            sss = ACycleID;
            if (Prms.Count > 0)
                sss = sss + "(";
            if (CycleGeomName != "")
                sss = sss + Chr(34) + CycleGeomName + Chr(34);
            n = 0;
            for (i = 0; i < Prms.Count; i++){
                if (Prms[i] != double.MaxValue) n = i + 1;  
            }
            for (i = 0; i < n; i++){
                if ((i > 0) || (CycleGeomName != "")) 
                    sss = sss + ",";
                if (Prms[i] != double.MaxValue)
                    sss = sss + Str(Math.Round(Prms[i], 3));  
            }
            if (Prms.Count > 0) sss = sss + ")";
            if (State.Cyclecompare != sss) {       //Dobavil variant dlya togo, chtoby vyvodit massiv otverstij odnim cziklom
                if (!IsFirstCycle && !Cycle_pocket) {          //menyaetsya parametr, nuzhno zakryt czikl
                    nc.WriteLineWithBlockN($"MCALL"); 
                    nc.X.v = post.LastPnt.X; nc.Y.v = post.LastPnt.Y ; nc.GInterp.v0 = double.MaxValue ; nc.Block.Out();   //Xolostye xody mezhdu proxodami
                }
                var t = nc.GInterp.Changed ? nc.GInterp : null;
                nc.WriteLineWithBlockN($"{t}{sss}"); //Vyvod czikla
                this.SetCycleCompareString(sss);  //Zapominaem vse parametry czikla
            }
        }

        public void Cycle_position(){
            switch(Abs(nc.GPlane.v)){
                case 17:
                    if (IsFirstCycle) //Ne vyvodit pervoe otverstie v czikle, t.k. schitaet chto ono uzhe vyvedeno
                        nc.X.v0 = double.MaxValue; 
                    nc.X.v = post.LastPnt.X; 
                    nc.Y.Show(post.LastPnt.Y);
                    // nc.Y.v = post.LastPnt.Y; nc.Y.v0 = double.MaxValue;  
                    nc.Block.Out(); // Vyvod otverstij  ! XY
                    break;
                case 18:
                    if (IsFirstCycle) //Ne vyvodit pervoe otverstie v czikle, t.k. schitaet chto ono uzhe vyvedeno
                        nc.Z.v0 = double.MaxValue; 
                    nc.Z.v = post.LastPnt.Z;
                    nc.X.Show(post.LastPnt.X);
                    // nc.X.v = post.LastPnt.X; nc.X.v0 = double.MaxValue;
                    nc.Block.Out(); // Vyvod otverstij  ! ZX
                    break;
                case 19:
                    if (IsFirstCycle) //Ne vyvodit pervoe otverstie v czikle, t.k. schitaet chto ono uzhe vyvedeno
                        nc.Y.v0 = double.MaxValue; 
                    nc.Y.v = post.LastPnt.Y;
                    nc.Z.Show(post.LastPnt.Z);
                    // nc.Z.v = post.LastPnt.Z; nc.Z.v0 = double.MaxValue;
                    nc.Block.Out(); // Vyvod otverstij  ! YZ
                    break;
            }
        }

        ///<summary>Set status of the cycle: false - off, true - on</summary>
        public SinumerikCycle SetStatus(bool status){
            State.CycleOn = status;
            return this;
        }

        ///<summary>Set polar interpolation status of the cycle: false - off, true - on</summary>
        public SinumerikCycle SetPolarInterpolationStatus(bool status){
            State.PolarInterp = status;
            return this;
        }

        ///<summary>Set cilind interpolation status of the cycle: false - off, true - on</summary>
        public SinumerikCycle SetCilindInterpolationStatus(bool status){
            State.CylindInterp = status;
            return this;
        }

        ///<summary>Set first status of the cycle: false - not first cycle, true - first cycle</summary>
        public SinumerikCycle SetFirstStatus(bool status){
            State.IsFirstCycle = status;
            return this;
        }

        ///<summary>Set pocket status of the cycle: false - not pocket, true - pocket</summary>
        public SinumerikCycle SetPocketStatus(bool status){
            State.Cycle_pocket = status;
            return this;
        }

        ///<summary>Set first status of the cycle: 0 - not cycle800, 1 - cycle800</summary>
        public SinumerikCycle SetCycleCompareString(string compare){
            State.Cyclecompare = compare;
            return this;
        }

    }
}
