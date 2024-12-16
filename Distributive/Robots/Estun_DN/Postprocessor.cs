using System.Collections;

namespace DotnetPostprocessing.Post
{

    /// <summary>The type of robot movement</summary>
    public enum RobotMovementType
    {
        Joint,
        Line,
        Circle
    }

    /// <summary>The type of point to know what to search with scanner.</summary>
    public enum SearchPointType
    {
        None,
        FirstEnterPoint,
        LastEnterPoint,
        TrackStartPoint,
        TrackingPoint,
        FirstExitPoint,
        LastExitPoint
    }

    /// <summary>The main class of the postprocessor that inherits from an abstract TPostprocessor class</summary>
    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Resulting count of robot program file pairs (*.erp+*.erd file pairs) we have.</summary>
        int programsCount;

        ///<summary>Resulting robot program main file with contains only calls of all other files.</summary>
        RobotProgramMainFile mainFile;

        ///<summary>Current robot program file with points we are writing.</summary>
        RobotProgramPointsFile pointsFile;

        ///<summary>Current robot program file with contains movements.</summary>
        RobotProgramMovesFile movesFile;

        ///<summary>Robot's current state - cartesian coordinates, joints, flips</summary>
        RobotState state;

        ///<summary>Last movement type</summary>
        RobotMovementType lastMoveType;
 
        ///<summary>Effector state type</summary>
        bool effectorIsOn=false;

        //<summary>Use point searching by scanner.</summary>
        bool useSearch = false;

        #endregion

        /// <summary>The method to be possible to use CLData breakpoints. Just add a "Function breakpoint" with this method's name.</summary>
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        /// <summary>Starts writing of a new program files pair (erp+erd). Finishes previous program files if they were.</summary>
        void StartNewProgram()
        {
            programsCount++; // increase the count of program files
            string progsDir = Settings.Params.Str["OutFiles.OutputFolder"] + @"\";
            string progName = Settings.Params.Str["OutFiles.ProgramName"];
            progName = progName + programsCount;
            if ((progName.Length<1) || (progName.Length>256)) {
                Log.Error("The program name can be from 1 to 256 characters.");
            } else if (Char.IsDigit(progName[0]))    
                Log.Error("The program name can NOT begin with a NUMBER.");
            if (progName.Contains('@') || progName.Contains('*'))    
                Log.Error("Using (@) or (*) in program name is not allowed.");

            // if (movesFile!=null)
            //     movesFile.EndFile();
            movesFile = new();
            movesFile.OutputFileName = progsDir + progName + ".erp";
            movesFile.StartFile();
            movesFile.SetCoord(state.BaseNum);
            movesFile.SetTool(state.ToolNum);

            // if (pointsFile!=null) 
            //     pointsFile.EndFile();
            pointsFile = new();
            pointsFile.OutputFileName = progsDir + progName + ".erd";
            pointsFile.FileIndex = programsCount;
            pointsFile.StartFile();

            mainFile.AddCall(progName);
        }

        /// <summary>Finishes writing of the current program files.</summary>
        void FinishPrograms() {
            if (pointsFile!=null) 
                pointsFile.EndFile();
            if (movesFile!=null)
                movesFile.EndFile();
        }
        
        /// <summary>Instead of PartNo</summary>
        public override void OnStartProject(ICLDProject prj)
        {
            useSearch = Settings.Params.Bol["Format.UseSearch"];
            // MaxMoveCount = Settings.Params.Int["OutFiles.MaxMoveCount"];
            
            state = new RobotState();
            state.accValue = Settings.Params.Flt["Smoothing.DefaultACC"];
            state.PLValue = Settings.Params.Int["Smoothing.PLValue"];
            state.velocity = Settings.Params.Flt["Smoothing.StartVelocity"];
            state.J.DefineExtAxesIsOn(prj);
            state.robotHolds = (RobotHolds)Settings.Params.Int["Format.RobotHolds"];

            string progsDir = Settings.Params.Str["OutFiles.OutputFolder"] + @"\";
            mainFile = new();
            mainFile.OutputFileName = progsDir + "main.erp";
            mainFile.StartFile();

            TTextNCFile mainERD = new();
            mainERD.OutputFileName = progsDir + "main.erd";
            mainERD.WriteLine();
        }
        
        /// <summary>Instead of Fini</summary>
        public override void OnFinishProject(ICLDProject prj)
        {
            mainFile.EndFile();
        }

