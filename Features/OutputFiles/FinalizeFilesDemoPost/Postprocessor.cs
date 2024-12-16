namespace DotnetPostprocessing.Post;

public partial class Postprocessor: TPostprocessor
{   
    TTextNCFile file;
    int fileCount;

    public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) {
        fileCount++;
        file = new TTextNCFile();
        file.OutputFileName = @"D:\Work\Files\NewDNPosts\FinalizeFilesDemoPost\bin\FinalizeDemo"+fileCount+".txt";

        file.WriteLine("<Document>");
    }
    
    public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) {
        file?.WriteLine(cmd.Caption);
    }

    public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) {
        file.WriteLine("</Document>");
        file = null;
    }

    public override void OnFinalizeNCFiles(TNCFilesManager ncFiles) {
        string notepadFileName = @"C:\Program Files\Notepad++\notepad++.exe";
        if (!File.Exists(notepadFileName) && File.Exists(@"C:\Program Files (x86)\Notepad++\notepad++.exe"))
            notepadFileName= @"C:\Program Files (x86)\Notepad++\notepad++.exe";
        if (!File.Exists(notepadFileName))
            notepadFileName = "notepad.exe";

        for (int i = 0; i < ncFiles.FileCount; i++)
        {
            // File.Copy(ncFiles[i].OutputFileName, @"//RobotCNC/file.txt");
            Process.Start(notepadFileName, ncFiles[i].OutputFileName);
            Thread.Sleep(500); // wait for notepad to open
        }
    }

}
