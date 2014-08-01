﻿using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private Model model;
		private ToolStripMenuItem activeVersion;
		private ToolStripItem apacheStart;
		private ToolStripItem apacheStop;
		private ToolStripItem apacheRestart;
		private WaitingForm waitingForm;

		public MainForm()
		{
			InitializeComponent();
			this.notifyIcon.Icon = this.Icon;
			this.model = new Model(Properties.Settings.Default.PhpDir, Properties.Settings.Default.HttpServerServiceName);
			this.waitingForm = new WaitingForm();
			this.initMainMenu();
		}

		private void initMainMenu()
		{
			var activeVersion = this.model.ActiveVersion;
			var versions = this.model.AvailableVersions;

			this.notifyIconMenu.Items.Clear();
			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version.ToString(), null, new EventHandler(version_Clicked));
				item.Tag = version;

				if (version.Equals(activeVersion))
				{
					this.setActiveItem(item);
				}

				this.notifyIconMenu.Items.Add(item);
			}
			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add(this.getApacheMenu());
			this.notifyIconMenu.Items.Add("Refresh", null, new EventHandler(refresh_Clicked));
			this.notifyIconMenu.Items.Add("Close", null, new EventHandler(close_Click));
		}

		private ToolStripMenuItem getApacheMenu()
		{
			var menu = new ToolStripMenuItem("Apache");
			this.apacheStart = menu.DropDownItems.Add("Start", null, new EventHandler(apacheStart_Clicked));
			this.apacheStop = menu.DropDownItems.Add("Stop", null, new EventHandler(apacheStop_Clicked));
			this.apacheRestart = menu.DropDownItems.Add("Restart", null, new EventHandler(apacheRestart_Clicked));
			this.updateApacheMenuState();

			return menu;
		}

		private void updateApacheMenuState()
		{
			var running = this.model.IsHttpServerRunning;
			this.apacheStart.Enabled = !running;
			this.apacheStop.Enabled = running;
			this.apacheRestart.Enabled = running;
		}

		private void setActiveItem(ToolStripMenuItem item)
		{
			if (this.activeVersion != null) this.activeVersion.Checked = false;
			this.activeVersion = item;
			this.activeVersion.Checked = true;
		}

		private async void attempt(Func<Task> action)
		{
			this.notifyIconMenu.Enabled = false;
			this.waitingForm.Show();

			try
			{
				await action();
			}
			catch (ApacheStartFailedException)
			{
				this.waitingForm.Hide();
				var button = MessageBox.Show("Unable to start Apache service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == System.Windows.Forms.DialogResult.Retry) attempt(action);
			}
			catch (ApacheStopFailedException)
			{
				this.waitingForm.Hide();
				var button = MessageBox.Show("Unable to stop Apache service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == System.Windows.Forms.DialogResult.Retry) attempt(action);
			}
			finally
			{
				this.waitingForm.Hide();
				this.updateApacheMenuState();
				this.notifyIconMenu.Enabled = true;
			}
		}

		private void version_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			var version = (Version)menuItem.Tag;

			attempt(async () =>
			{
				await this.model.SwitchTo(version);
				this.setActiveItem(menuItem);
			});
		}

		private void apacheStart_Clicked(object sender, EventArgs e)
		{
			attempt(async () =>
			{
				await this.model.StartApache();
			});
		}

		private void apacheStop_Clicked(object sender, EventArgs e)
		{
			attempt(async () =>
			{
				await this.model.StopApache();
			});
		}

		private void apacheRestart_Clicked(object sender, EventArgs e)
		{
			attempt(async () =>
			{
				await this.model.StartApache();
				await this.model.StopApache();
			});
		}

		private void refresh_Clicked(object sender, EventArgs e)
		{
			this.initMainMenu();
		}

		private void close_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
