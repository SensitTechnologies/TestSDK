﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Sensit.TestSDK.Calculations;
using Sensit.TestSDK.Communication;
using Sensit.TestSDK.Exceptions;
using Sensit.TestSDK.Interfaces;

namespace Sensit.TestSDK.Devices
{
	/// <summary>
	/// Communication driver for a GWInstek programmable power supply.
	/// </summary>
	/// <remarks>
	/// Product Website:
	/// https://www.gwinstek.com/en-global/products/detail/GPD-Series
	/// This is a four-channel programmable linear DC power supply.
	/// It communicates using a serial port and SCPI commands.
	/// </remarks>
	public class GPDX303S : SerialDevice, IVoltageReference, ICurrentReference,
		IVoltageController, ICurrentController
	{
		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public GPDX303S()
		{
			// Set a default baud rate.
			SerialPort.BaudRate = 9600;
		}

		#endregion

		#region Serial Device Methods

		public new int BaudRate
		{
			get => SerialPort.BaudRate;
			set
			{
				if ((value != 9600) &&
					(value != 57600) &&
					(value != 115200))
				{
					throw new DeviceSettingNotSupportedException("The GPD-X303S power supply does not support baud rate " + value.ToString() + ".");
				}

				SerialPort.BaudRate = value;
			}
		}

		public new int DataBits
		{
			get => SerialPort.DataBits;
			set
			{
				if (value != 8)
				{
					throw new DeviceSettingNotSupportedException("The GPD-X303S power supply only supports 8 data bits.");
				}
			}
		}

		public new Parity Parity
		{
			get => SerialPort.Parity;
			set
			{
				if (value != Parity.None)
					throw new DeviceSettingNotSupportedException("The GPD-X303S power supply does not support parity.");

				SerialPort.Parity = value;
			}
		}

		public new StopBits StopBits
		{
			get => SerialPort.StopBits;
			set
			{
				if (value != StopBits.One)
					throw new DeviceSettingNotSupportedException("The GPD-X303S power supply only supports one stop bit.");

				SerialPort.StopBits = value;
			}
		}

		public override void WriteSerialProperties(int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
		{
			DataBits = dataBits;
			Parity = parity;
			StopBits = stopBits;
		}

		/// <summary>
		/// Open the serial port with the correct settings.
		/// </summary>
		/// <param name="portName">serial port name (e.g. "COM3")</param>
		/// <param name="baudRate">baud rate</param>
		public override void Open(string portName, int baudRate)
		{
			try
			{
				// Set serial port settings.
				SerialPort.PortName = portName;
				BaudRate = baudRate;
				SerialPort.DataBits = 8;
				SerialPort.Parity = Parity.None;
				SerialPort.StopBits = StopBits.One;
				SerialPort.Handshake = Handshake.None;
				SerialPort.ReadTimeout = 500;
				SerialPort.WriteTimeout = 500;

				// Open the serial port.
				SerialPort.Open();
			}
			catch (SystemException ex)
			{
				throw new DevicePortException("Could not open power supply's serial port."
					+ Environment.NewLine + ex.Message);
			}
		}

		#endregion

		#region Reference Device Methods

		public Dictionary<VariableType, double> Readings { get; } = new Dictionary<VariableType, double>
		{
			{ VariableType.Current, 0.0 },
			{ VariableType.Voltage, 0.0 },
		};

		public UnitOfMeasure.Voltage VoltageUnit { get; set; } = UnitOfMeasure.Voltage.Volt;

		public UnitOfMeasure.Current CurrentUnit { get; set; } = UnitOfMeasure.Current.Amp;

		public void Read()
		{
			// Fetch the voltage reading.
			Readings[VariableType.Voltage] = SendQuery(new GPDX303S_SCPI().VOUT(1).Query());

			// Fetch the current reading.
			Readings[VariableType.Current] = SendQuery(new GPDX303S_SCPI().IOUT(1).Query());
		}

		#endregion

		private void SendCommand(string command)
		{
			try
			{
				// Write the command to the device.
				SerialPort.WriteLine(command);
			}
			catch (InvalidOperationException ex)
			{
				throw new DevicePortException("Could not read from power supply."
					+ Environment.NewLine + ex.Message);
			}
			catch (TimeoutException ex)
			{
				throw new DeviceCommunicationException("No response from power supply."
					+ Environment.NewLine + ex.Message);
			}
		}

		private float SendQuery(string command)
		{
			float result;
			try
			{
				// Write the command to the device.
				SerialPort.WriteLine(command);

				// Read the response.
				string message = SerialPort.ReadLine();

				// Remove any newlines or tabs.
				message = Regex.Replace(message, @"\t|\n|\r", "");

				// Split the string using spaces to separate each word.
				char[] separators = new char[] { ' ' };
				string[] words = message.Split(separators, StringSplitOptions.RemoveEmptyEntries);

				// Remove any non-digit characters.
				string value = words[words.Length - 1].Trim('V', 'A');

				// Convert the last word to a number.
				result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
			}
			catch (InvalidOperationException ex)
			{
				throw new DevicePortException("Could not read from power supply."
					+ Environment.NewLine + ex.Message);
			}
			catch (TimeoutException ex)
			{
				throw new DeviceCommunicationException("No response from power supply."
					+ Environment.NewLine + ex.Message);
			}

			return result;
		}

		#region Control Device Methods

		public void WriteSetpoint(VariableType type, double setpoint)
		{
			switch (type)
			{
				case VariableType.Current:
					SendCommand(new GPDX303S_SCPI().ISET(1, Convert.ToSingle(setpoint)).Command());
					break;
				case VariableType.Voltage:
					SendCommand(new GPDX303S_SCPI().VSET(1, Convert.ToSingle(setpoint)).Command());
					break;
				default:
					throw new DeviceSettingNotSupportedException("Power supply does not support " + type.ToString() + " setpoints.");
			}
		}

		public double ReadSetpoint(VariableType type)
		{
			double result;
			switch (type)
			{
				case VariableType.Current:
					result = SendQuery(new GPDX303S_SCPI().ISET(1).Query());
					break;
				case VariableType.Voltage:
					// Fetch the voltage reading.
					result = SendQuery(new GPDX303S_SCPI().VSET(1).Query());
					break;
				default:
					throw new DeviceSettingNotSupportedException("Power supply does not support " + type.ToString() + ".");
			}

			return result;
		}

		public void SetControlMode(ControlMode mode)
		{
			switch (mode)
			{
				case ControlMode.Ambient:
					// Turn output off.
					SendCommand(new GPDX303S_SCPI().OUT(false).Command());
					break;
				case ControlMode.Control:
					// Turn output on.
					SendCommand(new GPDX303S_SCPI().OUT(true).Command());
					break;
				case ControlMode.Measure:
					throw new DeviceSettingNotSupportedException("Power supply does not support measure mode."
						+ Environment.NewLine + "Do you need a multimeter instead?");
				default:
					throw new DeviceSettingNotSupportedException("Cannot set power supply control mode:"
						+ Environment.NewLine + "Unrecognized mode.");
			}
		}

		#endregion
	}
}
