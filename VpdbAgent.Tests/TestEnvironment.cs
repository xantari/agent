﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using IniParser;
using IniParser.Model;
using Moq;
using NLog;
using PusherClient;
using Splat;
using VpdbAgent.Application;
using VpdbAgent.Common.Filesystem;
using VpdbAgent.Models;
using VpdbAgent.PinballX;
using VpdbAgent.PinballX.Models;
using VpdbAgent.Tests.Mocks;
using VpdbAgent.ViewModels.Games;
using VpdbAgent.VisualPinball;
using VpdbAgent.Vpdb;
using VpdbAgent.Vpdb.Download;
using VpdbAgent.Vpdb.Models;
using IDependencyResolver = Splat.IDependencyResolver;
using ILogger = NLog.ILogger;
using Settings = VpdbAgent.Application.Settings;

namespace VpdbAgent.Tests
{
	public class TestEnvironment : IDisposable
	{
		public IDependencyResolver Locator => _locator;

		// constants
		public const string PinballXPath                  = @"C:\PinballX";
		public const string PinballXIniPath               = @"C:\PinballX\Config\PinballX.ini";
		public const string VisualPinballPath             = @"C:\Visual Pinball";
		public const string VisualPinballTablePath        = @"C:\Visual Pinball\Tables";
		public const string VisualPinballDatabasePath     = @"C:\PinballX\Databases\Visual Pinball";
		public const string VisualPinballDatabaseJsonPath = @"C:\PinballX\Databases\Visual Pinball\vpdb.json";
		public const string VisualPinballDatabaseXmlPath  = @"C:\PinballX\Databases\Visual Pinball\Visual Pinball.xml";
		public const string FuturePinballDatabaseJsonPath = @"C:\PinballX\Databases\Future Pinball\vpdb.json";

		// mocks
		public readonly Mock<IFile> File = new Mock<IFile>();
		public readonly Mock<IDirectory> Directory = new Mock<IDirectory>();
		public readonly Mock<ISettingsManager> SettingsManager = new Mock<ISettingsManager>();
		public readonly Mock<IFileSystemWatcher> FileSystemWatcher = new Mock<IFileSystemWatcher>();
		public readonly Mock<IMarshallManager> MarshallManager = new Mock<IMarshallManager>();
		public readonly Mock<IDatabaseManager> DatabaseManager = new Mock<IDatabaseManager>();
		public readonly Mock<IVpdbClient> VpdbClient = new Mock<IVpdbClient>();
		public readonly Mock<IVisualPinballManager> VisualPinballManager = new Mock<IVisualPinballManager>();
		public readonly Mock<IVpdbApi> VpdbApi = new Mock<IVpdbApi>();

		// observers
		public readonly Subject<string> PinballXIniWatcher = new Subject<string>();
		public readonly Subject<string> DatabaseWatcher = new Subject<string>();
		public readonly Subject<string> TableWatcher = new Subject<string>();
		public readonly Subject<Channel> UserChannel = new Subject<Channel>();
		public readonly Subject<VpdbUserFull> ApiAuthenticated = new Subject<VpdbUserFull>();

		private readonly ModernDependencyResolver _locator;


