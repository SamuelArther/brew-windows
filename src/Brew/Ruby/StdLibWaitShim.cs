namespace Brew.Ruby;

// Stand-in for the process-API branch's Interp.WaitFor (RubyThreads.cs), which also releases the
// interpreter lock while waiting. Delete this file when merging with that branch.
public sealed partial class Interp
{
    // Calls ready(sliceMs) until it answers true; each call may block up to sliceMs. Timeout.timeout
    // can interrupt between slices.
    public void WaitFor(Func<int, bool> ready)
    {
        while (!ready(100)) CheckDeadline();
    }
}
