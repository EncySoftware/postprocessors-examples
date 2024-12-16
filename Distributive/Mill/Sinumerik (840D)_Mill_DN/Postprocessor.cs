namespace DotnetPostprocessing.Post;

public partial class NCFile : TTextNCFile
{
    public string ProgName;

    public void OutBlockMov()
    {
        if (X.Changed || Y.Changed || Z.Changed ||
            XC_.Changed || YC_.Changed || ZC_.Changed ||
            A.Changed || B.Changed || C.Changed)
            Block.Out();
    }

    public void OutText(string text)
    {
        Text.Show(text);
        Block.Out();
    }

    public void WriteLineWithBlockN(string line)
    {
        if (String.IsNullOrEmpty(line))
            return;
        if (BlockN.Disabled)
            WriteLine(line);
        else
        {
            WriteLine(BlockN.ToString() + Block.WordsSeparator + line);
            BlockN.v += BlockN.AutoIncrementStep;
        }
    }

    public void WriteComment(string text)
    {
        WriteLine(";" + text);
    }

    public void SetDefaultSpiralTurn() => Turn.Show(0);

    public void SetAxisValues(ICLDMultiGotoCommand cmd, ref TInp3DPoint lastPnt)
    {
        foreach (CLDMultiMotionAxis axis in cmd.Axes)
        {
            if (axis.IsX)
            {
                X.v = axis.Value;
                lastPnt.X = X.v;
            }
            else if (axis.IsY)
            {
                Y.v = axis.Value;
                lastPnt.Y = Y.v;
            }
            else if (axis.IsZ)
            {
                Z.v = axis.Value;
                lastPnt.Z = Z.v;
            }
            else if (axis.IsA)
                A.v = axis.Value;
            else if (axis.IsB)
                B.v = axis.Value;
            else if (axis.IsC)
                C.v = axis.Value;
        }
    }

    public void SetAxisValues(ICLDMultiMotionCommand cmd)
    {
        foreach (CLDMultiMotionAxis axis in cmd.Axes)
        {
            if (axis.IsX)
                X.Show(axis.Value);
            else if (axis.IsY)
                Y.Show(axis.Value);
            else if (axis.IsZ)
                Z.Show(axis.Value);
            else if (axis.IsA)
                A.Show(axis.Value);
            else if (axis.IsB)
                B.Show(axis.Value);
            else if (axis.IsC)
                C.Show(axis.Value);
        }
    }
}

public partial class Postprocessor : TPostprocessor
{
    #region Common variables definition
    // Declare here global variables that apply to the entire postprocessor.

    ///<summary>Current nc-file</summary>
    public NCFile nc;

    public NCFile main;

    SinumerikCycle Cycle = null;

    public TInp3DPoint LastPnt;

    double DAM;
    double VARI;
    double VRT;

    double csC;
    double snC;

    #endregion

    #region Extentions

    ///<summary>Make names for all subroutines in the project</summary>
    public void RenameSubs()
    {
        var s = nc.ProgName;
        s = Replace(s, " ", "_");
        for (int i = 0; i < CLDProject.CLDSub.SubCount; i++)
        {
            var sub = CLDProject.CLDSub.GetSubByIndex(i);
            sub.Name = s + "_SUB" + sub.SubCode;
        }
    }

    public void OutHeader(bool isSub)
    {
        if (!Settings.Params.Bol["Format.BlocksNumbering"])
            nc.BlockN.Disable();

        if (isSub)
        {
            nc.WriteLine("%_N_" + nc.ProgName + "_SPF");
            nc.WriteComment("$PATH=/_N_SPF_DIR");
        }
        else
        {
            nc.WriteLine("%_N_" + nc.ProgName + "_MPF");
            nc.WriteComment("$PATH=/_N_MPF_DIR");
        }
        nc.WriteLine();

        nc.WriteComment(" Generated by CAM");
        nc.WriteComment($" Date: {CurDate()}");
        nc.WriteComment($" Time: {CurTime()}");
        nc.WriteLine();
    }