		public TestEnvironment(ILogger logger = null)
		{
			_locator = new ModernDependencyResolver();

			var abracadabra = Menu.Games[0];
			var mm = Menu.Games[1];

			//--------------------------------------------------------------------------
			// Fixtures
			//--------------------------------------------------------------------------


			// IMarshallManager
			MarshallManager.Setup(m => m.ParseIni(PinballXIniPath)).Returns(GetPinballXIni(_ini));
			MarshallManager.Setup(m => m.UnmarshallPlatformDatabase(VisualPinballDatabaseJsonPath)).Returns(new PlatformDatabase());
			MarshallManager.Setup(m => m.UnmarshallPlatformDatabase(FuturePinballDatabaseJsonPath)).Returns(new PlatformDatabase());
			MarshallManager.Setup(m => m.UnmarshallXml(Path.GetFileName(VisualPinballDatabaseXmlPath))).Returns(Menu);
			_locator.RegisterLazySingleton(() => MarshallManager.Object, typeof(IMarshallManager));

			// IFileSystemWatcher
			FileSystemWatcher.Setup(f => f.FileWatcher(PinballXIniPath)).Returns(PinballXIniWatcher);
			//FileSystemWatcher.Setup(f => f.DatabaseWatcher(It.IsAny<string>(), It.IsAny<IList<PinballXSystem>>())).Returns(DatabaseWatcher);
			//FileSystemWatcher.Setup(f => f.TablesWatcher(It.IsAny<IList<PinballXSystem>>())).Returns(TableWatcher);
			_locator.RegisterLazySingleton(() => FileSystemWatcher.Object, typeof(IFileSystemWatcher));

			// ISettingsManager
			SettingsManager.Setup(s => s.Settings).Returns(Settings);
			SettingsManager.Setup(s => s.ApiAuthenticated).Returns(ApiAuthenticated);
			_locator.RegisterLazySingleton(() => SettingsManager.Object, typeof(ISettingsManager));

			// IDatabaseManager
			_locator.RegisterLazySingleton(() => DatabaseManager.Object, typeof(IDatabaseManager));

			// VPDB API
			VpdbApi.Setup(api => api.GetReleasesBySize(TestVpdbApi.AbraCaDabraV20FileSize, GameItemViewModel.MatchThreshold)).Returns(TestVpdbApi.GetAbraCaDabraIdentify());
			VpdbApi.Setup(api => api.GetRelease(TestVpdbApi.AbraCaDabraReleaseId)).Returns(TestVpdbApi.GetAbraCaDabraDetails());

			// IVpdbClient
			VpdbClient.Setup(v => v.UserChannel).Returns(UserChannel);
			VpdbClient.Setup(v => v.Api).Returns(VpdbApi.Object);
			_locator.RegisterLazySingleton(() => VpdbClient.Object, typeof(IVpdbClient));

			// IVisualPinballManager
			_locator.RegisterLazySingleton(() => VisualPinballManager.Object, typeof(IVisualPinballManager));

			// IFile, IDirectory
			Directory.Setup(d => d.Exists(VisualPinballDatabasePath)).Returns(true);
			Directory.Setup(d => d.GetFiles(VisualPinballDatabasePath)).Returns(new[] { Path.GetFileName(VisualPinballDatabaseXmlPath) });
			File.Setup(f => f.Exists(Path.Combine(VisualPinballTablePath, abracadabra.Filename + ".vpt"))).Returns(true);
			File.Setup(f => f.FileSize(Path.Combine(VisualPinballTablePath, abracadabra.Filename + ".vpt"))).Returns(TestVpdbApi.AbraCaDabraV20FileSize);
			File.Setup(f => f.Exists(Path.Combine(VisualPinballTablePath, mm.Filename + ".vpx"))).Returns(true);
			File.Setup(f => f.FileSize(Path.Combine(VisualPinballTablePath, mm.Filename + ".vpx"))).Returns(10002);
			_locator.RegisterLazySingleton(() => File.Object, typeof(IFile));
			_locator.RegisterLazySingleton(() => Directory.Object, typeof(IDirectory));

			// Logging
			_locator.RegisterLazySingleton(() => logger ?? LogManager.GetCurrentClassLogger(), typeof(ILogger));
			_locator.RegisterLazySingleton(() => new Mock<CrashManager>(_locator.GetService<ILogger>()).Object, typeof(CrashManager));

			// IThreadManager
			_locator.RegisterLazySingleton(() => new TestThreadManager(), typeof(IThreadManager));


			//--------------------------------------------------------------------------
			// Real (non-mocked) services start here. Those are the tested ones.
			//--------------------------------------------------------------------------


			// IVersionManager
			_locator.RegisterLazySingleton(() => new VersionManager(
				_locator.GetService<CrashManager>(),
				_locator.GetService<ILogger>()
			), typeof(IVersionManager));

			// IMessageManager
			_locator.RegisterLazySingleton(() => new MessageManager(
				_locator.GetService<IDatabaseManager>(),
				_locator.GetService<CrashManager>()
			), typeof(IMessageManager));

			// IRealtimeManager
			_locator.RegisterLazySingleton(() => new RealtimeManager(
				_locator.GetService<IVpdbClient>(),
				_locator.GetService<ILogger>()
			), typeof(IRealtimeManager));

			// IJobManager
			_locator.RegisterLazySingleton(() => new JobManager(
				_locator.GetService<IDatabaseManager>(),
				_locator.GetService<IMessageManager>(),
				_locator.GetService<ILogger>(),
				_locator.GetService<CrashManager>()
			), typeof(IJobManager));

			// IMenuManager
			_locator.RegisterLazySingleton(() => new PinballX.MenuManager(
				_locator.GetService<IFileSystemWatcher>(),
				_locator.GetService<ISettingsManager>(),
				_locator.GetService<IMarshallManager>(),
				_locator.GetService<IThreadManager>(),
				_locator.GetService<IFile>(),
				_locator.GetService<IDirectory>(),
				_locator.GetService<ILogger>()), typeof(IMenuManager));

			// IPlatformManager
			_locator.RegisterLazySingleton(() => new Application.PlatformManager(
				_locator.GetService<IMenuManager>(),
				_locator.GetService<IThreadManager>(),
				_locator.GetService<ILogger>(),
				_locator
			), typeof(IPlatformManager));

			// IDownloadManager
			_locator.RegisterLazySingleton(() => new DownloadManager(
				_locator.GetService<IPlatformManager>(),
				_locator.GetService<IJobManager>(),
				_locator.GetService<IVpdbClient>(),
				_locator.GetService<ISettingsManager>(),
				_locator.GetService<IMessageManager>(),
				_locator.GetService<IDatabaseManager>(),
				_locator.GetService<ILogger>(),
				_locator.GetService<CrashManager>()
			), typeof(IDownloadManager));

			// IGameManager
			_locator.RegisterLazySingleton(() => new Application.GameManager(
				_locator.GetService<IMenuManager>(),
				_locator.GetService<IVpdbClient>(),
				_locator.GetService<ISettingsManager>(),
				_locator.GetService<IDownloadManager>(),
				_locator.GetService<IDatabaseManager>(),
				_locator.GetService<IVersionManager>(),
				_locator.GetService<IPlatformManager>(),
				_locator.GetService<IMessageManager>(),
				_locator.GetService<IRealtimeManager>(),
				_locator.GetService<IVisualPinballManager>(),
				_locator.GetService<IThreadManager>(),
				_locator.GetService<IFile>(),
				_locator.GetService<ILogger>()
			), typeof(IGameManager));
		}

