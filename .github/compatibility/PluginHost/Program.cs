using System.IO;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using Newtonsoft.Json.Linq;
using NINA.Core.Model;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Sequencer.SequenceItem;

var pluginPath = Path.GetFullPath(args.Single());
var contextType = typeof(PluginLoader).GetNestedType("PluginAssemblyLoadContext", BindingFlags.NonPublic)
    ?? throw new InvalidOperationException("Production plugin load context was not found.");
var context = (AssemblyLoadContext)Activator.CreateInstance(contextType, "3.2 compatibility", pluginPath, false);
var assembly = context.LoadFromAssemblyPath(pluginPath);
foreach (var reference in assembly.GetReferencedAssemblies().Where(x => x.Name.StartsWith("NINA."))) {
    if (reference.Version != new Version(3, 2, 0, 9001)) throw new Exception("Fixture was not compiled against 3.2.0.");
}
using var catalog = new AssemblyCatalog(assembly);
using var container = new CompositionContainer(catalog);
var manifest = container.GetExportedValue<IPluginManifest>();
if (manifest.Identifier != "a57a9056-b0b2-46ce-bc9a-21c963508de3") throw new Exception("Manifest composition failed.");
await manifest.Initialize();
var instruction = container.GetExportedValue<ISequenceItem>();
if (instruction.Clone() is not ISequenceItem) throw new Exception("Sequence item contract did not unify.");
var progress = new SynchronousProgress();
await ((SequenceItem)instruction).Execute(progress, CancellationToken.None);
var value = JObject.Parse(progress.Status);
if ((double)value["RA"] != 180 || (double)value["Dec"] != -30) throw new Exception("Old compiled API calls failed.");
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try {
    await ((SequenceItem)instruction).Execute(progress, canceled.Token);
    throw new Exception("Cancellation contract failed.");
} catch (OperationCanceledException) { }
await manifest.Teardown();
Console.WriteLine("PASS: unmodified 3.2.0 plugin loaded through the production context, composed, executed, cloned and canceled.");

sealed class SynchronousProgress : IProgress<ApplicationStatus> {
    public string Status { get; private set; }
    public void Report(ApplicationStatus value) => Status = value.Status;
}