    public void PrintAllTools()
    {
        nc.WriteComment(" Tools list");

        SortedList<int, string> sl = new SortedList<int, string>();

        int firstCapitalIndex = -1;
        string currentTool = "", previousTool = "";

        for (var i = 0; i < CLDProject.Operations.Count; i++)
        {
            var op = CLDProject.Operations[i];
            if (op.Tool != null && op.Tool.Command != null)
            {
                var t = 0;
                while (!string.IsNullOrEmpty(op.Tool.Caption) && !char.IsLetter(op.Tool.Caption[t + 1]))
                {
                    t++;
                    var objectsWithCharAndIndex = op.Tool.Caption.Substring(t).Select((c, i) => new { Char = c, Index = i });
                    firstCapitalIndex = objectsWithCharAndIndex.First(o => Char.IsUpper(o.Char)).Index;
                    t += firstCapitalIndex;
                }
                var skip = t;
                previousTool = currentTool;
                currentTool = op.Tool.Caption.Substring(skip);
                var text = string.IsNullOrEmpty(op.Tool.Caption) ? $"{Transliterate(previousTool)} D{op.Tool.Command.Geom.D} L{op.Tool.Command.Geom.L}"
                    : $"{Transliterate(currentTool)} D{op.Tool.Command.Geom.D} L{op.Tool.Command.Geom.L}, {op.Tool.Caption}";
                sl.TryAdd(op.Tool.Number, text);
            }
        }

        foreach (var tl in sl)
        {
            nc.WriteComment(" T" + tl.Key + " = " + tl.Value);
        }
        nc.WriteLine();
    }

    private string PartMatrixToString(INamedProperty csProp)
    {
        T3DMatrix m = new T3DMatrix
        {
            vX = p3d(csProp.Flt["vX.X"], csProp.Flt["vX.Y"], csProp.Flt["vX.Z"]),
            vY = p3d(csProp.Flt["vY.X"], csProp.Flt["vY.Y"], csProp.Flt["vY.Z"]),
            vZ = p3d(csProp.Flt["vZ.X"], csProp.Flt["vZ.Y"], csProp.Flt["vZ.Z"]),
            vT = p3d(csProp.Flt["vT.X"], csProp.Flt["vT.Y"], csProp.Flt["vT.Z"]),
            A = 0,
            B = 0,
            C = 0,
            D = 1
        };
        TComplexRotationConvention angTypes = new TComplexRotationConvention(
            TRotationConvention.XYZ, true, true);
        var cs = TRotationsConverter.MatrixToLocation(m, angTypes);
        var rounder = new NumericNCWord("{-####.####}", 0);
        var r = (double v) => rounder.ToString(v);
        return $"X{r(cs.P.X)} Y{r(cs.P.Y)} Z{r(cs.P.Z)} A{r(cs.N.A)} B{r(cs.N.B)} C{r(cs.N.C)}";
    }

    private string WorkpieceBoxToString(INamedProperty bxProp)
    {
        if ((bxProp == null) || bxProp.Bol["Empty"])
            return "";
        var pMin = p3d(bxProp.Flt["Min.X"], bxProp.Flt["Min.Y"], bxProp.Flt["Min.Z"]);
        var pMax = p3d(bxProp.Flt["Max.X"], bxProp.Flt["Max.Y"], bxProp.Flt["Max.Z"]);
        pMax = pMax - pMin;
        var rounder = new NumericNCWord("{-####.####}", 0);
        var r = (double v) => rounder.ToString(v);
        return $"X{r(pMin.X)} Y{r(pMin.Y)} Z{r(pMin.Z)} DX{r(pMax.X)} DY{r(pMax.Y)} DZ{r(pMax.Z)}";
    }

    public void PrintCS(ICLDProject prj)
    {
        nc.WriteComment(" Workpiece list");
        var f = prj.CLDFiles[0];
        var parts = prj.Arr["Parts"];
        if (parts == null)
            return;
        for (int i = 0; i <= parts.TopItem; i++)
        {
            var prt = parts[i];
            nc.WriteComment(" " + prt.Str["Name"]);
            var csList = prt.Arr["WCSList"];
            if (csList != null)
            {
                for (int j = 0; j <= csList.TopItem; j++)
                {
                    var cs = csList[j];
                    nc.WriteComment($"   G{cs.Str["Number"]} = {PartMatrixToString(cs.Ptr["Location"])}");
                }
            }
            nc.WriteComment($"   WRK = {WorkpieceBoxToString(prt.Ptr["WorkpieceBox"])}");
        }
        nc.WriteLine();
    }

    #endregion

