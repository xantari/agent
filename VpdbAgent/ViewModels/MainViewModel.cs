﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using Squirrel;
using VpdbAgent.Application;
using VpdbAgent.ViewModels.Downloads;
using VpdbAgent.ViewModels.Games;
using VpdbAgent.ViewModels.Messages;
using VpdbAgent.ViewModels.Settings;
using VpdbAgent.Vpdb;
using VpdbAgent.Vpdb.Download;

namespace VpdbAgent.ViewModels
{
	public class MainViewModel : ReactiveObject, IRoutableViewModel
	{
		private static readonly Version AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

		// commands
		public ReactiveCommand<Unit, IRoutableViewModel> GotoSettings { get; protected set; }
		public ReactiveCommand<Unit, Unit> RestartApp;
		public ReactiveCommand<Unit, Unit> CloseUpdateNotice;

		// screen
		public IScreen HostScreen { get; protected set; }
		public string UrlPathSegment => "main";

		// props
		public string LoginStatus => _loginStatus.Value;
		public string VersionName => $"Version {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}";
		public bool ShowUpdateNotice { get { return _showUpdateNotice; } set { this.RaiseAndSetIfChanged(ref _showUpdateNotice, value); } }

		// privates
		private readonly ObservableAsPropertyHelper<string> _loginStatus;
		private bool _showUpdateNotice = false;

		// tabs
		public GamesViewModel Games { get; }
		public DownloadsViewModel Downloads { get; }
		public MessagesViewModel Messsages { get; }

		public MainViewModel(IScreen screen, ISettingsManager settingsManager, IVersionManager versionManager)
		{
			HostScreen = screen;

			Games = new GamesViewModel(Locator.Current);
			Downloads = new DownloadsViewModel(Locator.Current.GetService<IJobManager>());
			Messsages = new MessagesViewModel(Locator.Current.GetService<IDatabaseManager>(), Locator.Current.GetService<IMessageManager>());

			GotoSettings = ReactiveCommand.CreateFromObservable(() => screen.Router.Navigate.Execute(new SettingsViewModel(screen, settingsManager, versionManager, Locator.Current.GetService<IGameManager>())));

			// login status
			settingsManager.WhenAnyValue(sm => sm.AuthenticatedUser)
				.Select(u => u == null ? "Not logged." : $"Logged as {u.Name}")
				.ToProperty(this, x => x.LoginStatus, out _loginStatus);

			// show notice when new version arrives but hide when button was clicked
			versionManager.NewVersionAvailable
				.Where(release => release != null)
				.Subscribe(newRelease => {
					ShowUpdateNotice = true;
				});

			// update notice close button
			CloseUpdateNotice = ReactiveCommand.Create(() => { ShowUpdateNotice = false; });

			// restart button
			RestartApp = ReactiveCommand.Create(() => { UpdateManager.RestartApp(); });
		}
	}
}
