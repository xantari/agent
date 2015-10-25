﻿using System;
using System.Windows.Forms;
using NLog;
using ReactiveUI;
using VpdbAgent.Application;

namespace VpdbAgent.ViewModels.Settings
{
	public class SettingsViewModel : ReactiveObject, IRoutableViewModel
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		// screen
		public IScreen HostScreen { get; protected set; }
		public string UrlPathSegment => "settings";

		// deps
		private readonly ISettingsManager _settingsManager;

		// commands
		public ReactiveCommand<object> ChooseFolder { get; } = ReactiveCommand.Create();
		public ReactiveCommand<object> SaveSettings { get; } = ReactiveCommand.Create();
		public ReactiveCommand<object> CloseSettings { get; } = ReactiveCommand.Create();

		private string _apiKey;
		private string _pbxFolder;
		private string _endpoint;
		private string _authUser;
		private string _authPass;

		public SettingsViewModel(IScreen screen, ISettingsManager settingsManager)
		{
			HostScreen = screen;
			_settingsManager = settingsManager;

			ApiKey = _settingsManager.ApiKey;
			AuthUser = _settingsManager.AuthUser;
			AuthPass = _settingsManager.AuthPass;
			Endpoint = _settingsManager.Endpoint;
			PbxFolder = _settingsManager.PbxFolder;

			ChooseFolder.Subscribe(_ => OpenFolderDialog());
			SaveSettings.Subscribe(_ => Save());

			CloseSettings.InvokeCommand(HostScreen.Router, r => r.NavigateBack);
		}

		private void OpenFolderDialog()
		{
			var dialog = new FolderBrowserDialog {
				ShowNewFolderButton = false
			};

			if (!string.IsNullOrWhiteSpace(PbxFolder)) {
				dialog.SelectedPath = PbxFolder;
			}
			var result = dialog.ShowDialog();
			PbxFolder = result == DialogResult.OK ? dialog.SelectedPath : string.Empty;
			Logger.Info("PinballX folder set to {0}.", PbxFolder);
		}

		private void Save()
		{
			_settingsManager.ApiKey = _apiKey;
			_settingsManager.AuthUser = _authUser;
			_settingsManager.AuthPass = _authPass;
			_settingsManager.Endpoint = _endpoint;
			_settingsManager.PbxFolder = _pbxFolder;

			var errors = _settingsManager.Validate();
			if (errors.Count == 0) {
				_settingsManager.Save();
				Logger.Info("Settings saved.");

			} else {

				// TODO properly display error
				foreach (var field in errors.Keys) {
					Logger.Error("Settings validation error for field {0}: {1}", field, errors[field]);
				}
			}
		}

		public string ApiKey
		{
			get { return _apiKey; }
			set { this.RaiseAndSetIfChanged(ref _apiKey, value); }
		}
		public string AuthUser
		{
			get { return _authUser; }
			set { this.RaiseAndSetIfChanged(ref _authUser, value); }
		}
		public string AuthPass
		{
			get { return _authPass; }
			set { this.RaiseAndSetIfChanged(ref _authPass, value); }
		}
		public string Endpoint
		{
			get { return _endpoint; }
			set { this.RaiseAndSetIfChanged(ref _endpoint, value); }
		}
		public string PbxFolder
		{
			get { return this._pbxFolder; }
			set { this.RaiseAndSetIfChanged(ref _pbxFolder, value); }
		}

		public string PbxFolderLabel => string.IsNullOrEmpty(_pbxFolder) ? "No folder set." : "Location:";
	}
}
