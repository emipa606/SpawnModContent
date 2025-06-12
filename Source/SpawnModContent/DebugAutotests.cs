using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SpawnModContent;

public static class DebugAutotests
{
    private static BoolGrid usedCells;
    private static CellRect overRect;


    [DebugAction("Autotests", "Spawn mod content...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void DoSpawnModContent()
    {
        var list = new List<DebugMenuOption>();

        foreach (var mod in LoadedModManager.RunningMods
                     .Where(mod => mod.Name != "Spawn Mod Content" && mod.AllDefs.Any())
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
        if (usedCells == null)
        {
            usedCells = new BoolGrid(Find.CurrentMap);
        }
        else
        {
            usedCells.ClearAndResizeTo(Find.CurrentMap);
        }

        var spawnWidth = Math.Min(Find.CurrentMap.Size.x, 100);
        var spawnHeight = Math.Min(Find.CurrentMap.Size.z, 100);

        overRect = new CellRect(Find.CurrentMap.Center.x - (spawnWidth / 2),
            Find.CurrentMap.Center.z - (spawnHeight / 2), spawnWidth, spawnHeight);

        //Autotests_ColonyMaker.DeleteAllSpawnedPawns(); Vanilla code does not check for relation object, causes issues with mechs.
        foreach (var pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList())
        {
            pawn.Destroy();
            pawn.relations?.ClearAllRelations();
        }

        Find.GameEnder.gameEnding = false;

        overRect.ClipInsideMap(Find.CurrentMap);
        foreach (var item in overRect)
        {
            Find.CurrentMap.roofGrid.SetRoof(item, null);
        }

        foreach (var item2 in overRect)
        {
            foreach (var item3 in item2.GetThingList(Find.CurrentMap).ToList())
            {
                try
                {
                    item3.Destroy();
                }
                catch
                {
                    // ignored
                }
            }
        }

        TryMakeBuilding(ThingDef.Named("SpawnModContent_PowerNode"));

        Log.Message("[SpawnModContent]: Searching for races to spawn.");
        var allRaceDefs = DefDatabase<ThingDef>.AllDefs.Where(k => k.modContentPack == mod && k.race != null);
        var spawnPawnKinds = new List<PawnKindDef>();
        if (allRaceDefs.Any())
        {
            Log.Message(
                $"[SpawnModContent]: Trying to spawn all playable pawnkinds, animals with corpses, leathers and meat from {allRaceDefs.Count()} raceDefs.");

            foreach (var raceDef in allRaceDefs)
            {
                var pawnKindDef = DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault(def => def.race == raceDef);
                if (pawnKindDef == null)
                {
                    continue;
                }

                if (spawnPawnKinds.Contains(pawnKindDef))
                {
                    continue;
                }

                if (pawnKindDef is CreepJoinerFormKindDef)
                {
                    continue;
                }

                if (raceDef.thingClass.Name.Contains("Vehicle"))
                {
                    continue;
                }

                if (!TryGetFreeRect(6, 3, out var cellRect))
                {
                    return;
                }

                spawnPawnKinds.Add(pawnKindDef);

                cellRect = cellRect.ContractedBy(1);
                foreach (var c in cellRect)
                {
                    Find.CurrentMap.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
                }

                var faction = FactionUtility.DefaultFactionFrom(pawnKindDef.defaultFactionDef);
                var pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                GenSpawn.Spawn(pawn, cellRect.Cells.ElementAt(0), Find.CurrentMap);
                PostPawnSpawn(pawn);

                pawn = PawnGenerator.GeneratePawn(pawnKindDef, faction);
                GenSpawn.Spawn(pawn, cellRect.Cells.ElementAt(1), Find.CurrentMap);
                PostPawnSpawn(pawn);
                try
                {
                    HealthUtility.DamageUntilDead(pawn);
                }
                catch
                {
                    pawn.Kill(null);
                }

                var compRottable = ((Corpse)cellRect.Cells.ElementAt(1).GetThingList(Find.CurrentMap)
                        .FirstOrDefault(t => t is Corpse))
                    ?.TryGetComp<CompRottable>();
                if (compRottable != null)
                {
                    compRottable.RotProgress += 1200000f;
                }

                if (pawnKindDef.RaceProps.leatherDef != null)
                {
                    GenSpawn.Spawn(pawnKindDef.RaceProps.leatherDef, cellRect.Cells.ElementAt(2),
                        Find.CurrentMap);
                }

                if (pawnKindDef.RaceProps.meatDef != null)
                {
                    GenSpawn.Spawn(pawnKindDef.RaceProps.meatDef, cellRect.Cells.ElementAt(3),
                        Find.CurrentMap);
                }
            }

            Log.Message($"[SpawnModContent]: Spawned {spawnPawnKinds.Count} pawnkinds.");
        }

        Find.GameEnder.gameEnding = false;

        var designator_Build = new Designator_Build(ThingDefOf.PowerConduit);
        for (var i = overRect.minX; i < overRect.maxX; i++)
        {
            for (var j = overRect.minZ; j < overRect.maxZ; j += 7)
            {
                designator_Build.DesignateSingleCell(new IntVec3(i, 0, j));
            }
        }

        for (var k2 = overRect.minZ; k2 < overRect.maxZ; k2++)
        {
            for (var l = overRect.minX; l < overRect.maxX; l += 7)
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
                var thing2 = TryMakeBuilding(thingDef);
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
                if (TryMakeBuilding(thingDef2) == null)
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
            if (TryGetFreeRect(6, 6, out var placingRect))
            {
                var currentIndex = 0;
                foreach (var thingDef in thingDefs)
                {
                    if (currentIndex == 36)
                    {
                        currentIndex = 0;
                        if (!TryGetFreeRect(6, 6, out placingRect))
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

        Log.Message("[SpawnModContent]: Searching for terrain to spawn.");
        var terrainDefs = DefDatabase<TerrainDef>.AllDefs.Where(def => def.modContentPack == mod);
        if (terrainDefs.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {terrainDefs.Count()} terrain.");
            if (TryGetFreeRect(6, 6, out var placingRect))
            {
                var currentIndex = 0;
                foreach (var terrainDef in terrainDefs)
                {
                    if (currentIndex == 36)
                    {
                        currentIndex = 0;
                        if (!TryGetFreeRect(6, 6, out placingRect))
                        {
                            Log.Message("Could not generate new item area");
                            break;
                        }
                    }

                    Find.CurrentMap.terrainGrid.SetTerrain(placingRect.Cells.ElementAt(currentIndex), terrainDef);
                    currentIndex++;
                }
            }
        }


        Log.Message("[SpawnModContent]: Searching for plants to grow.");
        var dummyZone = new Zone_Growing(Find.CurrentMap.zoneManager);

        var plantDefsToGrow = DefDatabase<ThingDef>.AllDefs.Where(Predicate);

        if (plantDefsToGrow.Any())
        {
            Log.Message($"[SpawnModContent]: Trying to spawn {plantDefsToGrow.Count()} plants to grow.");
            Find.CurrentMap.zoneManager.RegisterZone(dummyZone);

            foreach (var plantDefToGrow in plantDefsToGrow)
            {
                if (!TryGetFreeRect(5, 5, out var cellRect5))
                {
                    Log.Error("Could not get growing zone rect.");
                }

                cellRect5 = cellRect5.ContractedBy(1);
                foreach (var c3 in cellRect5)
                {
                    Find.CurrentMap.terrainGrid.SetTerrain(c3, TerrainDefOf.Soil);
                }

                new Designator_ZoneAdd_Growing().DesignateMultiCell(cellRect5.Cells);
                if (Find.CurrentMap.zoneManager.ZoneAt(cellRect5.CenterCell) is not Zone_Growing zone_Growing)
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
                try
                {
                    Find.CurrentMap.mapDrawer.SectionAt(cellRect5.CenterCell).RegenerateAllLayers();
                }
                catch
                {
                    // ignored
                }
            }
        }

        dummyZone.Delete();
        ClearAllHomeArea();
        FillWithHomeArea(overRect);
        DebugSettings.godMode = godMode;
        Thing.allowDestroyNonDestroyable = false;
        return;

        bool Predicate(ThingDef d)
        {
            return d.modContentPack == mod && d.plant != null; // && PlantUtility.CanSowOnGrower(d, dummyZone);
        }
    }

    private static void PostPawnSpawn(Pawn pawn)
    {
        if (pawn.Spawned && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
        {
            Lord lord = null;
            if (pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any(p => p != pawn))
            {
                lord = ((Pawn)GenClosest.ClosestThing_Global(pawn.Position,
                    pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction), 99999f,
                    p => p != pawn && ((Pawn)p).GetLord() != null)).GetLord();
            }

            if (lord == null || !lord.CanAddPawn(pawn))
            {
                lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_DefendPoint(pawn.Position), Find.CurrentMap);
            }

            if (lord != null && lord.LordJob.CanAutoAddPawns)
            {
                lord.AddPawn(pawn);
            }
        }

        pawn.Rotation = Rot4.South;
    }

    private static Thing TryMakeBuilding(ThingDef def)
    {
        if (!TryGetFreeRect(def.size.x + 2, def.size.z + 2, out var cellRect))
        {
            return null;
        }

        foreach (var c in cellRect)
        {
            Find.CurrentMap.terrainGrid.SetTerrain(c,
                def.terrainAffordanceNeeded?.defName == "ShallowWater"
                    ? TerrainDefOf.WaterShallow
                    : TerrainDefOf.Concrete);
        }

        cellRect.maxX -= 1;
        cellRect.maxZ -= 1;
        try
        {
            new Designator_Build(def).DesignateSingleCell(cellRect.CenterCell);
            if (def.thingClass?.Name.Contains("Vehicle") == true)
            {
                return cellRect.CenterCell.GetFirstPawn(Find.CurrentMap);
            }

            return cellRect.CenterCell.GetFirstBuilding(Find.CurrentMap);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetFreeRect(int width, int height, out CellRect result)
    {
        for (var i = overRect.minZ; i <= overRect.maxZ - height; i++)
        {
            for (var j = overRect.minX; j <= overRect.maxX - width; j++)
            {
                var cellRect = new CellRect(j, i, width, height);
                var allCellsUnused = true;
                for (var minZ = cellRect.minZ; minZ <= cellRect.maxZ; minZ++)
                {
                    for (var minX = cellRect.minX; minX <= cellRect.maxX; minX++)
                    {
                        if (!usedCells[minX, minZ])
                        {
                            continue;
                        }

                        allCellsUnused = false;
                        break;
                    }

                    if (!allCellsUnused)
                    {
                        break;
                    }
                }

                if (!allCellsUnused)
                {
                    continue;
                }

                result = cellRect;
                for (var minZ = cellRect.minZ; minZ <= cellRect.maxZ; minZ++)
                {
                    for (var minX = cellRect.minX; minX <= cellRect.maxX; minX++)
                    {
                        var c = new IntVec3(minX, 0, minZ);
                        usedCells.Set(c, true);
                        if (c.GetTerrain(Find.CurrentMap).passability == Traversability.Impassable)
                        {
                            Find.CurrentMap.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
                        }
                    }
                }

                return true;
            }
        }

        result = new CellRect(0, 0, width, height);
        return false;
    }

    private static void ClearAllHomeArea()
    {
        foreach (var c in Find.CurrentMap.AllCells)
        {
            Find.CurrentMap.areaManager.Home[c] = false;
        }
    }

    private static void FillWithHomeArea(CellRect r)
    {
        new Designator_AreaHomeExpand().DesignateMultiCell(r.Cells);
    }
}