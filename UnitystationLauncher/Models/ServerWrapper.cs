using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Serilog;
using System.Diagnostics;
using System.Reactive.Subjects;

namespace UnitystationLauncher.Models
{
    public class ServerWrapper : Server
    {
        private AuthManager authManager;
        public ServerWrapper(Server server, AuthManager authManager)
        {
            this.authManager = authManager;
            ServerName = server.ServerName;
            ForkName = server.ForkName;
            BuildVersion = server.BuildVersion;
            CurrentMap = server.CurrentMap;
            GameMode = server.GameMode;
            IngameTime = server.IngameTime;
            PlayerCount = server.PlayerCount;
            ServerIP = server.ServerIP;
            ServerPort = server.ServerPort;
            WinDownload = server.WinDownload;
            OSXDownload = server.OSXDownload;
            LinuxDownload = server.LinuxDownload;

            if (!Directory.Exists(Config.InstallationsPath))
            {
                Directory.CreateDirectory(Config.InstallationsPath);
            }

            var canDownload = Config.InstallationChanges
                .Select(e => !Directory.Exists(InstallationPath));

            var canStart = Config.InstallationChanges
                .Select(e => 
                    Directory.Exists(InstallationPath) && 
                    Installation.FindExecutable(InstallationPath) != null);

            Download = ReactiveCommand.Create(DownloadAsync, canDownload);
            Start = ReactiveCommand.Create(StartImp, canStart);
        }

        public Subject<int> Progress { get; set; } = new Subject<int>();

        public ReactiveCommand<Unit, Unit> Download { get; }

        public ReactiveCommand<Unit, Unit> Start { get; }

        public async void DownloadAsync()
        {
            Log.Information("Download requested...");
            Log.Information("Installation path: \"{Path}\"", InstallationPath);

            if (Directory.Exists(InstallationPath))
            {
                Log.Information("Installation path already occupied");
                return;
            }

            Log.Information("Download URL: \"{URL}\"", DownloadUrl);

            if (DownloadUrl is null)
            {
                throw new Exception("OS download is null");
            }

            Log.Information("Download started...");
            var webRequest = WebRequest.Create(DownloadUrl);
            var webResponse = await webRequest.GetResponseAsync();
            var responseStream = webResponse.GetResponseStream();
            Log.Information("Download connection established");
            using var progStream = new ProgressStream(responseStream);
            var length = webResponse.ContentLength;
            progStream.Progress
                .Select(p => (int)(p * 100 / length))
                .DistinctUntilChanged()
                .Subscribe(p => {
                    Progress.OnNext(p);
                    Log.Information("Progress: {prog}", p);
                    });

            await Task.Run(() =>
            {
                Log.Information("Extracting...");
                var archive = new ZipArchive(progStream);
                archive.ExtractToDirectory(InstallationPath);
                Log.Information("Download completed");
            });
        }

        private void StartImp()
        {
            var exe = Installation.FindExecutable(InstallationPath);
            if(exe != null)
            {
                var process = new Process();
                process.StartInfo.FileName = exe;
                process.StartInfo.Arguments =
                    $"--server {ServerIP} --port {ServerPort} --refreshtoken {authManager.CurrentRefreshToken} --uid {authManager.UID}";
                process.Start();
            }
        }
    }
}