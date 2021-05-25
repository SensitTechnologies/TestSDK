﻿using System.Collections.Generic;
using System.ComponentModel;
using Sensit.TestSDK.Calculations;

namespace Sensit.TestSDK.Interfaces
{
	public enum VariableType
	{
		[Description("Mass Flow")]
		MassFlow,
		[Description("Volume Flow")]
		VolumeFlow,
		Velocity,
		Pressure,
		Temperature,
		Current,
		Voltage,
		Channel
	}

	/// <summary>
	/// Gas Selection for Mass Flow Controllers
	/// </summary>
	public enum Gas
	{
		Air,
		Argon,
		Methane,
		[Description("Carbon Monoxide")]
		CarbonMonoxide,
		[Description("Carbon Dioxide")]
		CarbonDioxide,
		Ethane,
		Hydrogen,
		[Description("Hydrogen Sulfide")]
		HydrogenSulfide,
		[Description("Hydrogen Cyanide")]
		HydrogenCyanide,
		Helium,
		Nitrogen,
		[Description("Nitrous Oxide")]
		NitrousOxide,
		Neon,
		Oxygen,
		Propane,
		[Description("Normal Butane")]
		normalButane,
		Acetylene,
		Ethylene,
		isoButane,
		Krypton,
		Xenon,
		[Description("Sulfur Hexafluoride")]
		SulfurHexafluoride,
		[Description("75% Argon / 25% CO2")]
		C25,
		[Description("90% Argon / 10% CO2")]
		C10,
		[Description("92% Argon / 8% CO2")]
		C8,
		[Description("98% Argon / 2% CO2")]
		C2,
		[Description("75% CO2 / 25% Argon")]
		C75,
		[Description("75% Argon / 25% Helium")]
		He25,
		[Description("75% Helium / 25% Argon")]
		He75,
		[Description("90% Helium / 7.5% Argon / 2.5% CO2 (Praxair - Helistar® A1025)")]
		A1025,
		[Description("90% Argon / 8% CO2 / 2% Oxygen (Praxair - Stargon® CS)")]
		Star29,
		[Description("95% Argon / 5% Methane")]
		P5,
	}

	/// <summary>
	/// What the device should try to do.
	/// </summary>
	public enum ControlMode
	{
		Active,     // actively controlling the test environment
		Passive     // passively measuring the test environment
	}

	/// <summary>
	/// Device that measures and/or controls one or more dependent variables in a test.
	/// </summary>
	/// <remarks>
	/// Don't implement this interface directly.
	/// Devices should implement one of the more specific interfaces below.
	/// </remarks>
	public interface IDevice
	{
		/// <summary>
		/// Supported readings and their values.
		/// </summary>
		Dictionary<VariableType, double> Readings { get; }

		/// <summary>
		/// Supported setpoints and their values.
		/// </summary>
		Dictionary<VariableType, double> Setpoints { get; }

		/// <summary>
		/// Change the device's control mode.
		/// </summary>
		/// <param name="mode"></param>
		void SetControlMode(ControlMode mode);

		/// <summary>
		/// Write setpoint(s) to the device.
		/// </summary>
		/// <param name="variable">which variable to write</param>
		void Write(VariableType variable);

		/// <summary>
		/// Fetch new values from the device.
		/// </summary>
		void Read();
	}

	/// <summary>
	/// Device that measures gas mass flow.
	/// </summary>
	/// <remarks>
	/// May wish to split into two interfaces (mass and volumetric flow) in the future.
	/// </remarks>
	[Description("Gas Mass Flow Device")]
	public interface IMassFlowDevice : IDevice
	{
		UnitOfMeasure.Flow FlowUnit { get; set; }

		/// <summary>
		/// Gas used by device to calculate mass flow from volumetric flow.
		/// </summary>
		Gas GasSelection { get; set; }
	}

	/// <summary>
	/// Device that measures gas volumetric flow.
	/// </summary>
	[Description("Gas Volume Flow Device")]
	public interface IVolumeFlowDevice : IDevice
	{
		UnitOfMeasure.Flow FlowUnit { get; set; }
	}

	/// <summary>
	/// Device that measures gas velocity.
	/// </summary>
	[Description("Velocity Device")]
	public interface IVelocityDevice : IDevice
	{
		UnitOfMeasure.Velocity VelocityUnit { get; set; }
	}

	/// <summary>
	/// Device that measures pressure.
	/// </summary>
	[Description("Pressure Device")]
	public interface IPressureDevice : IDevice
	{
		UnitOfMeasure.Pressure PressureUnit { get; set; }
	}

	/// <summary>
	/// Device that measures temperature.
	/// </summary>
	[Description("Temperature Device")]
	public interface ITemperatureDevice : IDevice
	{
		UnitOfMeasure.Temperature TemperatureUnit { get; set; }
	}

	/// <summary>
	/// Device that measures electrical current.
	/// </summary>
	[Description("Current Device")]
	public interface ICurrentDevice : IDevice
	{
		UnitOfMeasure.Current CurrentUnit { get; set; }
	}

	/// <summary>
	/// Device that measures electrical voltage.
	/// </summary>
	[Description("Voltage Device")]
	public interface IVoltageDevice : IDevice
	{
		UnitOfMeasure.Voltage VoltageUnit { get; set; }
	}

	/// <summary>
	/// Device that delivers generic messages as a text string.
	/// </summary>
	/// <remarks>
	/// Intended to be a convenient way to support any UART-based device without
	/// requiring modifications to this software.
	/// </remarks>
	[Description("Message Device")]
	public interface IMessageDevice : IDevice
	{
		string Message { get; }
	}
}
