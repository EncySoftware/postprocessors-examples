namespace DotnetPostprocessing.Post;

///<summary>Resulting robot program main file with contains only calls of all other files.</summary>
public partial class RobotProgramMainFile : TTextNCFile
{
 
    /// <summary>Method in wich is possible to initialize some properties of the file.</summary>
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

    public void AddCall(string callingFileName)
    {
        WriteLine("CALL " + callingFileName);
    }

}
