namespace DotnetPostprocessing.Post;

/// <summary>The type that defines what the robot holds - tool or part.</summary>
public enum RobotHolds
{
    /// <summary>Robot holds tool</summary>
    Tool,
    /// <summary>Robot holds part</summary>
    Part
}

///<summary>List of variables that contain robot's state - axes values (J1-J6, E1-E6), flips, current and previous
///position in cartesian CS</summary>
public class RobotState
{
    ///<summary>Joints' values (J1-J6, E1-E6)</summary>
    public Joints J;

    ///<summary>Joints' values at the middle point of circle (J1-J6, E1-E6)</summary>
    public Joints middleJ;

    ///<summary>Cartesian coordinates (X Y Z A B C) in current position</summary>
    public TInpLocation Pos;

    ///<summary>Cartesian coordinates (X Y Z A B C) in previous position</summary>
    public TInpLocation prevPos;

    ///<summary>Cartesian coordinates (X Y Z A B C) in arc middle position</summary>
    public TInpLocation middlePos;

    ///<summary>Some undefined number 9999999</summary>
    public static double Undefined = 9999999;

    ///<summary>Undefined cartesian coordinates constant (X Y Z A B C)=9999999</summary>
    public static TInpLocation UndefinedLocation = new TInpLocation(Undefined, Undefined, Undefined, Undefined, Undefined, Undefined, Undefined);

    ///<summary>Current spindle revolutions</summary>
    public int spindleRevs = 0;

    ///<summary>True if tool is active (rotates)</summary>
    public bool toolIsOn = false;

    ///<summary>Current velocity of movements in mm/min</summary>
    public double velocity = 0;

    ///<summary>Current PL value for non joint movements</summary>
    public double PLValue = 9;

    ///<summary>Current ACC value</summary>
    public double accValue = 0;

    ///<summary>Current feed kind</summary>
    public CLDFeedKind feedKind;

    ///<summary>Previous feed kind</summary>
    public CLDFeedKind prevFeedKind;

    ///<summary>Name of variable for current feed. Depends on feed kind.</summary>
    public string feedVariableName;
    
    ///<summary>User frame number (UFRAME)</summary>
    public int BaseNum;

    ///<summary>User frame coordinates (X Y Z W P R)</summary>
    public TInpLocation Base;

    ///<summary>User tool number (TFRAME)</summary>
    public int ToolNum;

    ///<summary>User tool coordinates (X Y Z W P R)</summary>
    public TInpLocation Tool;

    ///<summary>What the robot holds - tool or part</summary>
    public RobotHolds robotHolds;

    ///<summary>Method to first initialize robot's state</summary>
    public RobotState() 
    {
        J = new ();
        middleJ = new();
    }
}
