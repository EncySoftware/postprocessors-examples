namespace DotnetPostprocessing.Post;

///<summary>Resulting robot program file with contains movements.</summary>
public partial class RobotProgramMovesFile : TTextNCFile
{
 
    /// <summary>Method in wich is possible to initialize some properties of the file</summary>
    public override void OnInit()
    {
        //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
    }

    public void StartFile()
    {
        WriteLine("Start:");
    }

    public void EndFile()
    {
        WriteLine("End;");
    }

    public void LaserSearch(string startPoint, string endPoint, string targetPoint, string feedVarName)
    {
        //MovL{P=t_l.P1,V=t_s.V500,B="RELATIVE",C=t_s.C10Num=1,JobEnable="False"}
        WriteLine("Laser_Search{ Num=1,Flag=0, startPos=t_l." + startPoint + ", endPos=t_l." + endPoint + ", targetPos=t_l." + targetPoint + ",V=t_s." + feedVarName + ",B=\"FINE\",triggerSignal=\"DITrig\",Number=0,Value=0}");

    }

    public void TrackStart(string pointName, string feedVarName)
    {
        //MovL{P=t_l.P1,V=t_s.V500,B="RELATIVE",C=t_s.C10Num=1,JobEnable="False"}
        WriteLine("TrackStart{P=t_l." + pointName + ",V=t_s."+feedVarName+",B=\"RELATIVE\",C=t_s.C100, Num=1,ExitMode=\"distance\",ExitTime=100}");
    }

    public void TrackEnd()
    {
        WriteLine("TrackEnd{}");
    }

    public void MoveL(string pointName, string feedVarName)
    {
        //MovL{P=t_l.P1,V=t_s.V500,B="RELATIVE",C=t_s.C10Num=1,JobEnable="False"}
        WriteLine("MovL{P=t_l." + pointName + ",V=t_s."+feedVarName+",B=\"RELATIVE\",C=t_s.C10Num=1,JobEnable=\"False\"}");
    }

    public void MoveLTrack(string pointName, string feedVarName)
    {
        //MovL{P=t_l.P1,V=t_s.V500,B="RELATIVE",C=t_s.C10Num=1,JobEnable="False"}
        WriteLine("MovLTrack{P=t_l." + pointName + ",V=t_s."+feedVarName+",B=\"RELATIVE\",C=t_s.C10Num=1,JobEnable=\"False\"}");
    }

    public void MoveJ(string pointName, string feedVarName)
    {
        //MovJ{P=t_p.J1,V=t_s.V10,B="ABSOLUTE",C=t_s.C0}
        WriteLine("MovJ{P=t_p." + pointName + ",V=t_s."+feedVarName+",B=\"ABSOLUTE\",C=t_s.C0");
    }

    public void MoveC(string midPointName, string endPointName, string feedVarName)
    {
       //MovC{A=t_l.P13,P=t_p.P14,V=t_s.V100,B="ABSOLUTE",C=t_s.C10}
        WriteLine("MovC{A=t_l." + midPointName + ",P=t_p." + endPointName + ",V=t_s."+feedVarName+",B=\"ABSOLUTE\",C=t_s.C10}");

    }

    public void SetTool(int toolNumber)
    {
        WriteLine("SetTool{Tool=t_g.Tool" + toolNumber + "}");
    }

    public void SetCoord(int workpieceNumber)
    {
        WriteLine("SetCoord{Coord=t_g.Wobj" + workpieceNumber + "}");
    }

}