		public Mock<T> Register<T>() where T : class
		{
			var mock = new Mock<T>();
			_locator.Register(() => mock.Object, typeof(T));
			return mock;
		}

		public Settings Settings = new Settings {
			PbxFolder = PinballXPath,
			ApiKey = "fake-test-api-key"
		};

		private readonly string[] _ini = {
			@"[VisualPinball]",
			@"Enabled = true",
			@"WorkingPath = " + VisualPinballPath,
			@"TablePath = " + VisualPinballTablePath,
			@"Executable = VPinball.exe",
			"Parameters = /play - \"[TABLEPATH]\\[TABLEFILE]\"",
		};

		public PinballXMenu Menu = new PinballXMenu() {
			Games = new List<PinballXGame> {
				new PinballXGame {
					Filename = "AbraCaDabra_FS_B2S",
					Description = "Abra Ca Dabra (Gottlieb 1975)",
					Manufacturer = "Gottlieb",
					Year = "1975",
					Type = "EM",
					Enabled = "true"
				},
				new PinballXGame {
					Filename = "MM_Supermod_WIP_v12.2",
					Description = "Medieval Madness (Williams 1997)",
					Manufacturer = "Williams",
					Year = "1997",
					Type = "SS",
					Enabled = "true"
				},
				new PinballXGame {
					Filename = "TOTAN_VP91x_3.2FS",
					Description = "Tales of the Arabian Nights (Williams 1996)",
					Manufacturer = "Williams",
					Year = "1996",
					Type = "SS",
					Enabled = "yes"
				},
				new PinballXGame {
					Filename = "The Shadow_FS_MOD_1.2",
					Description = "The Shadow (Bally 1994)",
					Manufacturer = "Bally",
					Year = "1994",
					Type = "SS",
					Enabled = "no"
				}
			}
		};

		public static IniData GetPinballXIni(string[] ini)
		{
			var byteArray = Encoding.UTF8.GetBytes(string.Join("\n", ini));
			var stream = new MemoryStream(byteArray);
			var parser = new FileIniDataParser();
			return parser.ReadData(new StreamReader(stream));
		}

		public void Dispose()
		{
			_locator.Dispose();
		}
	}
}
