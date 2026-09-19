using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Sequencer.SequenceItem;

[assembly: Guid("a57a9056-b0b2-46ce-bc9a-21c963508de3")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]

namespace NINA.CompatibilityFixture;

[Export(typeof(IPluginManifest))]
public sealed class Manifest : PluginBase { }

[Export(typeof(ISequenceItem))]
public sealed class Instruction : SequenceItem {
    public Instruction() { Name = "3.2 binary compatibility"; }
    public override object Clone() => new Instruction();

    public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        var coordinates = new Coordinates(12, -30, Epoch.J2000, Coordinates.RAType.Hours);
        var json = JsonConvert.SerializeObject(new { RA = coordinates.RADegrees, Dec = coordinates.Dec });
        progress.Report(new ApplicationStatus { Status = json });
        return Task.CompletedTask;
    }
}
