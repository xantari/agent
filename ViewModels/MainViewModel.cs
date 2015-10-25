﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using VpdbAgent.Application;
using VpdbAgent.ViewModels.Downloads;
using VpdbAgent.ViewModels.Games;
using VpdbAgent.Vpdb;

namespace VpdbAgent.ViewModels
{
	public class MainViewModel : ReactiveObject, IRoutableViewModel
	{
		// screen
		public IScreen HostScreen { get; protected set; }
		public string UrlPathSegment => "main";

		// tabs
		public GamesViewModel Games { get; }
		public DownloadsViewModel Downloads { get; }

		public MainViewModel(IScreen screen)
		{
			HostScreen = screen;

			Games = new GamesViewModel(
				Locator.Current.GetService<IGameManager>(),
				Locator.Current.GetService<IVpdbClient>());
			Downloads = new DownloadsViewModel();
		}
	}
}
