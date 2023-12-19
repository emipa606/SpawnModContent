using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace Brrainz
{
	[StaticConstructorOnStartup]
	internal class PromotionLayout
	{
		internal static bool Promotion(Rect mainRect, Page_ModsConfig page)
		{
			if (!SteamManager.Initialized)
			{
				return false;
			}
			ModMetaData selectedMod = page.selectedMod;
			if (selectedMod == null || selectedMod.GetWorkshopItemHook().steamAuthor.m_SteamID != CrossPromotion.userID || CrossPromotion.promotionMods.Count == 0)
			{
				return false;
			}
			float num = mainRect.width * 2f / 3f;
			float rightColumn = mainRect.width - num - 10f;
			GUI.BeginGroup(mainRect);
			try
			{
				PromotionLayout.ContentPart(mainRect, num, selectedMod, page);
				PromotionLayout.PromotionPart(mainRect, num, rightColumn, selectedMod, page);
			}
			catch
			{
				GUI.EndGroup();
				return false;
			}
			GUI.EndGroup();
			return true;
		}

		private static void ContentPart(Rect mainRect, float leftColumn, ModMetaData mod, Page_ModsConfig page)
		{
			List<ulong> list = (from wi in WorkshopItems.AllSubscribedItems
			select wi.PublishedFileId.m_PublishedFileId).ToList<ulong>();
			ulong mainModID = mod.GetPublishedFileId().m_PublishedFileId;
			SteamUGCDetails_t[] promoMods = CrossPromotion.promotionMods.ToArray();
			SteamUGCDetails_t steamUGCDetails_t = promoMods.FirstOrDefault((SteamUGCDetails_t m) => m.m_nPublishedFileId.m_PublishedFileId == mainModID);
			bool flag = ModLister.AllInstalledMods.Any((ModMetaData meta) => meta.GetPublishedFileId().m_PublishedFileId == mainModID && meta.Source == ContentSource.ModsFolder);
			bool flag2 = list.Contains(mainModID);
			ulong? lastPresentedMod = CrossPromotion.lastPresentedMod;
			ulong mainModID2 = mainModID;
			if (!(lastPresentedMod.GetValueOrDefault() == mainModID2 & lastPresentedMod != null))
			{
				PromotionLayout.leftScroll = Vector2.zero;
				PromotionLayout.rightScroll = Vector2.zero;
				CrossPromotion.lastPresentedMod = new ulong?(mainModID);
				new Thread(delegate()
				{
					SteamUGCDetails_t[] promoMods = promoMods;
					for (int i = 0; i < promoMods.Length; i++)
					{
						SteamUGCDetails_t promoMod = promoMods[i];
						CrossPromotion.UpdateVotingStatus(promoMod.m_nPublishedFileId.m_PublishedFileId, delegate(GetUserItemVoteResult_t result2, bool failure2)
						{
							CrossPromotion.allVoteStati[promoMod.m_nPublishedFileId.m_PublishedFileId] = ((result2.m_eResult == 1) ? new bool?(result2.m_bVotedUp) : null);
						});
					}
				}).Start();
			}
			string text = steamUGCDetails_t.m_rgchDescription;
			if (text == null || text.Length == 0)
			{
				text = mod.Description;
			}
			Rect outRect = new Rect(0f, 0f, leftColumn, mainRect.height);
			float num = outRect.width - 20f;
			Rect position = new Rect(0f, 0f, num, num * (float)mod.PreviewImage.height / (float)mod.PreviewImage.width);
			Rect rect = new Rect(0f, 34f + position.height, num, Text.CalcHeight(text, num));
			Rect viewRect = new Rect(0f, 0f, num, position.height + 20f + 8f + 10f + rect.height);
			Widgets.BeginScrollView(outRect, ref PromotionLayout.leftScroll, viewRect, true);
			GUI.DrawTexture(position, mod.PreviewImage, ScaleMode.ScaleToFit);
			WidgetRow row = new WidgetRow(position.xMax, position.yMax + 8f, UIDirection.LeftThenDown, num, 8f);
			if (!flag && row.CrossVersionButtonText("Unsubscribe".Translate(), null, true, true))
			{
				ThreadStart <>9__7;
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmUnsubscribe".Translate(mod.Name), delegate
				{
					mod.enabled = false;
					ThreadStart start;
					if ((start = <>9__7) == null)
					{
						start = (<>9__7 = delegate()
						{
							Workshop.Unsubscribe(mod);
							page.Notify_SteamItemUnsubscribed(new PublishedFileId_t(mainModID));
						});
					}
					new Thread(start).Start();
				}, true, null, WindowLayer.Dialog));
			}
			if (flag2 && row.CrossVersionButtonText("WorkshopPage".Translate(), null, true, true))
			{
				SteamUtility.OpenWorkshopPage(new PublishedFileId_t(mainModID));
			}
			if (Prefs.DevMode && mod.CanToUploadToWorkshop())
			{
				row = new WidgetRow(position.xMin, position.yMax + 8f, UIDirection.RightThenDown, num, 8f);
				if (row.CrossVersionButtonText("Upload", null, true, true))
				{
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSteamWorkshopUpload".Translate(), delegate
					{
						Workshop.Upload(mod);
					}, true, null, WindowLayer.Dialog));
				}
			}
			Widgets.Label(rect, text);
			Widgets.EndScrollView();
		}

		private static void PromotionPart(Rect mainRect, float leftColumn, float rightColumn, ModMetaData mod, Page_ModsConfig page)
		{
			PromotionLayout.<>c__DisplayClass4_0 CS$<>8__locals1 = new PromotionLayout.<>c__DisplayClass4_0();
			CS$<>8__locals1.page = page;
			CS$<>8__locals1.mainModID = mod.GetPublishedFileId();
			Text.Font = GameFont.Tiny;
			float num = 30f;
			Rect rect = new Rect(leftColumn + 10f, -4f, rightColumn - 20f, num);
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rect, "Mods of " + mod.CrossVersionAuthor().Replace("Andreas Pardeike", "Brrainz") + ":".Truncate(rect.width, null));
			Text.Anchor = TextAnchor.UpperLeft;
			Rect outRect = new Rect(leftColumn + 10f, num - 4f, rightColumn, mainRect.height - (num - 4f));
			float num2 = outRect.width - 20f;
			float num3 = num2 * 319f / 588f;
			IEnumerable<SteamUGCDetails_t> enumerable = from m in CrossPromotion.promotionMods.ToArray()
			where m.m_nPublishedFileId != CS$<>8__locals1.mainModID
			select m;
			List<ulong> list = (from wi in WorkshopItems.AllSubscribedItems
			select wi.PublishedFileId.m_PublishedFileId).ToList<ulong>();
			List<ulong> list2 = (from meta in ModLister.AllInstalledMods
			where meta.Active
			select meta.GetPublishedFileId().m_PublishedFileId).ToList<ulong>();
			float num4 = 0f;
			foreach (SteamUGCDetails_t steamUGCDetails_t in enumerable)
			{
				ulong myModID = steamUGCDetails_t.m_nPublishedFileId.m_PublishedFileId;
				bool flag = ModLister.AllInstalledMods.Any((ModMetaData meta) => meta.GetPublishedFileId().m_PublishedFileId == myModID && meta.Source == ContentSource.ModsFolder);
				bool flag2 = list.Contains(myModID);
				bool? flag3;
				CrossPromotion.allVoteStati.TryGetValue(myModID, out flag3);
				if (num4 > 0f)
				{
					num4 += 10f;
				}
				Texture2D texture2D = CrossPromotion.PreviewForMod(steamUGCDetails_t.m_nPublishedFileId.m_PublishedFileId);
				if (texture2D != null)
				{
					num4 += num2 * (float)texture2D.height / (float)texture2D.width + 2f;
					if (!flag)
					{
						if (flag2)
						{
							bool? flag4 = flag3;
							bool flag5 = false;
							if (!(flag4.GetValueOrDefault() == flag5 & flag4 != null))
							{
								continue;
							}
						}
						num4 += 16f;
					}
				}
			}
			Widgets.BeginScrollView(outRect, ref PromotionLayout.rightScroll, new Rect(0f, 0f, num2, num4), true);
			bool flag6 = true;
			Rect rect2 = new Rect(0f, 0f, num2, 0f);
			foreach (SteamUGCDetails_t steamUGCDetails_t2 in enumerable)
			{
				PromotionLayout.<>c__DisplayClass4_2 CS$<>8__locals3 = new PromotionLayout.<>c__DisplayClass4_2();
				CS$<>8__locals3.CS$<>8__locals1 = CS$<>8__locals1;
				CS$<>8__locals3.myModID = steamUGCDetails_t2.m_nPublishedFileId.m_PublishedFileId;
				CS$<>8__locals3.isLocalFile = ModLister.AllInstalledMods.Any((ModMetaData meta) => meta.GetPublishedFileId().m_PublishedFileId == CS$<>8__locals3.myModID && meta.Source == ContentSource.ModsFolder);
				CS$<>8__locals3.isSubbed = list.Contains(CS$<>8__locals3.myModID);
				bool flag7 = list2.Contains(CS$<>8__locals3.myModID);
				bool? flag8;
				CrossPromotion.allVoteStati.TryGetValue(CS$<>8__locals3.myModID, out flag8);
				if (!flag6)
				{
					rect2.y += 10f;
				}
				Texture2D texture2D2 = CrossPromotion.PreviewForMod(steamUGCDetails_t2.m_nPublishedFileId.m_PublishedFileId);
				if (texture2D2 != null)
				{
					rect2.height = num2 * (float)texture2D2.height / (float)texture2D2.width;
					GUI.DrawTexture(rect2, texture2D2, ScaleMode.ScaleToFit);
					Rect rect3 = rect2;
					rect3.xMax -= 4f;
					rect3.yMax -= 4f;
					rect3.xMin = rect3.xMax - 18f;
					rect3.yMin = rect3.yMax - 18f;
					bool flag9 = flag7;
					GUI.DrawTexture(rect3.ContractedBy(-2f), PromotionLayout.CheckboxBackground);
					Widgets.Checkbox(rect3.xMin, rect3.yMin, ref flag9, rect3.width, false, false, null, null);
					if (flag9 != flag7)
					{
						ModMetaData modMetaData = ModLister.AllInstalledMods.FirstOrDefault((ModMetaData meta) => meta.GetPublishedFileId().m_PublishedFileId == CS$<>8__locals3.myModID);
						if (modMetaData != null)
						{
							ModsConfig.SetActive(modMetaData, flag9);
							ModsConfig.Save();
						}
					}
					if (!Mouse.IsOver(rect3))
					{
						Widgets.DrawHighlightIfMouseover(rect2);
						if (Widgets.ButtonInvisible(rect2, true))
						{
							string str = steamUGCDetails_t2.m_rgchTitle + "\n\n" + steamUGCDetails_t2.m_rgchDescription;
							string buttonBText = (CS$<>8__locals3.isSubbed | CS$<>8__locals3.isLocalFile) ? "Select" : "Subscribe";
							Dialog_MessageBox window = new Dialog_MessageBox(str, "Close".Translate(), null, buttonBText, new Action(CS$<>8__locals3.<PromotionPart>g__actionButtonAction|9), null, false, null, null, WindowLayer.Dialog);
							Find.WindowStack.Add(window);
						}
					}
					rect2.y += rect2.height + 2f;
					rect2.height = 0f;
					if (!CS$<>8__locals3.isLocalFile)
					{
						if (!CS$<>8__locals3.isSubbed)
						{
							rect2.height = 16f;
							if (CrossPromotion.subscribingMods.Contains(CS$<>8__locals3.myModID))
							{
								Widgets.Label(rect2, PromotionLayout.WaitingString);
							}
							else if (Widgets.ButtonText(rect2, "Subscribe", false, true, true))
							{
								new Thread(delegate()
								{
									CrossPromotion.subscribingMods.Add(CS$<>8__locals3.myModID);
									SteamUGC.SubscribeItem(new PublishedFileId_t(CS$<>8__locals3.myModID));
								}).Start();
							}
						}
						else if (flag8 != null)
						{
							bool? flag4 = flag8;
							bool flag5 = false;
							if (flag4.GetValueOrDefault() == flag5 & flag4 != null)
							{
								rect2.height = 16f;
								if (Widgets.ButtonText(rect2, "Like", false, true, true))
								{
									new Thread(delegate()
									{
										CrossPromotion.allVoteStati[CS$<>8__locals3.myModID] = new bool?(true);
										SteamUGC.SetUserItemVote(new PublishedFileId_t(CS$<>8__locals3.myModID), true);
									}).Start();
								}
							}
						}
					}
					rect2.y += rect2.height;
				}
				flag6 = false;
			}
			Widgets.EndScrollView();
		}

		private static Texture2D CheckboxBackground
		{
			get
			{
				if (PromotionLayout._checkboxBackground == null)
				{
					PromotionLayout._checkboxBackground = SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.5f));
				}
				return PromotionLayout._checkboxBackground;
			}
		}

		private static string WaitingString
		{
			get
			{
				long num = DateTime.Now.Ticks / 20L % 4L;
				return (new string[]
				{
					"....",
					"... .",
					".. ..",
					". ..."
				})[(int)(checked((IntPtr)num))];
			}
		}

		private static Vector2 leftScroll = Vector2.zero;

		private static Vector2 rightScroll = Vector2.zero;

		private static Texture2D _checkboxBackground;
	}
}
