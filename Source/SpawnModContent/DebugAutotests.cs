using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SpawnModContent;

public static class DebugAutotests
{
    [DebugAction("Autotests", "Spawn mod content...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void DoTradeCaravanSpecific()
    {
        var list = new List<DebugMenuOption>();

        foreach (var mod in LoadedModManager.runningMods
                     .Where(mod => mod.Name != "Spawn Mod Content" && mod.defs?.Any() == true)
                     .OrderBy(mod => mod.Name))
        {
            list.Add(new DebugMenuOption(mod.Name, DebugMenuOptionMode.Action,
                delegate { SpawnModDefs(mod); }));
        }


        if (list.Count == 0)
        {
            Messages.Message("No loaded mods adds any spawnable items", MessageTypeDefOf.RejectInput, false);
            return;
        }

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
    }

    public static void SpawnModDefs(ModContentPack mod)
    {
        var godMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        Thing.allowDestroyNonDestroyable = true;
        if (Autotests_ColonyMaker.usedCells == null)
        {
            Autotests_ColonyMaker.usedCells = new BoolGrid(Autotests_ColonyMaker.Map);
        }
        else
        {
            Autotests_ColonyMaker.usedCells.ClearAndResizeTo(Autotests_ColonyMaker.Map);
        }

        var spawnWidth = Math.Min(Find.CurrentMap.Size.x, 100);
        var spawnHeight = Math.Min(Find.CurrentMap.Size.z, 100);

        Autotests_ColonyMaker.overRect = new CellRect(Find.CurrentMap.Center.x - (spawnWidth / 2),
            Find.CurrentMap.Center.z - (spawnHeight / 2), spawnWidth, spawnHeight);
        Autotests_ColonyMaker.DeleteAllSpawnedPawns();
        GenDebug.ClearArea(Autotests_ColonyMaker.overRect, Find.CurrentMap);
        Autotests_ColonyMaker.TryMakeBuilding(ThingDef.Named("SpawnModContent_PowerNode"));
        foreach (var raceDef in from k in DefDatabase<ThingDef>.AllDefs
                 where k.modContentPack == mod &&
                       k.race != null
                 select k)
        {
            var pawnKindDef =
                DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault(def => def.race != null &&
                                                                       def.race == raceDef &&
                                                                       (def.RaceProps?.Animal == true ||
                                                                        def.defaultFactionType?.isPlayer == true));
            if (pawnKindDef == null)
            {
                continue;
            }

            if (!Autotests_ColonyMaker.TryGetFreeRect(6, 3, out var cellRect))
            {
                return;
            }

            cellRect = cellRect.ContractedBy(1);
            foreach (var c in cellRect)
            {
                Autotests_ColonyMaker.Map.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
            }

            GenSpawn.Spawn(PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfPlayerSilentFail),
                cellRect.Cells.ElementAt(0),
                Autotests_ColonyMaker.Map);
            var intVec = cellRect.Cells.ElementAt(1);
            HealthUtility.DamageUntilDead((Pawn)GenSpawn.Spawn(
                PawnGenerator.GeneratePawn(pawnKindDef), intVec,
                Autotests_ColonyMaker.Map));
            var compRottable = ((Corpse)intVec.GetThingList(Find.CurrentMap).First(t => t is Corpse))
                .TryGetComp<CompRottable>();
            if (compRottable != null)
            {
                compRottable.RotProgress += 1200000f;
            }

            if (pawnKindDef.RaceProps.leatherDef != null)
            {
                GenSpawn.Spawn(pawnKindDef.RaceProps.leatherDef, cellRect.Cells.ElementAt(2),
                    Autotests_ColonyMaker.Map);
            }

            if (pawnKindDef.RaceProps.meatDef != null)
            {
                GenSpawn.Spawn(pawnKindDef.RaceProps.meatDef, cellRect.Cells.ElementAt(3), Autotests_ColonyMaker.Map);
            }
        }

        var designator_Build = new Designator_Build(ThingDefOf.PowerConduit);
        for (var i = Autotests_ColonyMaker.overRect.minX; i < Autotests_ColonyMaker.overRect.maxX; i++)
        {
            for (var j = Autotests_ColonyMaker.overRect.minZ; j < Autotests_ColonyMaker.overRect.maxZ; j += 7)
            {
                designator_Build.DesignateSingleCell(new IntVec3(i, 0, j));
            }
        }

        for (var k2 = Autotests_ColonyMaker.overRect.minZ; k2 < Autotests_ColonyMaker.overRect.maxZ; k2++)
        {
            for (var l = Autotests_ColonyMaker.overRect.minX; l < Autotests_ColonyMaker.overRect.maxX; l += 7)
            {
                designator_Build.DesignateSingleCell(new IntVec3(l, 0, k2));
            }
        }

        var alreadyBuiltBuildings = new List<ThingDef>();
        foreach (var thingDef in from def in DefDatabase<ThingDef>.AllDefs
                 where def.modContentPack == mod &&
                       typeof(Building_WorkTable).IsAssignableFrom(def.thingClass)
                 select def)
        {
            var thing2 = Autotests_ColonyMaker.TryMakeBuilding(thingDef);
            if (thing2 == null)
            {
                Log.Message($"Could not make worktable: {thingDef.defName}");
                break;
            }

            alreadyBuiltBuildings.Add(thingDef);
            if (thing2 is not Building_WorkTable building_WorkTable)
            {
                continue;
            }

            foreach (var recipe in building_WorkTable.def.AllRecipes)
            {
                building_WorkTable.billStack.AddBill(recipe.MakeNewBill());
            }
        }

        foreach (var thingDef2 in from def in DefDatabase<ThingDef>.AllDefs
                 where def.modContentPack == mod && def.category == ThingCategory.Building &&
                       def.BuildableByPlayer && !alreadyBuiltBuildings.Contains(def) && def != ThingDefOf.PowerConduit
                 select def)
        {
            if (Autotests_ColonyMaker.TryMakeBuilding(thingDef2) == null)
            {
                Log.Message($"Could not make building: {thingDef2.defName}");
            }
        }

        var itemDefs = (from def in DefDatabase<ThingDef>.AllDefs
            where def.modContentPack == mod && DebugThingPlaceHelper.IsDebugSpawnable(def) &&
                  def.category == ThingCategory.Item
            select def).ToList();
        if (itemDefs.Any())
        {
            if (Autotests_ColonyMaker.TryGetFreeRect(6, 6, out var placingRect))
            {
                var currentIndex = 0;
                foreach (var thingDef in itemDefs)
                {
                    if (currentIndex == 36)
                    {
                        currentIndex = 0;
                        if (!Autotests_ColonyMaker.TryGetFreeRect(6, 6, out placingRect))
                        {
                            Log.Message("Could not generate new item area");
                            break;
                        }
                    }

                    DebugThingPlaceHelper.DebugSpawn(thingDef, placingRect.Cells.ElementAt(currentIndex), -1, true);
                    currentIndex++;
                }
            }
        }

        var dummyZone = new Zone_Growing(Autotests_ColonyMaker.Map.zoneManager);
        Autotests_ColonyMaker.Map.zoneManager.RegisterZone(dummyZone);
        var allDefs = DefDatabase<ThingDef>.AllDefs;

        bool Predicate(ThingDef d)
        {
            return d.modContentPack == mod && d.plant != null && PlantUtility.CanSowOnGrower(d, dummyZone);
        }

        foreach (var plantDefToGrow in allDefs.Where(Predicate))
        {
            if (!Autotests_ColonyMaker.TryGetFreeRect(6, 6, out var cellRect6))
            {
                Log.Error("Could not get growing zone rect.");
            }

            cellRect6 = cellRect6.ContractedBy(1);
            foreach (var c3 in cellRect6)
            {
                Autotests_ColonyMaker.Map.terrainGrid.SetTerrain(c3, TerrainDefOf.Soil);
            }

            new Designator_ZoneAdd_Growing().DesignateMultiCell(cellRect6.Cells);
            if (Autotests_ColonyMaker.Map.zoneManager.ZoneAt(cellRect6.CenterCell) is Zone_Growing zone_Growing)
            {
                zone_Growing.SetPlantDefToGrow(plantDefToGrow);
            }
        }

        dummyZone.Delete();
        Autotests_ColonyMaker.ClearAllHomeArea();
        Autotests_ColonyMaker.FillWithHomeArea(Autotests_ColonyMaker.overRect);
        DebugSettings.godMode = godMode;
        Thing.allowDestroyNonDestroyable = false;
    }
}