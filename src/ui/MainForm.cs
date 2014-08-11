﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
	internal partial class MainForm : Form
	{
		private IList<IProcessManager> processManagers;
		private VersionsManager phpVersions;
		private WaitingForm waitingForm;

		public MainForm(IList<IProcessManager> processManagers, VersionsManager phpVersions, WaitingForm waitingForm)
		{
			this.processManagers = processManagers;
			this.phpVersions = phpVersions;
			this.waitingForm = waitingForm;

			this.InitializeComponent();
			this.InitializeMainMenu();
		}

		private void InitializeMainMenu()
		{
			this.notifyIconMenu.Items.Clear();
			var activeVersion = this.phpVersions.GetActive();
			var versions = this.phpVersions.GetAvailable();

			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version);
				item.Checked = version.Equals(activeVersion);
				item.Click += (sender, args) => this.Attempt("PHP version to change", async () =>
				{
					await this.phpVersions.SwitchTo(version);
				});

				this.notifyIconMenu.Items.Add(item);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());

			foreach (var pm in this.processManagers)
			{
				var menu = new ProcessMenu(pm);
				menu.StartItem.Click += (sender, args) => this.Attempt(pm.Name + " to start", pm.Start);
				menu.StopItem.Click += (sender, args) => this.Attempt(pm.Name + " to stop", pm.Stop);
				menu.RestartItem.Click += (sender, args) => this.Attempt(pm.Name + " to restart", pm.Restart);
				this.notifyIconMenu.Items.Add(menu);
			}

			this.notifyIconMenu.Items.Add("Close", null, (sender, args) => Application.Exit());
		}

		private async void Attempt(string description, Func<Task> action)
		{
			this.notifyIconMenu.Enabled = false;
			this.waitingForm.description.Text = @"Waiting for " + description + @"...";
			this.waitingForm.Show();

			while (true)
			{
				try
				{
					await action();
					break;
				}
				catch (ProcessException ex)
				{
					var msg = "Unable to " + ex.Operation + " " + ex.Name + ".";
					var dialogResult = MessageBox.Show(msg, "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (dialogResult != DialogResult.Retry) break;
				}
			}
			
			this.InitializeMainMenu();
			this.waitingForm.Hide();
			this.notifyIconMenu.Enabled = true;
		}
	}
}
