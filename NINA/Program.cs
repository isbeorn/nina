using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.SignalR;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NINA.Profile;
using System.Linq;
using NINA.Utility;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.ViewModel.Interfaces;
using NINA.Plugin.Interfaces;

// Disable file watching to prevent inotify limit issues on Linux
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

// Now build the full application with all services
var builder = WebApplication.CreateBuilder(args);

// Add full services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", builder => {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .DisallowCredentials(); // SignalR with * origin cannot use credentials
    });
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<SignalRNotificationBroadcaster>();
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

// Command line
var allArgs = Environment.GetCommandLineArgs();
// Filter out ASP.NET Core built-in options to avoid parsing errors in CommandLineOptions
var customArgs = allArgs.Where(arg => !arg.StartsWith("--urls") &&
                                     !arg.StartsWith("--environment") &&
                                     !arg.StartsWith("--contentroot") &&
                                     !arg.StartsWith("--applicationName") &&
                                     !arg.StartsWith("--webroot") &&
                                     !arg.StartsWith("--webconfig")).ToArray();
CommandLineOptions commandLineOptions = new(customArgs);
if (commandLineOptions.HasErrors) {
    Environment.Exit(-1);
}
if (commandLineOptions.Debug) {
    CoreUtil.DebugMode = commandLineOptions.Debug;
}

var startWithProfileId = commandLineOptions.ProfileId;

// Profile
ProfileService profileService = new();
if (!profileService.TryLoad(startWithProfileId)) {
    Environment.Exit(-1);
}

Logger.SetLogLevel(profileService.ActiveProfile.ApplicationSettings.LogLevel);

if (customArgs.Length > 0) {
    Logger.Info("Launched with command line arguments: " + string.Join(" ", customArgs));
}

// Register application services NOW that we have an active profile
new IoCBindings(profileService, commandLineOptions).Load(builder.Services);

var app = builder.Build();

// Now that profile is ready, instantiate services and Controller
_ = app.Services.GetService<IImageSaveController>();
_ = app.Services.GetService<IImagingVM>();
_ = app.Services.GetService<IApplicationVM>();
_ = app.Services.GetService<IEquipmentVM>();
_ = app.Services.GetService<ISkyAtlasVM>();
_ = app.Services.GetService<ISequenceNavigationVM>();
_ = app.Services.GetService<IFramingAssistantVM>();
_ = app.Services.GetService<IFlatWizardVM>();
_ = app.Services.GetService<IOptionsVM>();
_ = app.Services.GetService<IApplicationDeviceConnectionVM>();
_ = app.Services.GetService<IVersionCheckVM>();
_ = app.Services.GetService<IApplicationStatusVM>();
_ = app.Services.GetService<IImageHistoryVM>();
_ = app.Services.GetService<IPluginsVM>();
_ = app.Services.GetService<GlobalObjects>();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS for SignalR connections from other ports/origins
app.UseCors("AllowAll");

// Start web server to expose profile endpoints
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Set up the notification broadcaster to use SignalR
var signalRBroadcaster = app.Services.GetRequiredService<SignalRNotificationBroadcaster>();
Notification.Broadcaster = async (message) => {
    await signalRBroadcaster.BroadcastNotificationAsync(message);
};

Logger.Info("Server fully initialized and running.");

// Register shutdown handler
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => {
    Logger.Info("Application shutting down...");

    // Call ApplicationVM.Closing() to properly disconnect equipment and cleanup
    var applicationVM = app.Services.GetService<IApplicationVM>();
    if (applicationVM != null) {
        try {
            // Use reflection to call the private Closing method
            var closingMethod = applicationVM.GetType().GetMethod("Closing",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            closingMethod?.Invoke(applicationVM, null);
        } catch (Exception ex) {
            Logger.Error("Failed to call ApplicationVM.Closing()", ex);
        }
    }
});

// Keep server running
app.Run();