    public override void OnStartNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc = new NCFile();
        nc.ProgName = cldSub.Name;
        nc.OutputFileName = Path.GetDirectoryName(main.OutputFileName) + @"\" +
            Path.ChangeExtension(cldSub.Name, ".spf");
        OutHeader(true);
        nc.MCoolant.Hide(main.MCoolant.v); nc.X.Hide(main.X.v); nc.Y.Hide(main.Y.v); nc.Z.Hide(main.Z.v);
    }

    public override void OnCallNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.WriteLine($"CALL \"_N_{cldSub.Name}_SPF\"");
    }

    public override void OnFinishNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.M.Show(17);
        nc.Block.Out();
        main.MCoolant.Hide(nc.MCoolant.v); main.X.Hide(nc.X.v); main.Y.Hide(nc.Y.v); main.Z.Hide(nc.Z.v);
        nc = main;
    }

    public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
    {
        if (cmd.IsToolName || (cmd.CLDFile.FileType == CLDFileType.NCSub) && cmd.Index <= 1)
            return;
        nc.WriteComment(Transliterate(cmd.Text));
    }

    public override void OnStartProject(ICLDProject prj)
    {
        main = new NCFile();
        main.OutputFileName = Path.ChangeExtension(Settings.Params.Str["OutFiles.NCFileName"], ".mpf");
        main.ProgName = Settings.Params.Str["OutFiles.NCProgName"];
        if (String.IsNullOrEmpty(main.ProgName))
            main.ProgName = Path.GetFileNameWithoutExtension(main.OutputFileName);
        nc = main;

        RenameSubs();

        OutHeader(false);

        PrintAllTools();

        PrintCS(prj);

        LastPnt = new TInp3DPoint();
        Cycle = new SinumerikCycle(this);
    }

    public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
    {
        Cycle.SetFirstStatus(true);
    }

    public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
    {
        if (cmd.TechInfo.Enabled)
        {
            if (cmd.CLDFile.Index != CLDProject.CLDFiles.FileCount - 1)
            {
                nc.M.Show(1);
                nc.Block.Out();
            }
            nc.WriteLine();
        }
    }

    public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
    {
        nc.CoordSys.v = cmd.CSNumber;
    }

    public override void OnLocalCS(ICLDOriginCommand cmd, CLDArray cld)
    {
        if (cmd.IsOff)
        {
            Cycle.Cycle800SwitchOff();
        }
        else
        { // Local CS activation (CYCLE800)
            nc.Block.Out();

            int v_Dir = 0; // Stay
            if (cmd.PositioningMode > CLDOriginPositionMode.Stay)
            { // Turn, Move
                if (cmd.Ptr["Axes(AxisBPos)"] != null)
                    if (cmd.Flt["Axes(AxisBPos).Value"] >= 0)
                        v_Dir = +1;
                    else
                        v_Dir = -1;
                if (cmd.Ptr["Axes(AxisAPos)"] != null)
                    if (cmd.Flt["Axes(AxisAPos).Value"] >= 0)
                        v_Dir = +1;
                    else
                        v_Dir = -1;
            }

            int v_ST = 000000;
            if (v_Dir > 0)
                v_ST = v_ST + 200000; // Direction selection "Plus" optimized, Swivel no direction "minus" off
            else if (v_Dir < 0)
                v_ST = v_ST + 100000; // Direction selection "Minus" optimized, Swivel no direction "plus" off

            if (cmd.IsSpatial)
                //CYCLE800(_FR, _TC, _ST, _MODE, _X0, _Y0, _Z0, _A, _B, _C, _X1, _Y1, _Z1, _DIR, _FR _I)
                Cycle.Cycle800(0, "", v_ST, 0 * 128 + 0 * 64 + 1 * 32 + 1 * 16 + 1 * 8 + 0 * 4 + 0 * 2 + 1 * 1,
                    cmd.WCS.P.X, cmd.WCS.P.Y, cmd.WCS.P.Z,
                    cmd.WCS.N.A, cmd.WCS.N.B, cmd.WCS.N.C,
                    0, 0, 0, v_Dir, 0);
            else
            {
                double v_A = 0, v_B = 0, v_C = 0;
                if (cmd.Ptr["Axes(AxisAPos)"] != null)
                    v_A = cmd.Flt["Axes(AxisAPos).Value"];
                if (cmd.Ptr["Axes(AxisBPos)"] != null)
                    v_B = cmd.Flt["Axes(AxisBPos).Value"];
                if (cmd.Ptr["Axes(AxisCPos)"] != null)
                    v_C = cmd.Flt["Axes(AxisCPos).Value"];
                //CYCLE800(_FR, _TC, _ST, _MODE, _X0, _Y0, _Z0, _A, _B, _C, _X1, _Y1, _Z1, _DIR, _FR _I)
                Cycle.Cycle800(0, "", v_ST, 1 * 128 + 1 * 64 + 1 * 32 + 1 * 16 + 1 * 8 + 0 * 4 + 0 * 2 + 1 * 1,
                    cmd.WCS.P.X, cmd.WCS.P.Y, cmd.WCS.P.Z,
                    v_A, v_B, v_C, 0, 0, 0, v_Dir, 0);
            }
        }
    }

    public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
    {
        nc.CoordSys.Hide();
        nc.Tool.Show(cmd.Number);
        nc.DTool.Reset(Abs(cmd.LCorNum));
        nc.OutText($"{cmd.TechOperation.Tool.Caption}");
        nc.OutText("M6");

        nc.Feed.RestoreDefaultValue(false);
        nc.GInterp.RestoreDefaultValue(false); nc.GPlane.RestoreDefaultValue(false);
        nc.X.RestoreDefaultValue(false); nc.Y.RestoreDefaultValue(false); nc.Z.RestoreDefaultValue(false);
        nc.A.RestoreDefaultValue(false); nc.B.RestoreDefaultValue(false); nc.C.RestoreDefaultValue(false);
        nc.CoordSys.Show();
    }

    public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
    {
        nc.GPlane.v = cmd.PlaneGCode;
    }

    public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
    {
        if (cmd.IsOn)
        { // Spindle On
            nc.CoordSys.Show();
            nc.Block.Out();
            switch (cmd.SpeedMode)
            {
                case CLDSpindleSpeedMode.RPM:
                    nc.S.Show(cld[2]);
                    nc.Msp.Show(cmd.IsClockwiseDir ? 3 : 4);
                    nc.Block.Out();
                    break;
                case CLDSpindleSpeedMode.CSS: // css
                    throw new Exception("CSS mode not realized");
            }
        }
        else if (cmd.IsOff)
        { // Spindle Off
            nc.Msp.Show(5);
            nc.Block.Out();
        }
        else if (cmd.IsOrient) { /* Spindle Orient*/ }
    }

    public override void OnRadiusCompensation(ICLDCutComCommand cmd, CLDArray cld)
    {
        if (cmd.IsOn)
        {
            if (cmd.IsRightDirection)
                nc.KorEcv.v = 42;
            else
                nc.KorEcv.v = 41;
        }
        else
            nc.KorEcv.v = 40;
    }

    public override void OnLengthCompensation(ICLDCutComCommand cmd, CLDArray cld)
    {
        if (cmd.IsOn)
            nc.DTool.Reset(Abs(cmd.CorrectorNumber));
    }

    public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
    {
        nc.X.Hide(cld[1]); nc.Y.Hide(cld[2]); nc.Z.Hide(cld[3]);
        LastPnt.X = cld[1]; LastPnt.Y = cld[2]; LastPnt.Z = cld[3];
    }

    public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
    {
        if (!Cycle.CycleOn || Cycle.Cycle_pocket)
        {  // Drugoj vyvod dlya sverlilnyx cziklov poziczij otverstij
            if ((nc.GInterp.v > 1) && (nc.GInterp.v < 4)) nc.GInterp.v = 1;

            nc.X.v = Cycle.PolarInterp ? (cld[1] * csC - cld[2] * snC) : cmd.EP.X;  // X,Y,Z in absolutes
            nc.Y.v = Cycle.PolarInterp ? (cld[2] * csC + cld[1] * snC) : cmd.EP.Y;

            nc.Z.v0 = nc.Z.v;
            nc.Z.v = cmd.EP.Z;

            nc.OutBlockMov();                     // output in block NC programm
            LastPnt.X = nc.X.v; LastPnt.Y = nc.Y.v; LastPnt.Z = nc.Z.v; // current coordinates
        }
        LastPnt.X = cld[1]; LastPnt.Y = cld[2]; LastPnt.Z = cld[3]; //Zapominaem koordinaty otvertsij
        Cycle.SetPocketStatus(false); // Vyklyuchaem metku dlya czikla poketov 
    }

    public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
    {
        nc.Feed.v = cmd.FeedValue;
        if (cld[3] == 315) nc.GFeed.v = 94;
        else nc.GFeed.v = 95;
        nc.GFeed.Hide();
        nc.GInterp.v = 1;   // G1
    }

    public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
    {
        if ((nc.GInterp.v != 0) && (nc.GInterp.v != 1))
            nc.GInterp.v = 1;

        //N50 G0 B0 C0
        nc.SetAxisValues(cmd, ref LastPnt);

        nc.GInterp.UpdateState();
        nc.OutBlockMov();
    }

    public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
    {
        //N230 G3 X-810.31 Y-8.355 I=AC(-771.052) J=AC(-0.977)

        double tempX = Cycle.PolarInterp ? (cld[5] * csC - cld[6] * snC) : cmd.EP.X;
        double tempY = Cycle.PolarInterp ? (cld[6] * csC + cld[5] * snC) : cmd.EP.Y;
        double tempZ = cmd.EP.Z;

        nc.GInterp.v = cld[4] * Sgn(cld[17]) > 0 ? 3 : 2; //G3/G2

        nc.X.Show(tempX); nc.Y.Show(tempY); nc.Z.Show(tempZ); // X,Y,Z in absolutes

        if ((Abs(nc.GPlane.v) == 17) && (LastPnt.Z == tempZ))
            nc.Z.Hide(tempZ);
        else if ((Abs(nc.GPlane.v) == 18) && (LastPnt.Y == tempY))
            nc.Y.Hide(tempY);
        else if ((Abs(nc.GPlane.v) == 19) && (LastPnt.X == tempX))
            nc.Z.Hide(tempX);

        // Esli spiral, to vyvodim Turn (kolichestvo oborotov) yavno
        if ((Abs(cmd.Plane) == 17) && (LastPnt.Z != tempZ))
        {
            nc.SetDefaultSpiralTurn();
            if ((LastPnt.X == tempX) && (LastPnt.Y == tempY))
                nc.Turn.v = 1;  // polnyj oborot
        }
        else if ((Abs(cmd.Plane) == 18) && (LastPnt.Y != tempY))
        {
            nc.SetDefaultSpiralTurn();
            if ((LastPnt.X == tempX) && (LastPnt.Z == tempZ))
                nc.Turn.v = 1;  // polnyj oborot
        }
        else if ((Abs(cmd.Plane) == 19) && (LastPnt.X != tempX))
        {
            nc.SetDefaultSpiralTurn();
            if ((LastPnt.Y == tempY) && (LastPnt.Z == tempZ))
                nc.Turn.v = 1;  // polnyj oborot
        };

        if (Abs(nc.GPlane.v) != 19)
            nc.XC_.Show(Cycle.PolarInterp ? (cld[1] * csC - cld[2] * snC) : cld[1]);
        if (Abs(nc.GPlane.v) != 18)
            nc.YC_.Show(Cycle.PolarInterp ? (cld[2] * csC + cld[1] * snC) : cld[2]);
        if (Abs(nc.GPlane.v) != 17)
            nc.ZC_.Show(cld[3]);

        nc.OutBlockMov();
        LastPnt.X = tempX; LastPnt.Y = tempY; LastPnt.Z = tempZ;
    }

    public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
    {
        nc.Block.Out();

        nc.SetAxisValues(cmd);

        if (nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.A.Changed || nc.B.Changed || nc.C.Changed)
        {
            nc.SUPA.Show();
            nc.DTool.v = 0; // Korrektor na dlinu
            nc.Block.Out();
        }
    }

    public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
    {
        if (nc.GInterp.v > 0) nc.GInterp.v = 0;
    }

    public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
    {
        if (cmd.IsOn)
            if (cld[2] == 1)
            {
                nc.MCoolant.v0 = nc.MCoolant.v;
                nc.MCoolant.v = 8; // zhidkost
            }
            else if (cld[2] == 2)
            {
                nc.MCoolant.v0 = nc.MCoolant.v;
                nc.MCoolant.v = 8;
            } // tuman
            else if (cld[2] == 3)
            {
                nc.MCoolant.v0 = nc.MCoolant.v;
                nc.MCoolant.v = 8;
            } // instrument
            else
            {
                nc.MCoolant.v0 = nc.MCoolant.v;
                nc.MCoolant.v = 8;
            } // chto-to eshhe
        else
            nc.MCoolant.Show(9);
    }

    public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
    {
        if (Cycle.CycleOn)
            Cycle.SetStatus(false);
        nc.Block.Out();

        nc.SetAxisValues(cmd);
        nc.SUPA.Show();
        nc.GInterp.Show(0);
        nc.DTool.v = 0; // Korrektor na dlinu

        nc.Block.Out();
        nc.GPlane.RestoreDefaultValue(false);
    }

    public override void OnHoleExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
    {
        int CycleNumber;       // Cycle number
        string CycleName;      // Cycle name
        string CycleGeomName;  // Imya podprogrammy-geometrii czikla
        int CDIR;              // Thread direction 2-G2, 3-G3
        double SDIR;           // Spindle rotation direction
        double CurPos;         // Current position (applicate)
        double CPA = 0;            // Absciss
        double CPO = 0;            // Ordinate
        double TempCoord;      // Auxiliary variable
        double RTP, RFP, SDIS, DP, DPR;

        if (cmd.IsOn)
        {
            Cycle.SetStatus(true);      // ON
            nc.GFeed.v = cld[9] == 1 ? 94 : 95;
            nc.GFeed.Hide();
            nc.Feed.v = cld[10];// Feed_@=MaxReal
            nc.Block.Out();
        }
        else if (cmd.IsOff) Cycle.SetStatus(false); // OFF
        else if (cmd.IsCall)
        { // CALL
            CycleNumber = 0;
            CycleName = "CYCLE";
            CycleGeomName = "";
            switch (cmd.CycleType)
            {
                case 473:
                case >= 481 and <= 491:
                    switch (Abs(nc.GPlane.v))
                    {
                        case 17: // XY
                            CurPos = nc.Z.v;
                            CPA = nc.X.v;
                            CPO = nc.Y.v;
                            break;
                        case 18: // ZX
                            CurPos = nc.Y.v;
                            CPA = nc.Z.v;
                            CPO = nc.X.v;
                            break;
                        case 19: // YZ
                            CurPos = nc.X.v;
                            CPA = nc.Y.v;
                            CPO = nc.Z.v;
                            break;
                        default:
                            CurPos = nc.Z.v;
                            CPA = nc.X.v;
                            CPO = nc.Y.v;
                            throw new Exception("Undefined cycle plane");
                    }
                    if (nc.GPlane.v < 0)
                    {
                        TempCoord = CPA;
                        CPA = CPO;
                        CPO = TempCoord;
                    }
                    // Define base levels
                    RTP = CurPos;
                    RFP = CurPos - cld[7] * Sgn(nc.GPlane.v); // CurPos - Tp
                    SDIS = cld[7] - cld[6]; // Tp - Sf
                    DP = CurPos - cld[8] * Sgn(nc.GPlane.v); // CurPos - Bt
                    DPR = cld[8] - cld[7]; // Bt - Tp
                                           // CycleXX(RTP,RFP,SDIS,DP)
                    Cycle.AddPrm(RTP, 0);
                    Cycle.AddPrm(RFP, 1);
                    Cycle.AddPrm(SDIS, 2);
                    Cycle.AddPrm(DP, 3);
                    CycleNumber = 81;
                    switch (cmd.CycleType)
                    {
                        case 481:
                        case 482:
                        case >= 485 and <= 489: // Simple drilling
                            CycleNumber = cmd.CycleType - 400;
                            Cycle.AddPrm(double.MaxValue, 4);
                            if (cld[15] > 0) Cycle.AddPrm(cld[15], 5); // Delay in seconds
                                                                       // Spindle rotation direction
                            SDIR = nc.S.v > 0 ? 3 : 4;
                            if ((cmd.CycleType == 486) || (cmd.CycleType == 488))
                            {
                                if (!(cld[15] > 0))
                                    Cycle.AddPrm(double.MaxValue, 5);
                                Cycle.AddPrm(SDIR, 6);
                            }
                            else if (cmd.CycleType == 487)
                                Cycle.AddPrm(SDIR, 5);
                            if (cmd.CycleType == 485)
                            {
                                if (!(cld[15] > 0))
                                    Cycle.AddPrm(double.MaxValue, 5);
                                Cycle.AddPrm(cld[10], 6); // WorkFeed
                                Cycle.AddPrm(cld[14], 7); // ReturnFeed
                            }
                            else if (cmd.CycleType == 486)
                            {
                                if (!(cld[15] > 0))
                                    Cycle.AddPrm(double.MaxValue, 5);
                                Cycle.AddPrm(0, 7); // RPA
                                Cycle.AddPrm(0, 8); // RPO
                                Cycle.AddPrm(0, 9); // RPAP
                                Cycle.AddPrm(0, 10); // POSS
                                if (Cycle.Prms[6] == 0)
                                    Cycle.AddPrm(double.MaxValue, 6);
                            }
                            break;
                        case 473:
                        case 483: // Deep drilling (473-chip breaking, 483-chip removing)
                            CycleNumber = 83;
                            Cycle.AddPrm(double.MaxValue, 4);
                            Cycle.AddPrm(CurPos - (cld[7] + cld[17]) * Sgn(nc.GPlane.v), 5); // FDEP = CurPos-(Tp+St)
                            Cycle.AddPrm(double.MaxValue, 6);
                            Cycle.AddPrm(cld[18], 7); // DAM - degression
                            Cycle.AddPrm(cld[15], 8); // DTB - Bottom delay
                            Cycle.AddPrm(cld[16], 9); // DTS - Top delay
                            Cycle.AddPrm(1, 10); // FRF - First feed coef
                            Cycle.AddPrm((cmd.CycleType == 473) ? 0 : 1, 11); // VARI - breaking or removing
                            Cycle.AddPrm(double.MaxValue, 12);
                            Cycle.AddPrm(cld[18], 13);// _MDEP - Minimal deep step (=degression)
                            if (cmd.CycleType == 473)
                                Cycle.AddPrm(cld[20], 14); // _VRT - LeadOut
                            else
                            {
                                Cycle.AddPrm(double.MaxValue, 14);
                                Cycle.AddPrm(cld[19], 16); // _DIS1 - Deceleration
                            }
                            Cycle.AddPrm(0, 15); // _DTD - finish delay (if 0 then = DTB)
                            break;
                        case 484: // Tapping
                            SDIR = nc.S.v > 0 ? 3 : 4;
                            if (cld[19] == 1)
                            { // Fixed socket
                                CycleNumber = 84;
                                Cycle.AddPrm(double.MaxValue, 4);
                                Cycle.AddPrm(double.MaxValue, 5);
                                Cycle.AddPrm(SDIR, 6); // SDAC
                                Cycle.AddPrm(double.MaxValue, 7);
                                Cycle.AddPrm((nc.S.v > 0) ? cld[17] : -cld[17], 8); // PIT
                                Cycle.AddPrm(cld[18], 9); // POSS
                                Cycle.AddPrm(double.MaxValue, 10);
                                Cycle.AddPrm(double.MaxValue, 11);
                                Cycle.AddPrm(double.MaxValue, 12);
                                Cycle.AddPrm(1, 13); //  PTAB
                            }
                            else
                            { // Floating socket
                                CycleNumber = 840;
                                Cycle.AddPrm(double.MaxValue, 4);
                                Cycle.AddPrm(double.MaxValue, 5);
                                Cycle.AddPrm(0, 6); // SDR
                                Cycle.AddPrm(SDIR, 7); // SDAC
                                Cycle.AddPrm(11, 8); // ENC
                                Cycle.AddPrm(double.MaxValue, 9);
                                Cycle.AddPrm((nc.S.v > 0) ? cld[17] : -cld[17], 10); // PIT
                                Cycle.AddPrm(double.MaxValue, 11);
                                Cycle.AddPrm(1, 12);
                            }
                            if (Cycle.IsFirstCycle)
                            {
                                VARI = 0;
                                DAM = cld[2] / 5;
                                VRT = DAM / 5;
                                // Input "Vvedite parametry czikla narezaniya rezby CYCLE84:",
                                //       "Podtip czikla VARI (0-prostoe, 1-lomka struzhki, 2-udalenie struzhki)", VARI,
                                //       "SHag dlya lomki struzhki (DAM)", DAM,
                                //       "Return pri lomke struzhki (VRT)", VRT
                            }
                            if (VARI > 0)
                            {
                                Cycle.AddPrm(VARI, 15); //VARI
                                Cycle.AddPrm(DAM, 16);  //DAM
                                Cycle.AddPrm(VRT, 17);  //VRT
                            }
                            break;
                        case 490: // Thread milling
                            CycleNumber = 90;
                            Cycle.AddPrm(double.MaxValue, 4);
                            Cycle.AddPrm(cld[16], 5); // DIATH - Outer diameter
                            Cycle.AddPrm(cld[16] - cld[22] * 2, 6); // KDIAM - Inner diameter
                            Cycle.AddPrm(cld[17], 7); // PIT - thread step
                            Cycle.AddPrm(cld[10], 8); // FFR - Work feed
                            CDIR = cld[19]; // CDIR - Spiral direction
                            if ((CDIR != 2) && (CDIR != 3))
                                if ((nc.S.v > 0) && (CDIR == 0)) CDIR = 3;
                                else if ((nc.S.v <= 0) && (CDIR == 0)) CDIR = 2;
                                else if ((nc.S.v > 0) && (CDIR == 1)) CDIR = 2;
                                else if ((nc.S.v <= 0) && (CDIR == 1)) CDIR = 3;
                            Cycle.AddPrm(CDIR, 9);
                            Cycle.AddPrm(cld[18], 10); // TYPTH - 0-inner, 1-outer thread
                            Cycle.AddPrm(CPA, 11); // CPA - Center X
                            Cycle.AddPrm(CPO, 12); // CPO - Center Y
                            break;
                        case 491: // Hole pocketing
                            CycleNumber = 4;
                            CycleName = "POCKET";
                            Cycle.SetPocketStatus(true);
                            Cycle.AddPrm(0.5 * cld[16], 4);// PRAD - Radius
                            Cycle.AddPrm(CPA, 5); // PA - Center X
                            Cycle.AddPrm(CPO, 6);// PO - Center Y
                            Cycle.AddPrm(cld[20], 7); // MID - Deep step
                            Cycle.AddPrm(0, 8); // FAL - finish wall stock
                            Cycle.AddPrm(0, 9); // FALD - finish deep stock
                            Cycle.AddPrm(cld[10], 10); // FFP1 - Work feed
                            Cycle.AddPrm(cld[12], 11); // FFD - Plunge feed
                            CDIR = cld[19];
                            if (CDIR <= 1) CDIR = 1 - CDIR;
                            Cycle.AddPrm(CDIR, 12); // CDIR - Spiral direction
                            Cycle.AddPrm(21, 13); // VARI - Rough spiral machining
                            Cycle.AddPrm(cld[22], 14);// MIDA - Horizontal step
                            Cycle.AddPrm(double.MaxValue, 15);
                            Cycle.AddPrm(double.MaxValue, 16);
                            Cycle.AddPrm(0.5 * cld[18], 17); // RAD1 - Spiral radius
                            Cycle.AddPrm(cld[17], 18); // DP1 - Spiral step
                            break;
                    }
                    break;// 5D Drilling cycles
            } // end cmd.CycleType

            if (cmd.CycleType == 491)
            {// Dlya karmanov POCKET
                Cycle.OutCycle(CycleName + Str(CycleNumber), CycleGeomName);
                nc.GInterp.v0 = double.MaxValue;
            }
            else
            { // Vyvod czikla MCALL dlya gruppy otverstij
                Cycle.OutCycle("MCALL" + " " + CycleName + Str(CycleNumber), CycleGeomName);
                Cycle.Cycle_position();  //Vyvod poziczij otverstij
            }
            Cycle.SetFirstStatus(false);
        }

        if (cmd.IsOff && cmd.CycleType != 491)
        {//Vyklyuchenie czikla
            nc.WriteLineWithBlockN($"MCALL");
            Cycle.SetFirstStatus(true);
            Cycle.SetCycleCompareString(""); // Prinuditelno stiraem, t.k. czikl zakryt
        } //Vyklyuchenie czikla
    }

    public override void OnFinishProject(ICLDProject prj)
    {
        nc.Block.Out();
        nc.M.Show(30);// M30 end programm
        nc.Block.Out();
        CLDSub.Translate();
    }

    public override void OnInterpolation(ICLDInterpolationCommand cmd, CLDArray cld)
    {
        if (cmd.InterpType == 9023)
        {// MULTIAXIS interpolation
            if (cmd.IsOn)
            { // Switch on
                nc.Block.Out();
                nc.WriteLineWithBlockN($"TRAORI");
                nc.CoordSys.v0 = double.MaxValue; nc.Block.Out();
                nc.WriteLineWithBlockN($"ORIWKS");
                nc.WriteLineWithBlockN($"ORIAXES");
            }
            else
            {          // Switch off
                nc.Block.Out();
                nc.WriteLineWithBlockN($"TRAFOOF");
            }
        }
        else if (cmd.InterpType == 9021)
        {// Polar interpolation
            if (cmd.IsOn)
            { // Switch on
                nc.Block.Out();
                nc.WriteLine("TRANSMIT");
                Cycle.SetPolarInterpolationStatus(true);
                csC = Cos(nc.A.v);
                snC = -Sin(nc.A.v);
            }
            else
            {          // Switch off
                nc.Block.Out();
                nc.WriteLine("TRANSMIT");
                Cycle.SetPolarInterpolationStatus(true);
                nc.A.RestoreDefaultValue(false);
            }
        }
        else if (cmd.InterpType == 9022)
        {// Cylindrical interpolation
            if (cmd.IsOn)
            { // Switch on
                nc.Block.Out();
                nc.WriteLine("TRACYL(" + Str(2 * cld[3]) + ")");
                Cycle.SetCilindInterpolationStatus(true);
            }
            else
            {             // Switch off
                nc.Block.Out();
                nc.WriteLine("TRAFOOF");
                //Output "TMCOFF"
                Cycle.SetCilindInterpolationStatus(false);
                nc.A.RestoreDefaultValue(false);
            }
        }
    }

    public override void OnAxesBrake(ICLDAxesBrakeCommand cmd, CLDArray cld)
    {
        foreach (CLDAxisBrake axis in cmd.Axes)
        {
            if (axis.IsA)
                nc.MABrake.v = axis.StateIsOn ? 46 : 47;
            else if (axis.IsB)
                nc.MBBrake.v = axis.StateIsOn ? 48 : 49;
            else if (axis.IsC)
                nc.MCBrake.v = axis.StateIsOn ? 10 : 11;
        }
        if (nc.MABrake.Changed || nc.MBBrake.Changed || nc.MCBrake.Changed)
        {
            nc.GInterp.Hide();
            nc.Block.Out();
            nc.GInterp.UpdateState();
        }
    }

    public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.GPause.Show(4);
        nc.FPause.Show(cld[1]);
        nc.Block.Out();
    }

    public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.M.Show(1);// M01
        nc.Block.Out();
    }

    public override void OnStop(ICLDStopCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.M.Show(0);// M00
        nc.Block.Out();
    }

    public override void StopOnCLData()
    {
        base.StopOnCLData();
    }
}