        /// <summary>Instead of PPFun(58)</summary>
        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            state.Pos = RobotState.UndefinedLocation;
            state.feedKind = CLDFeedKind.First;

            if ((Pos("ExtTool", op.ToolRevolverID)>=0) || (Pos("TableTool", op.ToolRevolverID)>=0)) 
                state.robotHolds = RobotHolds.Part;

            if (op.Tool!=null)
                state.ToolNum = op.Tool.Number;
            if (op.WorkpieceCSCommand!=null)
                state.BaseNum = Round(op.WorkpieceCSCommand.CSNumber);

            StartNewProgram();
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            FinishPrograms();            
        }  

        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            movesFile.WriteLine(cmd.Text);
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            // if (cmd.IsOn) {
            //     int revs = Round(cmd.RPMValue*2000/18000);
            //     if (revs != state.spindleRevs) {
            //         movesFile.WriteLine("AO[1]=" + revs + ";");
            //         state.spindleRevs = revs;
            //     }
            //     if (!state.toolIsOn) {
            //         movesFile.WriteLine("DO[1]=ON ;");
            //         movesFile.WriteLine("WAIT  5.00(sec) ;");
            //         state.toolIsOn = true;
            //     }
            // } else if (cmd.IsOff) {
            //     if (state.toolIsOn) {
            //         movesFile.WriteLine("DO[1]=OFF ;");
            //         state.toolIsOn = false;
            //     }
            // }   
        }

        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            int n = Round(cmd.CSNumber);
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.MCS;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                state.BaseNum = n;
                state.Base = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                state.ToolNum = n;
                state.Tool = coords;
            }
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            int n = cmd.Number;
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.Overhang;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                state.ToolNum = n;
                state.Tool = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                state.BaseNum = n;
                state.Base = coords;
            }
        }

        /// <summary>Fedrate + Rapid</summary>
        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            state.prevFeedKind = state.feedKind;
            state.feedKind = cmd.FeedKind;
            state.velocity = cmd.FeedValue / 60;
            if (cmd.IsRapid)
                state.feedVariableName = Settings.Params.Str["Speeds.RapidFeedVar"];//"V500";
            else
                state.feedVariableName = Settings.Params.Str["Speeds.WorkFeedVar"];
        }

        private CLDFeedKind GetFeedKindOfNextMove()
        {
            CLDFeedKind nextFeedKind = state.feedKind;
            var cmd = this.CurrentCmd.Next;
            while (cmd != null) {
                if (cmd is ICLDMoveVelocityCommand)
                {   
                    nextFeedKind = (cmd as ICLDMoveVelocityCommand).FeedKind;
                    break;
                } else if (cmd is ICLDMotionCommand) {
                    break;
                }
                cmd = cmd.Next;
            }
            return nextFeedKind;
        }

        public override void OnEffector(ICLDEffectorCommand cmd, CLDArray cld)
        {
        //    if (cmd.IsOn && !effectorIsOn) {
        //        prg.WriteLine("DOUT M#(111)=ON");
        //    } else if (cmd.IsOff  && effectorIsOn) {
        //        prg.WriteLine("DOUT M#(111)=OFF");
        //    }
            effectorIsOn = cmd.IsOn;
        }

        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            Motion(cmd);            
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            Motion(cmd);            
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        public override void OnMultiArc(ICLDMultiArcCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        /// <summary>All movement commands call this method (MultiGOTO, PhysicGoto, MultiArc, From)</summary>
        void Motion(ICLDMotionCommand movement) 
        {
            state.prevPos = state.Pos;
            if (movement.CmdType==CLDCmdType.MultiArc) {
                // if ((lastMoveType==RobotMovementType.Joint) || (lastMoveType==RobotMovementType.Line))
                //     WriteState(RobotMovementType.Circle, 1);
                
                ICLDMultiArcCommand cmd = movement as ICLDMultiArcCommand;
                state.middlePos = new TInpLocation(cmd.MidP.P, cmd.MidP.N);
                state.middleJ.Assign(state.J);
                state.middleJ.FillJoints(cmd.MidP.Axes);

                state.Pos = new TInpLocation(cmd.EndP.P, cmd.EndP.N);
                state.J.Assign(state.middleJ);
                state.J.FillJoints(cmd.EndP.Axes);
                WriteState(RobotMovementType.Circle);
            } else {
                ICLDMultiMotionCommand cmd = movement as ICLDMultiMotionCommand;
                state.Pos = new TInpLocation(cmd.EP, cmd.EN);
                state.J.FillJoints(cmd.Axes);
                if (movement.CmdType==CLDCmdType.From) {
                    // Output nothing, just remember
                    return;
                } else if (movement.CmdType==CLDCmdType.MultiGoto) {
                    WriteState(RobotMovementType.Line);
                } else { //PhysicGoto or GoHome
                    WriteState(RobotMovementType.Joint);
                }
            }
        }

        void WriteState(RobotMovementType movType)
        {
            switch (movType)    
            {
                case RobotMovementType.Joint:
                    var jointPointName = pointsFile.AddJointPoint(state.J);
                    movesFile.MoveJ(jointPointName, Settings.Params.Str["Speeds.JointFeedVar"]);
                    break;
                case RobotMovementType.Line:
                    if (useSearch) {
                        var pointType = DetectSearchPointType();
                        var spatialPointName = "";
                        switch(pointType){
                            case SearchPointType.FirstEnterPoint:
                                movesFile.LaserSearch("P_LEx", "P_FEx", "P_ExT", state.feedVariableName );
                                movesFile.MoveL("P_LEx", state.feedVariableName);
                                movesFile.MoveL("P_FEn", state.feedVariableName);
                                movesFile.LaserSearch("P_FEn", "P_LEn", "P_EnT", state.feedVariableName );
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_FEn");
                                break;
                            case SearchPointType.LastEnterPoint:
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_LEn");
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_EnT");
                                movesFile.MoveL(spatialPointName, state.feedVariableName);
                                // movesFile.TrackStart(spatialPointName, state.feedVariableName);
                                break;
                            case SearchPointType.TrackStartPoint:
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J);
                                movesFile.TrackStart(spatialPointName, state.feedVariableName);
                                break;
                            case SearchPointType.FirstExitPoint:
                                movesFile.TrackEnd();
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_ExT");
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_FEx");
                                movesFile.MoveL("P_ExT", state.feedVariableName);
                                break;
                            case SearchPointType.LastExitPoint:
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J, "P_LEx");
                                movesFile.MoveL(spatialPointName, state.feedVariableName);
                                break;
                            case SearchPointType.TrackingPoint:    
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J);
                                movesFile.MoveLTrack(spatialPointName, state.feedVariableName);
                                break;
                            default:
                                spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J);
                                movesFile.MoveL(spatialPointName, state.feedVariableName);
                                break;
                        }
                    } else {
                        var spatialPointName = pointsFile.AddSpatialPoint(state.Pos, state.J);
                        movesFile.MoveL(spatialPointName, state.feedVariableName);
                    }
                    break;
                case RobotMovementType.Circle:
                    var midPointName = pointsFile.AddSpatialPoint(state.middlePos, state.middleJ);
                    var endPointName = pointsFile.AddSpatialPoint(state.Pos, state.J);
                    movesFile.MoveC(midPointName, endPointName, state.feedVariableName);
                    break;
            }
            lastMoveType = movType;
            state.prevFeedKind = state.feedKind;
        }

        SearchPointType DetectSearchPointType()
        {
            SearchPointType pointType = SearchPointType.None;
            var nextFeedKind = GetFeedKindOfNextMove();
            if ((nextFeedKind!=state.feedKind) && (nextFeedKind==CLDFeedKind.Engage))
                pointType = SearchPointType.FirstEnterPoint;
            else if ((state.prevFeedKind!=state.feedKind) && (state.feedKind==CLDFeedKind.Engage))
                pointType = SearchPointType.LastEnterPoint;
            else if ((nextFeedKind!=state.feedKind) && (nextFeedKind==CLDFeedKind.Retract))
                pointType = SearchPointType.FirstExitPoint;
            else if ((state.prevFeedKind!=state.feedKind) && (state.feedKind==CLDFeedKind.Retract))
                pointType = SearchPointType.LastExitPoint;
            else if ((state.prevFeedKind==CLDFeedKind.Engage) && (state.feedKind==CLDFeedKind.Working))
                pointType = SearchPointType.TrackStartPoint;
            else if (state.feedKind==CLDFeedKind.Working)
                pointType = SearchPointType.TrackingPoint;
            return pointType;
        }
    }
}
