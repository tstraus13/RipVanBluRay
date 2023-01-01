using System.Collections.Concurrent;
using System.Diagnostics;

namespace RipVanBluRay.Models;

public class SharedState
{
    internal List<DiscDrive> DiscDrives;
    internal List<Process> MoveProcesses;
    internal ConcurrentQueue<string> FilesToMove;

    public SharedState()
    {
        DiscDrives = new List<DiscDrive>();
        MoveProcesses = new List<Process>();
        FilesToMove = new ConcurrentQueue<string>();
    }
}