﻿using IniParser.Model;
using System;
using ReactiveUI;
using VpdbAgent.Models;
using Splat;
using VpdbAgent.Application;

namespace VpdbAgent.PinballX.Models
{
	/// <summary>
	/// A "system" as read PinballX.
	/// 
	/// This comes live from PinballX.ini and resides only in memory. It's 
	/// updated when PinballX.ini changes.
	/// </summary>
	public class PinballXSystem
	{
		public string Name { get; set; }
		public bool Enabled { get; set; }
		public string WorkingPath { get; set; }
		public string TablePath { get; set; }
		public string Executable { get; set; }
		public string Parameters { get; set; }
		public Platform.PlatformType Type { get; set; }

		public string DatabasePath { get; set; }
		public string MediaPath { get; set; }

		public ReactiveList<Game> Games { get; private set; } = new ReactiveList<Game>();

		public PinballXSystem(KeyDataCollection data)
		{
			var systemType = data["SystemType"];
			if ("0".Equals(systemType)) {
				Type = Platform.PlatformType.Custom;
			} else if ("1".Equals(systemType)) {
				Type = Platform.PlatformType.VP;
			} else if ("2".Equals(systemType)) {
				Type = Platform.PlatformType.FP;
			}
			Name = data["Name"];

			SetByData(data);
		}

		public PinballXSystem(Platform.PlatformType type, KeyDataCollection data)
		{
			Type = type;
			switch (type) {
				case Platform.PlatformType.VP:
					Name = "Visual Pinball";
					break;
				case Platform.PlatformType.FP:
					Name = "Future Pinball";
					break;
				case Platform.PlatformType.Custom:
					Name = "Custom";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
			SetByData(data);
		}

		private void SetByData(KeyDataCollection data)
		{
			Enabled = "true".Equals(data["Enabled"], StringComparison.InvariantCultureIgnoreCase);
			WorkingPath = data["WorkingPath"];
			TablePath = data["TablePath"];
			Executable = data["Executable"];

			var settingsManager = Locator.Current.GetService<ISettingsManager>();
			DatabasePath = settingsManager.PbxFolder + @"\Databases\" + Name;
			MediaPath = settingsManager.PbxFolder + @"\Media\" + Name;
		}

		public override string ToString()
		{
			return $"[System] {Name} ({Games.Count})";
		}
	}
}
