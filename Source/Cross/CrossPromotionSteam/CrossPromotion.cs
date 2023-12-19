using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace Brrainz
{
	public static class CrossPromotion
	{
		public static void Install(ulong userID)
		{
			CrossPromotion.userID = userID;
			if (Harmony.HasAnyPatches("brrainz-crosspromotion"))
			{
				return;
			}
			Harmony harmony = new Harmony("brrainz-crosspromotion");
			harmony.Patch(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(ModLister.RebuildModList()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>())), null, new HarmonyMethod(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(CrossPromotion.ModLister_RebuildModList_Postfix()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()))), null, null);
			harmony.Patch(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(Expression.New(typeof(Page_ModsConfig)), methodof(Window.PostClose()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>())), null, new HarmonyMethod(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(CrossPromotion.Page_ModsConfig_PostClose_Postfix()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()))), null, null);
			harmony.Patch(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(WorkshopItems.Notify_Subscribed(PublishedFileId_t)), new Expression[]
			{
				Expression.Constant(default(PublishedFileId_t), typeof(PublishedFileId_t))
			}), Array.Empty<ParameterExpression>())), null, new HarmonyMethod(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(CrossPromotion.WorkshopItems_Notify_Subscribed_Postfix(PublishedFileId_t)), new Expression[]
			{
				Expression.New(methodof(PublishedFileId_t..ctor(ulong)), new Expression[]
				{
					Expression.Constant(0UL, typeof(ulong))
				})
			}), Array.Empty<ParameterExpression>()))), null, null);
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Page_ModsConfig), "DoWindowContents", null, null), null, null, new HarmonyMethod(SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(CrossPromotion.Page_ModsConfig_DoWindowContents_Transpiler(IEnumerable<CodeInstruction>, ILGenerator)), new Expression[]
			{
				Expression.Constant(null, typeof(IEnumerable<CodeInstruction>)),
				Expression.Constant(null, typeof(ILGenerator))
			}), Array.Empty<ParameterExpression>()))), null);
		}

		private static void ModLister_RebuildModList_Postfix()
		{
			CrossPromotion.ModPreviewPath(0UL);
			new Thread(delegate()
			{
				CrossPromotion.FetchPromotionMods();
			}).Start();
		}

		private static void Page_ModsConfig_PostClose_Postfix()
		{
			CrossPromotion.subscribingMods.Clear();
		}

		private static void WorkshopItems_Notify_Subscribed_Postfix(PublishedFileId_t pfid)
		{
			ulong longID = pfid.m_PublishedFileId;
			if (!CrossPromotion.subscribingMods.Contains(longID))
			{
				return;
			}
			CrossPromotion.subscribingMods.Remove(longID);
			Func<ModMetaData, bool> <>9__1;
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				IEnumerable<ModMetaData> allInstalledMods = ModLister.AllInstalledMods;
				Func<ModMetaData, bool> predicate;
				if ((predicate = <>9__1) == null)
				{
					predicate = (<>9__1 = ((ModMetaData meta) => meta.GetPublishedFileId().m_PublishedFileId == longID));
				}
				ModMetaData modMetaData = allInstalledMods.FirstOrDefault(predicate);
				if (modMetaData == null)
				{
					return;
				}
				ModsConfig.SetActive(modMetaData, true);
				ModsConfig.Save();
				Find.WindowStack.Add(new MiniDialog(modMetaData.Name + " added", null, null, null, null, null, false, null, null));
			});
		}

		private static IEnumerable<CodeInstruction> Page_ModsConfig_DoWindowContents_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
			int[] array = (from pair in list.Select((CodeInstruction instr, int idx) => new Pair<int, CodeInstruction>(idx, instr)).Where(delegate(Pair<int, CodeInstruction> pair)
			{
				MethodInfo methodInfo = pair.Second.operand as MethodInfo;
				return methodInfo != null && methodInfo.Name == "BeginGroup";
			})
			select pair.First).ToArray<int>();
			int[] array2 = (from pair in list.Select((CodeInstruction instr, int idx) => new Pair<int, CodeInstruction>(idx, instr)).Where(delegate(Pair<int, CodeInstruction> pair)
			{
				MethodInfo methodInfo = pair.Second.operand as MethodInfo;
				return methodInfo != null && methodInfo.Name == "EndGroup";
			})
			select pair.First).ToArray<int>();
			if (array.Length != 2 || array2.Length != 2)
			{
				return instructions;
			}
			int index = array[1] - 1;
			int num = array2[0];
			Label label = generator.DefineLabel();
			list[num + 1].labels.Add(label);
			CodeInstruction codeInstruction = list[index];
			list.InsertRange(index, new CodeInstruction[]
			{
				codeInstruction.Clone(),
				new CodeInstruction(OpCodes.Ldarg_0, null),
				new CodeInstruction(OpCodes.Call, CrossPromotion.m_Promotion),
				new CodeInstruction(OpCodes.Brtrue, label)
			});
			return list.AsEnumerable<CodeInstruction>();
		}

		internal static string ModPreviewPath(ulong modID)
		{
			string text = Path.GetTempPath() + "BrrainzMods" + Path.DirectorySeparatorChar.ToString();
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text + modID.ToString() + "-preview.jpg";
		}

		internal static byte[] SafeRead(string path)
		{
			for (int i = 1; i <= 5; i++)
			{
				try
				{
					return File.ReadAllBytes(path);
				}
				catch (Exception)
				{
					Thread.Sleep(250);
				}
			}
			return null;
		}

		internal static Texture2D PreviewForMod(ulong modID)
		{
			Texture2D texture2D;
			if (CrossPromotion.previewTextures.TryGetValue(modID, out texture2D))
			{
				return texture2D;
			}
			string path = CrossPromotion.ModPreviewPath(modID);
			if (!File.Exists(path))
			{
				return null;
			}
			texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			if (texture2D.LoadImage(CrossPromotion.SafeRead(path)))
			{
				CrossPromotion.previewTextures[modID] = texture2D;
			}
			return texture2D;
		}

		internal static void UpdateVotingStatus(ulong modID, Action<GetUserItemVoteResult_t, bool> callback)
		{
			CallResult<GetUserItemVoteResult_t>.APIDispatchDelegate apidispatchDelegate = new CallResult<GetUserItemVoteResult_t>.APIDispatchDelegate(callback.Invoke);
			SteamAPICall_t userItemVote = SteamUGC.GetUserItemVote(new PublishedFileId_t(modID));
			CallResult<GetUserItemVoteResult_t>.Create(apidispatchDelegate).Set(userItemVote, null);
		}

		private static void AsyncUserModsQuery(UGCQueryHandle_t query, Action<SteamUGCQueryCompleted_t, bool> callback)
		{
			CallResult<SteamUGCQueryCompleted_t>.APIDispatchDelegate apidispatchDelegate = delegate(SteamUGCQueryCompleted_t result, bool failure)
			{
				callback(result, failure);
				SteamUGC.ReleaseQueryUGCRequest(query);
			};
			SteamAPICall_t steamAPICall_t = SteamUGC.SendQueryUGCRequest(query);
			CallResult<SteamUGCQueryCompleted_t>.Create(apidispatchDelegate).Set(steamAPICall_t, null);
		}

		private static void AsyncDownloadQuery(UGCHandle_t content, string path, Action<RemoteStorageDownloadUGCResult_t, bool> callback)
		{
			CallResult<RemoteStorageDownloadUGCResult_t>.APIDispatchDelegate apidispatchDelegate = new CallResult<RemoteStorageDownloadUGCResult_t>.APIDispatchDelegate(callback.Invoke);
			SteamAPICall_t steamAPICall_t = SteamRemoteStorage.UGCDownloadToLocation(content, path, 0U);
			CallResult<RemoteStorageDownloadUGCResult_t>.Create(apidispatchDelegate).Set(steamAPICall_t, null);
		}

		public static void FetchPromotionMods()
		{
			if (!SteamManager.Initialized)
			{
				return;
			}
			AppId_t appID = SteamUtils.GetAppID();
			UGCQueryHandle_t ugcqueryHandle_t = SteamUGC.CreateQueryUserUGCRequest(new AccountID_t((uint)CrossPromotion.userID), 0, 10, 5, appID, appID, 1U);
			SteamUGC.SetReturnLongDescription(ugcqueryHandle_t, true);
			SteamUGC.SetRankedByTrendDays(ugcqueryHandle_t, 7U);
			CrossPromotion.AsyncUserModsQuery(ugcqueryHandle_t, delegate(SteamUGCQueryCompleted_t result, bool failure)
			{
				for (uint num = 0U; num < result.m_unNumResultsReturned; num += 1U)
				{
					SteamUGCDetails_t mod;
					if (SteamUGC.GetQueryUGCResult(result.m_handle, num, ref mod) && !CrossPromotion.promotionMods.Any((SteamUGCDetails_t m) => m.m_nPublishedFileId.m_PublishedFileId == mod.m_nPublishedFileId.m_PublishedFileId))
					{
						CrossPromotion.promotionMods.Add(mod);
						ulong modID = mod.m_nPublishedFileId.m_PublishedFileId;
						string path = CrossPromotion.ModPreviewPath(modID);
						if (!File.Exists(path) || new FileInfo(path).Length != (long)mod.m_nPreviewFileSize)
						{
							CrossPromotion.AsyncDownloadQuery(mod.m_hPreviewFile, path, delegate(RemoteStorageDownloadUGCResult_t result2, bool failure2)
							{
								if (File.Exists(path) && CrossPromotion.previewTextures.ContainsKey(modID))
								{
									CrossPromotion.previewTextures.Remove(modID);
								}
							});
						}
						CrossPromotion.UpdateVotingStatus(modID, delegate(GetUserItemVoteResult_t result2, bool failure2)
						{
							CrossPromotion.allVoteStati[modID] = ((result2.m_eResult == 1) ? new bool?(result2.m_bVotedUp) : null);
						});
					}
				}
			});
		}

		private const string _crosspromotion = "brrainz-crosspromotion";

		internal static ulong userID;

		internal static List<SteamUGCDetails_t> promotionMods = new List<SteamUGCDetails_t>();

		internal static Dictionary<ulong, bool?> allVoteStati = new Dictionary<ulong, bool?>();

		internal static Dictionary<ulong, Texture2D> previewTextures = new Dictionary<ulong, Texture2D>();

		internal static List<ulong> subscribingMods = new List<ulong>();

		internal static ulong? lastPresentedMod = null;

		private static readonly MethodInfo m_Promotion = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(PromotionLayout.Promotion(Rect, Page_ModsConfig)), new Expression[]
		{
			Expression.New(typeof(Rect)),
			Expression.Constant(null, typeof(Page_ModsConfig))
		}), Array.Empty<ParameterExpression>()));
	}
}
