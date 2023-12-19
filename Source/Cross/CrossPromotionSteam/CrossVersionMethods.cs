using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Brrainz
{
	internal static class CrossVersionMethods
	{
		internal static string CrossVersionAuthor(this ModMetaData mod)
		{
			string value = Traverse.Create(mod).Property("AuthorsString", null).GetValue<string>();
			string value2 = Traverse.Create(mod).Property("Author", null).GetValue<string>();
			return (value ?? value2).Replace("Andreas Pardeike", "Brrainz");
		}

		internal static bool CrossVersionButtonText(this WidgetRow row, string label, string tooltip = null, bool drawBackground = true, bool doMouseoverSound = true)
		{
			if (CrossVersionMethods.mButtonText == null)
			{
				CrossVersionMethods.mButtonText = AccessTools.Method(typeof(WidgetRow), "ButtonText", null, null);
				CrossVersionMethods.buttonTextDefaults = (from p in CrossVersionMethods.mButtonText.GetParameters()
				select p.DefaultValue).ToArray<object>();
			}
			object[] array = CrossVersionMethods.buttonTextDefaults;
			array[0] = label;
			array[1] = tooltip;
			array[2] = drawBackground;
			array[3] = doMouseoverSound;
			return (bool)CrossVersionMethods.mButtonText.Invoke(row, array);
		}

		private static MethodInfo mButtonText = null;

		private static object[] buttonTextDefaults = new object[0];
	}
}
