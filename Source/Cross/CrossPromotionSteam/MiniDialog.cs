using System;
using UnityEngine;
using Verse;

namespace Brrainz
{
	internal class MiniDialog : Dialog_MessageBox
	{
		internal MiniDialog(string text, string buttonAText = null, Action buttonAAction = null, string buttonBText = null, Action buttonBAction = null, string title = null, bool buttonADestructive = false, Action acceptAction = null, Action cancelAction = null) : base(text, buttonAText, buttonAAction, buttonBText, buttonBAction, title, buttonADestructive, acceptAction, cancelAction, WindowLayer.Dialog)
		{
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(320f, 240f);
			}
		}
	}
}
