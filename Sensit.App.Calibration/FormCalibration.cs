﻿using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using Sensit.TestSDK.Forms;
using Sensit.TestSDK.Interfaces;
using Sensit.TestSDK.Settings;
using Sensit.TestSDK.Utilities;

namespace Sensit.App.Calibration
{
	/// <summary>
	/// GUI operations and settings access are handled here.
	/// </summary>
	public partial class FormCalibration : Form
	{
		#region Constants

		// These constants specify the order that controls appear in the 
		// columns of tableLayoutPanelDevicesUnderTest.
		private const int COLUMN_DEVICES_NAME = 0;
		private const int COLUMN_DEVICES_TYPE = 1;
		private const int COLUMN_DEVICES_PORT = 2;
		private const int COLUMN_EVENTS_DEVICE = 0;
		private const int COLUMN_EVENTS_VARIABLE = 1;
		private const int COLUMN_EVENTS_VALUE = 2;
		private const int COLUMN_EVENTS_DURATION = 3;
		private const int COLUMN_EVENTS_STATUS = 4;

		#endregion

		#region Fields

		// allow the form to wait for tests to cancel/complete before closing application
		private bool _closeAfterTest = false;

		// test equipment
		private Equipment _equipment;

		// tests
		private Test _test;

		// test settings
		TestSetting _testSettings;

		#endregion

		#region Constructor

		public FormCalibration()
		{
			// Initialize the form.
			InitializeComponent();

			// Add version string to title bar.
			if (ApplicationDeployment.IsNetworkDeployed)
			{
				Text += " " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
			}

			// List the control devices in the form, and select the first one.
			List<Type> deviceTypes = Utilities.FindClasses(typeof(IControlDevice));
			foreach (Type deviceType in deviceTypes)
			{
				comboBoxDeviceType.Items.Add(deviceType.GetDescription());
			}
			comboBoxDeviceType.SelectedIndex = 0;

			// List the variable types in the form, and select the first one.
			foreach (VariableType v in (VariableType[])Enum.GetValues(typeof(VariableType)))
			{
				comboBoxEventVariable.Items.Add(v.GetDescription());
			}
			comboBoxEventVariable.SelectedIndex = 0;

			// Select the most recently used termination option.
			radioButtonRepeatYes.Checked = Properties.Settings.Default.Repeat;
			radioButtonRepeatNo.Checked = !Properties.Settings.Default.Repeat;
		}

		#endregion

		#region Test

		/// <summary>
		/// Enable/disable user controls based on whether is test is being run.
		/// </summary>
		/// <param name="testInProgress">true if test is in progress; false otherwise</param>
		private void SetControlEnable(bool testInProgress)
		{
			// Settings group boxes.
			groupBoxDevices.Enabled = !testInProgress;
			groupBoxEvents.Enabled = !testInProgress;
			groupBoxLog.Enabled = !testInProgress;

			// Repeat controls.
			radioButtonRepeatNo.Enabled = testInProgress;
			radioButtonRepeatYes.Enabled = !testInProgress;

			// Start, stop buttons.
			buttonStart.Enabled = !testInProgress;
			buttonStop.Enabled = testInProgress;

			// Menu items.
			startToolStripMenuItem.Enabled = !testInProgress;
			pauseToolStripMenuItem.Enabled = testInProgress;
			stopToolStripMenuItem.Enabled = testInProgress;
			newToolStripMenuItem.Enabled = !testInProgress;
			openToolStripMenuItem.Enabled = !testInProgress;
			saveToolStripMenuItem.Enabled = !testInProgress;
		}

		/// <summary>
		/// When "Start" button is clicked, fetch settings, create equipment/test, start test.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ButtonStart_Click(object sender, EventArgs e)
		{
			try
			{
				// Disable most of the user controls.
				SetControlEnable(true);

				// Create object for the equipment.
				_equipment = new Equipment();

				// TODO:  Add devices to settings file.
				// foreach (row in tableLayoutPanelDevices)
				{
					CheckBox checkBoxDeviceName = tableLayoutPanelDevices.GetControlFromPosition(COLUMN_DEVICES_NAME, 1) as CheckBox;
					ComboBox comboBoxDeviceType = tableLayoutPanelDevices.GetControlFromPosition(COLUMN_DEVICES_TYPE, 1) as ComboBox;
					ComboBox comboBoxDevicePort = tableLayoutPanelDevices.GetControlFromPosition(COLUMN_DEVICES_PORT, 1) as ComboBox;
					//_equipment.Add(...);
				}

				// Create test object and link its actions to actions on this form.
				// https://syncor.blogspot.com/2010/11/passing-getter-and-setter-of-c-property.html
				_test = new Test(_testSettings, _equipment, textBoxLogFilename.Text)
				{
					Finished = TestFinished,
					UpdateProgress = TestUpdate,
					Repeat = Properties.Settings.Default.Repeat
				};

				// Start the test.
				_test.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
			}
		}

