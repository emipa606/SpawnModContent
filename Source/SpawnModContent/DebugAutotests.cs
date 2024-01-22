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

        //Autotests_ColonyMaker.DeleteAllSpawnedPawns(); Vanilla code does not check for relation object, causes issues with mechs.
        foreach (var pawn in Autotests_ColonyMaker.Map.mapPawns.AllPawnsSpawned.ToList())
        {
            pawn.Destroy();
            pawn.relations?.ClearAllRelations();
        }

        Find.GameEnder.gameEnding = false;

        GenDebug.ClearArea(Autotests_ColonyMaker.overRect, Find.CurrentMap);
        Autotests_ColonyMaker.TryMakeBuilding(ThingDef.Named("SpawnModContent_PowerNode"));

        Log.Message("[SpawnModContent]: Searching for races to spawn.");
        var allRaceDefs = DefDatabase<ThingDef>.AllDefs.Where(k => k.modContentPack == mod && k.race != null);
        if (allRaceDefs.Any())
        {
            Log.Message(
                $"[SpawnModContent]: Trying to spawn all playable pawnkinds, animals with corpses, leathers and meat from {allRaceDefs.Count()} raceDefs.");

            foreach (var raceDef in allRaceDefs)
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

                var compRottable = ((Corpse)intVec.GetThingList(Find.CurrentMap).FirstOrDefault(t => t is Corpse))
                    ?.TryGetComp<CompRottable>();
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
                    GenSpawn.Spawn(pawnKindDef.RaceProps.meatDef, cellRect.Cells.ElementAt(3),
                        Autotests_ColonyMaker.Map);
                }
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

        Log.Message("[SpawnModContent]: Searching for workbenches to spawn.");
        var alreadyBuiltBuildings = new List<ThingDef>();
        var workBenches = DefDatabase<ThingDef>.AllDefs.Where(def => def.modContentPack == mod &&
                                                                     typeof(Building_WorkTable).IsAssignableFrom(
                                                                         def.thingClass));
        if (workBenches.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {workBenches.Count()} workTables with all recipes.");
            foreach (var thingDef in workBenches)
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
        }

        Log.Message("[SpawnModContent]: Searching for buildings to spawn.");
        var allOtherBuildings = DefDatabase<ThingDef>.AllDefs.Where(def =>
            def.modContentPack == mod && def.category == ThingCategory.Building && def.BuildableByPlayer &&
            !alreadyBuiltBuildings.Contains(def) && def != ThingDefOf.PowerConduit);
        if (allOtherBuildings.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {allOtherBuildings.Count()} buildings.");
            foreach (var thingDef2 in allOtherBuildings)
            {
                if (Autotests_ColonyMaker.TryMakeBuilding(thingDef2) == null)
                {
                    Log.Message($"Could not make building: {thingDef2.defName}");
                }
            }
        }

        Log.Message("[SpawnModContent]: Searching for items to spawn.");
        var thingDefs = DefDatabase<ThingDef>.AllDefs.Where(def =>
            def.modContentPack == mod && DebugThingPlaceHelper.IsDebugSpawnable(def) &&
            def.category == ThingCategory.Item);

        if (thingDefs.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {thingDefs.Count()} items.");
            if (Autotests_ColonyMaker.TryGetFreeRect(6, 6, out var placingRect))
            {
                var currentIndex = 0;
                foreach (var thingDef in thingDefs)
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


        Log.Message("[SpawnModContent]: Searching for plants to grow.");
        var dummyZone = new Zone_Growing(Autotests_ColonyMaker.Map.zoneManager);

        var plantDefsToGrow = DefDatabase<ThingDef>.AllDefs.Where(Predicate);

        if (plantDefsToGrow.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {plantDefsToGrow.Count()} plants to grow.");
            Autotests_ColonyMaker.Map.zoneManager.RegisterZone(dummyZone);

            foreach (var plantDefToGrow in plantDefsToGrow)
            {
                if (!Autotests_ColonyMaker.TryGetFreeRect(5, 5, out var cellRect5))
                {
                    Log.Error("Could not get growing zone rect.");
                }

                cellRect5 = cellRect5.ContractedBy(1);
                foreach (var c3 in cellRect5)
                {
                    Autotests_ColonyMaker.Map.terrainGrid.SetTerrain(c3, TerrainDefOf.Soil);
                }

                new Designator_ZoneAdd_Growing().DesignateMultiCell(cellRect5.Cells);
                if (Autotests_ColonyMaker.Map.zoneManager.ZoneAt(cellRect5.CenterCell) is not Zone_Growing zone_Growing)
                {
                    continue;
                }

                zone_Growing.SetPlantDefToGrow(plantDefToGrow);
                DebugThingPlaceHelper.DebugSpawn(plantDefToGrow, cellRect5.CenterCell, -1, true, canBeMinified: false);
                var plant = cellRect5.CenterCell.GetPlant(Find.CurrentMap);
                if (plant?.def.plant == null)
                {
                    continue;
                }

                var num = (int)((1f - plant.Growth) * plant.def.plant.growDays);
                plant.Age += num;
                plant.Growth = 1f;
                Find.CurrentMap.mapDrawer.SectionAt(cellRect5.CenterCell).RegenerateAllLayers();
            }
        }

        dummyZone.Delete();
        Autotests_ColonyMaker.ClearAllHomeArea();
        Autotests_ColonyMaker.FillWithHomeArea(Autotests_ColonyMaker.overRect);
        DebugSettings.godMode = godMode;
        Thing.allowDestroyNonDestroyable = false;
        return;

        bool Predicate(ThingDef d)
        {
            return d.modContentPack == mod && d.plant != null; // && PlantUtility.CanSowOnGrower(d, dummyZone);
        }
    }
}