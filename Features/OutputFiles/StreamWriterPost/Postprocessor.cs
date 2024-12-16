namespace DotnetPostprocessing.Post;

using System.Diagnostics;
using System.IO;

public partial class Postprocessor: TPostprocessor
{
    string fileName = Path.GetTempFileName(); //@"D:\Work\Files\NewDNPosts\StreamWriterPost\StreamWriterTest.txt";
    StreamWriter file;

    public override void OnStartProject(ICLDProject prj)
    {
        file = new StreamWriter(fileName, false);
        file.AutoFlush = true;
    }
    
    public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) 
    {
        file.WriteLine(cmd.Caption);
        //Debug.WriteLine(cmd.Caption);
    }

    public override void OnFinishProject(ICLDProject prj)
    {
        NCFiles.AddExternalFile(fileName, this);
    }

}
