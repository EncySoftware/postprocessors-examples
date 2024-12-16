namespace DotnetPostprocessing.Post;

using System.Collections;

/// <summary>The type that contains the values of robot axes J1-J6, E1-E6 and some joints' properties</summary>
public class Joints
{
    /// <summary>Array of joints' values J1-J6. J[0] doesn't use, just for convenient indexing starting from 1.</summary>
    public double[] J = new double[7];

    /// <summary>Array of robot's external axes' values E1-E6. E[0] doesn't use, just for convenient indexing starting from 1.</summary>
    public double[] E = new double[7];

    /// <summary>The flag that defines enabled the external axis E[i] or not</summary>
    public bool[] IsEOn = new bool[7];

    /// <summary>Units of external axis E[i] - 'mm' or 'deg'</summary>
    public bool[] EUnitsAreDegrees = new bool[7];

    /// <summary>True if at least one of E1-E6 is enabled</summary>
    public bool ThereAreE = false;

    /// <summary>Method to first initialize joints</summary>
    public Joints()
    {
        for (int i = 1; i <= 6; i++)
            EUnitsAreDegrees[i] = false;
    }

    /// <summary>Copy state from another instance</summary>
    public void Assign(Joints srcJ)
    {
        // assign to J copy of srcJ.J
        J = (double[])srcJ.J.Clone();
        // assign to E copy of srcJ.E
        E = (double[])srcJ.E.Clone();
        // assign to IsEOn copy of srcJ.IsEOn
        IsEOn = (bool[])srcJ.IsEOn.Clone();
        // assign to EUnitsAreDegrees copy of srcJ.EUnitsAreDegrees
        EUnitsAreDegrees = (bool[])srcJ.EUnitsAreDegrees.Clone();
        // set ThereAreE
        ThereAreE = srcJ.ThereAreE;
    }

    /// <summary>Method to read axes values from axes array of MultiGoto (and MultiArc) command to J[1..6] and E[1..6]</summary>
    public void FillJoints(IEnumerable axes)
    {
        foreach (CLDMultiMotionAxis ax in axes)
        {
            if (ax.IsA1)
                this.J[1] = ax.Value;
            else if (ax.IsA2)
                this.J[2] = ax.Value;
            else if (ax.IsA3)
                this.J[3] = ax.Value;
            else if (ax.IsA4)
                this.J[4] = ax.Value;
            else if (ax.IsA5)
                this.J[5] = ax.Value;
            else if (ax.IsA6)
                this.J[6] = ax.Value;
            else if (ax.IsE1)
                this.E[1] = ax.Value;
            else if (ax.IsE2)
                this.E[2] = ax.Value;
            else if (ax.IsE3)
                this.E[3] = ax.Value;
        }
    }

    /// <summary>Method to read from the machine's properties the list of available external axes and their type</summary>
    public void DefineExtAxesIsOn(ICLDProject prj)
    {
        foreach (ICLDMachineAxisInfo ax in prj.Machine.Axes)
        {
            if (!ax.Enabled)
                continue;
            for (int i = 1; i <= 6; i++)
            {
                if (SameText(ax.AxisID, "ExtAxis" + i + "Pos"))
                {
                    IsEOn[i] = true;
                    EUnitsAreDegrees[i] = ax.IsRotary;
                    break;
                }
            }
        }
        // If E[i] is on then enabling all axes below i
        bool on = false;
        for (int i = 6; i >= 1; i--)
        {
            on = on || IsEOn[i];
            IsEOn[i] = on;
        }
        ThereAreE = on;
    }

    public int GetMode()
    {
        int flag1 = 0;
        int flag3 = 0;
        int flag5 = 0;
        if ((J[5] >= 0) && (J[5] <= 180))
            flag5 = 0;
        if ((J[5] < 0) && (J[5] > -180))
            flag5 = 1;
        int mode = flag5 + 2 * flag3 + 4 * flag1;
        return mode;
    }


    public string GetConfdata()
    {
        var cf = new int[7];
        for (int i = 1; i <= 6; i++)
        {
            cf[i] = 0;
            if ((J[i]>-180) && (J[i]<=180)) 
                cf[i] = 0;
            else
            if ((J[i]>180) && (J[i]<=3*180)) 
                cf[i] = 1;
            else
            if ((J[i]>-180*3) && (J[i]<=-180))
                cf[i] = -1;
        }

        string confdata = "confdata={_type=\"POSCFG\",mode="+GetMode()+$",cf1={cf[1]},cf2={cf[2]},cf3={cf[3]},cf4={cf[4]},cf5={cf[5]},cf6={cf[6]}" + "}";
        return confdata;
    }

}
