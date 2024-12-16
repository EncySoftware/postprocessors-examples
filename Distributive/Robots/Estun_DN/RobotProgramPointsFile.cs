namespace DotnetPostprocessing.Post;

///<summary>Resulting robot program file with points we are writing.</summary>
public partial class RobotProgramPointsFile: TTextNCFile
{
    ///<summary>Program file index</summary>
    public int FileIndex;

    ///<summary>The count of spatial (P) points inside the file</summary> 
    public int SpatialPointsCount;

    ///<summary>The count of joint (J) points inside the file</summary> 
    public int JointPointsCount;

    ///<summary>Number value formatter</summary>
    public NumericNCWord Number = new NumericNCWord("{+0000.000}", 0); //{-####.###}

    ///<summary>Integer number value formatter</summary>
    public NumericNCWord IntNumber = new NumericNCWord("{0000}", 0); 

    ///<summary>Points to the position in file where we write spatial (P) points</summary> 
    private INCLabel spatialPointsSection;

    ///<summary>Points to the position in file where we write joint (J) points</summary> 
    private INCLabel jointPointsSection;

    /// <summary>Method in wich is possible to initialize some properties of the file</summary>
    public override void OnInit()
    {
    //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
    }

    public void StartFile()
    {
        spatialPointsSection = CreateLabel();
        // add empty line between sections
        WriteLine();
        jointPointsSection = CreateLabel();

        spatialPointsSection.SnapToRight();
        jointPointsSection.SnapToRight();
    }

    public void EndFile()
    {
        
    }

    ///<summary>Add a new spatial (P) point to the file. Returns name of this point.</summary> 
    public string AddSpatialPoint(TInpLocation pos, Joints joints, string DesiredPointName = "")
    {
        //P2={_type="CPOS",confdata={_type="POSCFG",mode=0,cf1=0,cf2=0,cf3=0,cf4=0,cf5=0,cf6=0},x=101.828,y=-28.414,z=-46.586,a=-25.279,b=23.123,c=-140.253,a7=10,a8=0.0000000,a9=0.0000000,a10=0.0000000,a11=0.0000000,a12=0.0000000,a13=0.0000000,a14=0.0000000,a15=0.0000000,a16=0.0000000}
        DefaultLabel = spatialPointsSection;
        SpatialPointsCount++;
        string pointName = "P" + IntNumber.ToString(SpatialPointsCount);
        if (DesiredPointName != "")
            pointName = DesiredPointName; // + "_" + FileIndex;
        Write(pointName + "={_type=\"CPOS\"," + joints.GetConfdata());
        Write(", x=" + Number.ToString(pos.P.X));
        Write(",y=" + Number.ToString(pos.P.Y));
        Write(",z=" + Number.ToString(pos.P.Z));
        Write(",a=" + Number.ToString(pos.N.A));
        Write(",b=" + Number.ToString(pos.N.B));
        Write(",c=" + Number.ToString(pos.N.C));

        for (int i = 1; i <= 6; i++)
            Write($", a{i}=" + Number.ToString(joints.J[i]));
        for (int i = 7; i <= 12; i++)
            if (joints.IsEOn[i-6])
                Write($", a{i}=" + Number.ToString(joints.E[i-6]));
            else
                Write($", a{i}=" + Number.ToString(0));
        for (int i = 13; i <= 16; i++)
            Write($", a{i}=" + Number.ToString(0));

        WriteLine("}");
        return pointName;
    }

    ///<summary>Add a new joint (J) point to the file. Returns name of this point.</summary> 
    public string AddJointPoint(Joints joints, string DesiredPointName = "")
    {
        //J1={_type="APOS",a1=13.75,a2=-24.123,a3=40.485,a4=-111.693,a5=16.681,a6=83.03,a7=10,a8=0.0000000,a9=0.0000000,a10=0.0000000,a11=0.0000000,a12=0.0000000,a13=0.0000000,a14=0.0000000,a15=0.0000000,a16=0.0000000}
        DefaultLabel = jointPointsSection;
        JointPointsCount++;
        string pointName = "J" + IntNumber.ToString(JointPointsCount);
        if (DesiredPointName != "")
            pointName = DesiredPointName; // + "_" + FileIndex;
        Write(pointName + "={_type=\"APOS\"");

        for (int i = 1; i <= 6; i++)
            Write($", a{i}=" + Number.ToString(joints.J[i]));
        for (int i = 7; i <= 12; i++)
            if (joints.IsEOn[i-6])
                Write($", a{i}=" + Number.ToString(joints.E[i-6]));
            else
                Write($", a{i}=" + Number.ToString(0));
        for (int i = 13; i <= 16; i++)
            Write($", a{i}=" + Number.ToString(0));

        WriteLine("}");
        return pointName;
    }

}

