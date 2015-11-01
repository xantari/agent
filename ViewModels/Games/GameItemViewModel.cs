﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using NLog;
using ReactiveUI;
using Splat;
using VpdbAgent.Application;
using VpdbAgent.Vpdb;
using VpdbAgent.Vpdb.Models;
using Game = VpdbAgent.Models.Game;

namespace VpdbAgent.ViewModels.Games
{
	public class GameItemViewModel : ReactiveObject
	{
		private const long MatchThreshold = 512;

		// deps
		private static readonly Logger Logger = Locator.CurrentMutable.GetService<Logger>();
		private static readonly IVpdbClient VpdbClient = Locator.CurrentMutable.GetService<IVpdbClient>();
		private static readonly IGameManager GameManager = Locator.CurrentMutable.GetService<IGameManager>();

		// commands
		public ReactiveCommand<List<Release>> IdentifyRelease { get; protected set; }
		public ReactiveCommand<object> CloseResults { get; protected set; } = ReactiveCommand.Create();

		// data
		public Game Game { get; }

		// needed for filters
		private bool _isVisible = true;
		public bool IsVisible { get { return _isVisible; } set { this.RaiseAndSetIfChanged(ref _isVisible, value); } }

		// release search results
		private IEnumerable<GameResultItemViewModel> _identifiedReleases;
		public IEnumerable<GameResultItemViewModel> IdentifiedReleases { get { return _identifiedReleases; } set { this.RaiseAndSetIfChanged(ref _identifiedReleases, value); } }

		// statuses
		public bool IsExecuting => _isExecuting.Value;
		public bool HasExecuted { get { return _hasExecuted; } set { this.RaiseAndSetIfChanged(ref _hasExecuted, value); } }
		public bool HasResults { get { return _hasResults; } set { this.RaiseAndSetIfChanged(ref _hasResults, value); } }
		public bool ShowIdentifyButton { get { return _showIdentifyButton; } set { this.RaiseAndSetIfChanged(ref _showIdentifyButton, value); } }
		private readonly ObservableAsPropertyHelper<bool> _isExecuting;
		private bool _hasExecuted;
		private bool _hasResults;
		private bool _showIdentifyButton;

		public GameItemViewModel(Game game)
		{
			Game = game;

			// release identify
			IdentifyRelease = ReactiveCommand.CreateAsyncObservable(_ => VpdbClient.Api.GetReleasesBySize(game.FileSize, MatchThreshold).SubscribeOn(Scheduler.Default));
			IdentifyRelease.Select(releases => releases
				.Select(release => new {release, release.Versions})
				.SelectMany(x => x.Versions.Select(version => new {x.release, version, version.Files}))
				.SelectMany(x => x.Files.Select(file => new GameResultItemViewModel(game, x.release, x.version, file, CloseResults)))
			).Subscribe(x => {

				var releases = x as GameResultItemViewModel[] ?? x.ToArray();
				var numMatches = 0;
				GameResultItemViewModel match = null;

				// ReSharper disable once LoopCanBePartlyConvertedToQuery
				foreach (var vm in releases) {
					if (game.Filename.Equals(vm.File.Reference.Name) && game.FileSize == vm.File.Reference.Bytes) {
						numMatches++;
						match = vm;
					}
				}

				// if file name and file size are identical, directly match.
				if (numMatches == 1 && match != null) {
					GameManager.LinkRelease(match.Game, match.Release, match.File.Reference.Id);

				} else {
					IdentifiedReleases = releases;
					HasExecuted = true;
				}

			});

			// handle errors
			IdentifyRelease.ThrownExceptions.Subscribe(e => { Logger.Error(e, "Error matching game."); });

			// spinner
			IdentifyRelease.IsExecuting.ToProperty(this, vm => vm.IsExecuting, out _isExecuting);

			IdentifyRelease.Select(r => r.Count > 0).Subscribe(hasResults => { HasResults = hasResults; });

			// close button
			CloseResults.Subscribe(_ => { HasExecuted = false; });


			// identify button visibility
			Changed.Subscribe(x => {
				if (x.PropertyName.Equals("HasExecuted")) {
					UpdateStatus();
				}
			});
			Game.Changed.Subscribe(x => {
				if (x.PropertyName.Equals("HasRelease")) {
					UpdateStatus();
				}
			});
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			ShowIdentifyButton = !HasExecuted && !Game.HasRelease;
		}

		public override string ToString()
		{
			return $"[GameViewModel] {Game}";
		}
	}
}