		/// <summary>
		/// When the "Stop" button is clicked, run the Stop action.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ButtonStop_Click(object sender, EventArgs e)
		{
			ConfirmAbort();
		}

		/// <summary>
		/// Before exiting, check the user's wishes and safely end testing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FormCalibration_FormClosing(object sender, FormClosingEventArgs e)
		{
			// If a test exists and is running...
			if ((_test != null) && (_test.IsBusy()))
			{
				// Cancel application shutdown.
				e.Cancel = true;

				// If the user chooses to abort the test...
				if (ConfirmAbort() == true)
				{
					// Remember to close the application after the test finishes.
					_closeAfterTest = true;
				}
			}

			// Save settings.
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// Before exiting, check the user's wishes and safely end testing.
		/// </summary>
		/// <returns>true if we're quitting; false if cancelled</returns>
		private bool ConfirmAbort()
		{
			DialogResult result = DialogResult.OK;  // whether to quit or not

			// If a test exists and is running...
			if ((_test != null) && (_test.IsBusy()))
			{
				// Ask the user if they really want to stop the test.
				result = MessageBox.Show("Abort the test?", "Abort", MessageBoxButtons.OKCancel);
			}

			// If we're quitting, cancel a test (don't update GUI; the "TestFinished" method will do that).
			if (result == DialogResult.OK)
			{
				try
				{
					_test?.Stop();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Could not stop test." + Environment.NewLine + ex.Message, "Error");
				}
			}

			// Return whether or not we're stopping the test.
			return (result == DialogResult.OK);
		}

		/// <summary>
		/// When the "Repeat" selection is changed, remember the selection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RadioButtonRepeat_CheckedChanged(object sender, EventArgs e)
		{
			// Do stuff only if the radio button is checked.
			// (Otherwise the actions will run twice.)
			if (((RadioButton)sender).Checked)
			{
				// If the "Open" radio button has been checked...
				if (((RadioButton)sender) == radioButtonRepeatYes)
				{
					Properties.Settings.Default.Repeat = true;
				}
				else if (((RadioButton)sender) == radioButtonRepeatNo)
				{
					Properties.Settings.Default.Repeat = false;
				}
			}
		}

		private void TextBoxLogFilename_TextChanged(object sender, EventArgs e)
		{
			// Remember logfile name.
			Properties.Settings.Default.Logfile = textBoxLogFilename.Text;
		}

		#endregion

		#region File Menu

		private void SupportToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo("https://github.com/SensitTechnologies/TestSuite/wiki");
				Process.Start(processStartInfo);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + Environment.NewLine +
					"I was trying to navigate to: https://github.com/SensitTechnologies/TestSuite/wiki.", "ERROR");
			}
		}

		private void NewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Remove all devices.
			Utilities.TableLayoutPanelClear(tableLayoutPanelDevices);

			// Remove all events.
			Utilities.TableLayoutPanelClear(tableLayoutPanelEvents);
		}

		private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Allow the user to select a filename.
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Filter = "XML-File|*.xml";
			fileDialog.Title = "Open test settings file";
			fileDialog.ShowDialog();

			// If a valid filename has been selected...
			if (!string.IsNullOrEmpty(fileDialog.FileName))
			{
				// TODO:  Load settings from file.

				// Save to XML file.
				Settings.Save(_testSettings, fileDialog.FileName);
			}

			// Clean up.
			fileDialog.Dispose();
		}

		private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Allow the user to select a filename.
			SaveFileDialog fileDialog = new SaveFileDialog();
			fileDialog.Filter = "XML-File|*.xml";
			fileDialog.Title = "Save test settings file";
			fileDialog.ShowDialog();

			// If a valid filename has been selected...
			if (!string.IsNullOrEmpty(fileDialog.FileName))
			{
				// TODO:  Save settings to file.

				// Save to XML file.
				Settings.Save(_testSettings, fileDialog.FileName);
			}

			// Clean up.
			fileDialog.Dispose();
		}

		/// <summary>
		/// When File --> Exit menu item is clicked, close the application.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// This will invoke the "FormClosing" action, so nothing else to do here.

			// Exit the application.
			Application.Exit();
		}

		/// <summary>
		/// When Test --> Pause menu item is clicked, pause the test.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_test?.Pause();
		}

		#endregion

		#region Help Menu

		/// <summary>
		/// When the user clicks Help --> About, show an about box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Show the repository where this program can be found.
			// For the sake of future engineers.
			string description = "Source code can be found at:" + Environment.NewLine
				+ "https://github.com/SensitTechnologies/TestSuite";

			// Create an about box.
			using (FormAbout formAbout = new FormAbout(description))
			{

				// Show the about box.
				// ShowDialog() disables interaction with the app's other forms.
				// Show() does not.
				formAbout.ShowDialog();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Update the form with the test's status.
		/// </summary>
		/// <param name="message"></param>
		public void TestUpdate(int percent, string message)
		{
			// Update the progress bar.
			toolStripProgressBar1.Value = percent;

			// Update the status message.
			toolStripStatusLabel1.Text = message;

			// Update variables in "Status" tab.
			UpdateVariable(groupBoxMassFlow, textBoxMassFlowSetpoint, textBoxMassFlowValue, VariableType.MassFlow);
			UpdateVariable(groupBoxVolumeFlow, textBoxVolumeFlowSetpoint, textBoxVolumeFlowValue, VariableType.VolumeFlow);
			UpdateVariable(groupBoxVelocity, textBoxVelocitySetpoint, textBoxVelocityValue, VariableType.Velocity);
			UpdateVariable(groupBoxPressure, textBoxPressureSetpoint, textBoxPressureValue, VariableType.Pressure);
			UpdateVariable(groupBoxTemperature, textBoxTempSetpoint, textBoxTempValue, VariableType.Temperature);
			UpdateVariable(groupBoxCurrent, textBoxCurrentSetpoint, textBoxCurrentValue, VariableType.Current);
			UpdateVariable(groupBoxVoltage, textBoxVoltageSetpoint, textBoxVoltageValue, VariableType.Voltage);
		}

		private void UpdateVariable(GroupBox groupBox, TextBox setpoint, TextBox value, VariableType variableType)
		{
			if (_test.Variables.ContainsKey(variableType))
			{
				groupBox.Visible = true;
				setpoint.Text = _test.Variables[variableType].Setpoint.ToString("G4");
				value.Text = _test.Variables[variableType].Value.ToString("G4");
			}
			else
			{
				groupBox.Visible = false;
			}
		}

		/// <summary>
		/// Reset the form after a test is completed or cancelled.
		/// </summary>
		public void TestFinished()
		{
			// Enable most of the controls.
			SetControlEnable(false);

			// If requested, close the application.
			if (_closeAfterTest)
			{
				Application.Exit();
			}

			// Update the progress bar.
			toolStripProgressBar1.Value = 0;

			// Update the status message.
			toolStripStatusLabel1.Text = "Ready...";
		}

		#endregion

		private void ButtonDeviceAdd_Click(object sender, EventArgs e)
		{
			// Stop the GUI from looking weird while we update it.
			tableLayoutPanelDevices.SuspendLayout();

			// Add a new row to the table layout panel.
			tableLayoutPanelDevices.RowCount++;
			tableLayoutPanelDevices.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			// Add a checkbox with the device's name.
			tableLayoutPanelDevices.Controls.Add(new CheckBox
			{
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				Dock = DockStyle.None,
				Text = textBoxDeviceName.Text,
			}, COLUMN_DEVICES_NAME, tableLayoutPanelDevices.RowCount - 1);

			// Add the device type.
			tableLayoutPanelDevices.Controls.Add(new Label
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				AutoSize = true,
				Dock = DockStyle.None,
				Text = comboBoxDeviceType.Text,
			}, COLUMN_DEVICES_TYPE, tableLayoutPanelDevices.RowCount - 1);

			// Add a comboBox for serial port.
			ComboBox comboBox = new ComboBox
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				Dock = DockStyle.None,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			comboBox.Items.AddRange(SerialPort.GetPortNames());
			tableLayoutPanelDevices.Controls.Add(comboBox, COLUMN_DEVICES_PORT, tableLayoutPanelDevices.RowCount - 1);

			// Make the GUI act normally again.
			tableLayoutPanelDevices.ResumeLayout();

			// Add the device to the list of available devices on the "Events" tab.
			comboBoxEventDevice.Items.Add(textBoxDeviceName.Text);
		}

		private void ButtonEventAdd_Click(object sender, EventArgs e)
		{
			// Stop the GUI from looking weird while we update it.
			tableLayoutPanelEvents.SuspendLayout();

			// Add a new row to the table layout panel.
			tableLayoutPanelEvents.RowCount++;
			tableLayoutPanelEvents.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			// Add a checkbox with the event's name.
			tableLayoutPanelEvents.Controls.Add(new CheckBox
			{
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				Dock = DockStyle.None,
				Text = comboBoxEventDevice.Text,
			}, COLUMN_EVENTS_DEVICE, tableLayoutPanelEvents.RowCount - 1);

			// Add the variable.
			tableLayoutPanelEvents.Controls.Add(new Label
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				AutoSize = true,
				Dock = DockStyle.None,
				Text = comboBoxEventVariable.Text,
			}, COLUMN_EVENTS_VARIABLE, tableLayoutPanelEvents.RowCount - 1);

			// Add the variable's value.
			tableLayoutPanelEvents.Controls.Add(new Label
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				AutoSize = true,
				Dock = DockStyle.None,
				Text = numericUpDownEventValue.Text
			}, COLUMN_EVENTS_VALUE, tableLayoutPanelEvents.RowCount - 1);

			// Add the duration.
			tableLayoutPanelEvents.Controls.Add(new Label
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				AutoSize = true,
				Dock = DockStyle.None,
				Text = numericUpDownEventDuration.Text
			}, COLUMN_EVENTS_DURATION, tableLayoutPanelEvents.RowCount - 1);

			// Add the status.
			tableLayoutPanelEvents.Controls.Add(new Label
			{
				Anchor = AnchorStyles.Left | AnchorStyles.Top,
				AutoSize = true,
				Dock = DockStyle.None,
				Text = "Queued"
			}, COLUMN_EVENTS_STATUS, tableLayoutPanelEvents.RowCount - 1);

			// Make the GUI act normally again.
			tableLayoutPanelEvents.ResumeLayout();
		}

		private void CheckBoxDeviceSelectAll_CheckedChanged(object sender, EventArgs e)
		{
			foreach (CheckBox c in tableLayoutPanelDevices.Controls.OfType<CheckBox>())
			{
				c.Checked = ((CheckBox)sender).Checked;
			}
		}

		private void CheckBoxEventSelectAll_CheckedChanged(object sender, EventArgs e)
		{
			foreach (CheckBox c in tableLayoutPanelEvents.Controls.OfType<CheckBox>())
			{
				c.Checked = ((CheckBox)sender).Checked;
			}
		}

		private void ButtonDeviceDelete_Click(object sender, EventArgs e)
		{
			// Hunt backwards for checked boxes or we'll miss the last one selected.
			for (int row = tableLayoutPanelDevices.RowCount - 1; row >= 0; row--)
			{
				Control c = tableLayoutPanelDevices.GetControlFromPosition(0, row);
				if ((c is CheckBox box) && (box.Checked))
				{
					// Remove the controls in the row.
					Utilities.TableLayoutPanelRemoveRow(tableLayoutPanelDevices, row);

					// Remove the option from the "Events" tab.
					comboBoxEventDevice.Items.Remove(c.Text);
				}
			}

			// Uncheck the checkbox.
			checkBoxDeviceSelectAll.Checked = false;
		}

		private void ButtonEventDelete_Click(object sender, EventArgs e)
		{
			// Hunt backwards for checked items or we'll miss the last one selected.
			for (int row = tableLayoutPanelEvents.RowCount - 1; row >= 0; row--)
			{
				Control c = tableLayoutPanelEvents.GetControlFromPosition(0, row);
				if ((c is CheckBox box) && (box.Checked))
				{
					// Remove the controls in the row.
					Utilities.TableLayoutPanelRemoveRow(tableLayoutPanelEvents, row);
				}
			}

			// Uncheck the checkbox.
			checkBoxEventSelectAll.Checked = false;
		}

		private void ButtonLogBrowse_Click(object sender, EventArgs e)
		{
			// Create a file browser.
			OpenFileDialog openFileDialog = new OpenFileDialog()
			{
				InitialDirectory = textBoxLogFilename.Text,
				Title = "Browse Log Files",
				CheckFileExists = false,
				CheckPathExists = true,
				DefaultExt = "csv",
				Filter = "txt files (*.txt)|*.txt|csv files (*.csv)|*.csv|All files (*.*)|*.*",
				FilterIndex = 2,
			};

			// Show the dialog box to the user.
			// If the user selects a file...
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				// Use that file.
				textBoxLogFilename.Text = openFileDialog.FileName;

				// Remember it for next time.
				Properties.Settings.Default.Logfile = openFileDialog.FileName;
			}
		}
	}
}
