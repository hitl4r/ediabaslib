﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BMW.Rheingold.CoreFramework.Contracts.Vehicle;

namespace PsdzClient.Core
{
	public static class GearboxUtility
	{
		public static void SetGearboxType(Vehicle vehicle, string gearboxType, [CallerMemberName] string caller = null)
		{
			if (!GearboxUtility.useLegacyGearboxTypeDetection(vehicle))
			{
				return;
			}
			//Log.Info("GearboxUtility.SetGearboxType()", "Gearbox type set to " + gearboxType + ". Called from: " + caller, Array.Empty<object>());
			vehicle.Getriebe = gearboxType;
		}

		public static void SetAutomaticGearboxByEgsEcu(Vehicle vehicle)
		{
			if (!GearboxUtility.useLegacyGearboxTypeDetection(vehicle))
			{
				return;
			}
			if (vehicle.getECUbyECU_GRUPPE("G_EGS") != null && string.CompareOrdinal(vehicle.Getriebe, "AUT") != 0)
			{
				//Log.Info("GearboxUtility.SetAutomaticGearboxByEgsEcu()", "found EGS ecu in vehicle with recoginzed manual gearbox; will be overwritten", Array.Empty<object>());
				vehicle.Getriebe = "AUT";
			}
		}

		public static void PerformGearboxAssignments(Vehicle vehicle)
		{
			if (!GearboxUtility.useLegacyGearboxTypeDetection(vehicle))
			{
				return;
			}
			if (!string.IsNullOrEmpty(vehicle.Ereihe) && "E65 E66 E67 E68".Contains(vehicle.Ereihe))
			{
				vehicle.Getriebe = "AUT";
				return;
			}
			if (!GearboxUtility.HasVehicleGearboxECU(vehicle) && vehicle.getECU(new long?(24L)) == null)
			{
				if (vehicle.FA != null && vehicle.FA.SA != null && vehicle.FA.SA.Count > 0 && !vehicle.FA.SA.Contains("205"))
				{
					//Log.Info("VehicleIdent.doVehicleIdent()", "gearbox set to manual, because vehicle does not contain 205 sa", Array.Empty<object>());
					vehicle.Getriebe = "MECH";
				}
				if (vehicle.ECU != null && vehicle.ECU.Count > 0 && vehicle.BNType != BNType.IBUS)
				{
					//Log.Info("VehicleIdent.doVehicleIdent()", "gearbox set to manual, because vehicle has no D_EGS or G_EGS", Array.Empty<object>());
					vehicle.Getriebe = "MECH";
				}
				return;
			}
			ECU ecu = vehicle.getECU(new long?(24L));
			if ((ecu != null || (vehicle.VehicleIdentLevel != IdentificationLevel.VINVehicleReadout && vehicle.VehicleIdentLevel != IdentificationLevel.VINVehicleReadoutOnlineUpdated)) && (ecu == null || string.IsNullOrEmpty(ecu.VARIANTE) || string.Compare(ecu.VARIANTE, 0, "DKG", 0, 3, StringComparison.OrdinalIgnoreCase) != 0) && (ecu == null || string.IsNullOrEmpty(ecu.VARIANTE) || string.Compare(ecu.VARIANTE, 0, "GSGE", 0, 4, StringComparison.OrdinalIgnoreCase) != 0) && (ecu == null || string.IsNullOrEmpty(ecu.VARIANTE) || string.Compare(ecu.VARIANTE, 0, "SMG", 0, 3, StringComparison.OrdinalIgnoreCase) != 0) && (ecu == null || string.IsNullOrEmpty(ecu.VARIANTE) || string.Compare(ecu.VARIANTE, 0, "SSG", 0, 3, StringComparison.OrdinalIgnoreCase) != 0) && (ecu == null || string.IsNullOrEmpty(ecu.VARIANTE) || string.Compare(ecu.VARIANTE, 0, "GSD", 0, 3, StringComparison.OrdinalIgnoreCase) != 0) && !vehicle.hasSA("2MK") && !vehicle.hasSA("206") && !vehicle.hasSA("2TC"))
			{
				if ("MECH".Equals(vehicle.Getriebe, StringComparison.OrdinalIgnoreCase))
				{
					//Log.Info("VehicleIdent.doVehicleIdent()", "found EGS ECU in vehicle with recognized manual gearbox; will be overwritten", Array.Empty<object>());
				}
				vehicle.Getriebe = "AUT";
				return;
			}
			if ("AUT".Equals(vehicle.Getriebe, StringComparison.OrdinalIgnoreCase))
			{
				//Log.Info("VehicleIdent.doVehicleIdent()", "found DKG ECU in vehicle with recognized automatic gearbox; will be overwritten", Array.Empty<object>());
			}
			vehicle.Getriebe = "MECH";
		}

		public static bool HasVehicleGearboxECU(Vehicle vehicle)
		{
			return !"W10".Equals(vehicle.Motor, StringComparison.OrdinalIgnoreCase) && ("AUT".Equals(vehicle.Getriebe, StringComparison.OrdinalIgnoreCase) || vehicle.hasSA("205") || vehicle.hasSA("206") || vehicle.hasSA("2TB") || vehicle.hasSA("2TC") || vehicle.hasSA("2MK"));
		}

		public static void SetGearboxTypeFromCharacteristics(Vehicle vehicle, PdszDatabase.Characteristics gearboxCharacteristic)
		{
			string name = gearboxCharacteristic.Name;
			//Log.Info("GearboxUtility.SetGearboxTypeFromCharacteristics()", "Gearbox type: '" + name + "' found in the xep_characteristics table.", Array.Empty<object>());
			vehicle.Getriebe = name;
		}
#if false
		public static void SetServiceCodeIfGearboxIsNotDetected(Vehicle vehicle, IFasta2Service fasta)
		{
			if (vehicle.Getriebe == "X")
			{
				fasta.AddServiceCode("NVI04_Gearbox_X", vehicle.Ereihe + ", " + vehicle.Typ, LayoutGroup.D);
			}
		}
#endif
		private static DateTime legacyDetectionConditionDate = new DateTime(2020, 7, 1);

		private static Predicate<Vehicle> useLegacyGearboxTypeDetection = delegate (Vehicle v)
		{
			bool result;
			try
			{
				result = (new DateTime(int.Parse(v.Modelljahr), int.Parse(v.Modellmonat), 1) < GearboxUtility.legacyDetectionConditionDate);
			}
			catch (Exception)
			{
				result = false;
			}
			return result;
		};

		public const string Manual = "MECH";

		public const string Automatic = "AUT";

		public const string NoGearbox = "-";

		public const string UnknownGearbox = "X";
	}
}
