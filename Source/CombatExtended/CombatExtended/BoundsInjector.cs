using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using RimWorld;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;
namespace CombatExtended;

public static class BoundsInjector
{
    public enum GraphicType
    {
        Pawn,
        Plant
    }

    private static ConcurrentDictionary<string, Vector2> boundMap = [];
    private static ConcurrentDictionary<Texture2D, (Color[] pixels, int width, int height)> _textureCache = [];

    public static Vector2 BoundMap(Graphic graphic, GraphicType type)
    {
        if (boundMap.TryGetValue(graphic.path, out Vector2 cachedBounds))
        {
            return cachedBounds;
        }

        try
        {
            Vector2 bounds = ExtractBounds(graphic, type);
            boundMap[graphic.path] = bounds;
            return bounds;
        }
        catch (Exception e)
        {
            throw new Exception("BoundMap(,)", e);
        }
    }

    private static Vector2 ExtractBoundCollection(Graphic_Collection graphic, GraphicType type)
    {
        IEnumerable<Vector2> bounds = graphic.subGraphics.Select(x => ExtractBounds(x, type));
        return new Vector2(bounds.Average(v => v.x), bounds.Average(v => v.y));
    }

    private static Vector2 ExtractBounds(Graphic graphic, GraphicType type)
    {
        if (graphic is Graphic_Collection graphic_collection)
        {
            return ExtractBoundCollection(graphic_collection, type);
        }

        int vWidth;
        int vHeight;

        IntRange vBounds;

        try
        {
            vBounds = Def_Extensions.CropVertical(GetCachedColors((graphic.MatEast.mainTexture as Texture2D), out vWidth, out vHeight), vWidth, vHeight);
        }
        catch (Exception ex)
        {
            throw new Exception("Combat Extended :: CropVertical error while cropping Textures/" + graphic.path + "_side", ex);
        }

        //Plants only care for verts
        //This is assuming PLANTS TAKE UP A FULL TILE!!
        // TODO : Refactor
        if (type == GraphicType.Plant)
        {
            return new Vector2(1f, (vBounds.max - vBounds.min) / (float)vHeight);
        }

        int hWidth;
        int hHeight;

        IntRange hBounds;

        try
        {
            hBounds = Def_Extensions.CropHorizontal(GetCachedColors((graphic.MatSouth.mainTexture as Texture2D), out hWidth, out hHeight), hWidth, hHeight);
        }
        catch (Exception ex)
        {
            throw new Exception("Combat Extended :: CropHorizontal error while cropping Textures/" + graphic.path + "_front", ex);
        }

        return new Vector2(
            (hBounds.max - hBounds.min) / (float)hWidth,
            (vBounds.max - vBounds.min) / (float)vHeight);
    }

    private static void CollectGraphic(Graphic graphic, HashSet<Texture2D> textures)
    {
        if (graphic == null)
        {
            return;
        }
        if (graphic is Graphic_Collection col)
        {
            foreach (Graphic sub in col.subGraphics)
            {
                CollectGraphic(sub, textures);
            }
            return;
        }
        if (graphic.MatEast?.mainTexture is Texture2D east)
        {
            textures.Add(east);
        }
        if (graphic.MatSouth?.mainTexture is Texture2D south)
        {
            textures.Add(south);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color[] GetCachedColors(Texture2D tex, out int width, out int height)
    {
        if (_textureCache.TryGetValue(tex, out var cached))
        {
            width = cached.width;
            height = cached.height;
            return cached.pixels;
        }
        //Can't fall back to original GetColorSafe as it is not thread safe
        throw new Exception($"Combat Extended :: Texture '{tex.name}' was not cached. This is a bug - texture should have been collected in CollectTextures.");
    }


    public static void Inject()
    {
        List<PawnKindDef> pawnKindsToInject = [];
        foreach (PawnKindDef pawnKindDef in DefDatabase<PawnKindDef>.AllDefs)
        {
            if (!pawnKindDef.RaceProps.Humanlike)
            {
                pawnKindsToInject.Add(pawnKindDef);
            }
        }

        List<ThingDef> plantsToInject = [];
        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
        {
            if (thingDef.plant != null)
            {
                plantsToInject.Add(thingDef);
            }
        }

        // Textures need to be gathered on main thread
        // Bulk of the time for this method, but no way to speed up texture gathering
        HashSet<Texture2D> textures = CollectTextures(pawnKindsToInject, plantsToInject);

        int pending = textures.Count;
        foreach (Texture2D tex in textures)
        {
            // Needs to be done on main thread
            Texture2D capturedTex = tex;
            (int w, int h) = CE_Utility.GetScaledSize(tex);
            RenderTexture rt = CE_Utility.BlitToRenderTexture(tex, w, h);

            // Allows the GPU to do the work we need seperate from the CPU. This stops the CPU from stalling while the GPU does it's job
            AsyncGPUReadback.Request(rt, 0, request =>
            {
                RenderTexture.ReleaseTemporary(rt);

                if (!request.hasError)
                {
                    _textureCache[capturedTex] = (CE_Utility.ConvertToColors(request.GetData<Color32>()), w, h);
                }
                else
                {
                    Log.Error($"Combat Extended :: AsyncGPUReadback failed for {capturedTex.name}");
                }

                // Increments down as GPU gives the CPU the information. Only want to finish injecting after all textures are predone
                if (Interlocked.Decrement(ref pending) != 0)
                {
                    return;
                }

                GenThreading.ParallelForEach(pawnKindsToInject, InjectPawnKinds);
                GenThreading.ParallelForEach(plantsToInject, InjectPlants);
                Log.Message("Combat Extended :: Bounds pre-generated");

            });
        }
    }

    private static HashSet<Texture2D> CollectTextures(List<PawnKindDef> pawnKinds, List<ThingDef> plants)
    {
        HashSet<Texture2D> textures = [];

        for (int i = 0; i < pawnKinds.Count; i++)
        {
            PawnKindDef def = pawnKinds[i];
            foreach (PawnKindLifeStage ls in def.lifeStages)
            {
                CollectGraphic(ls.bodyGraphicData?.Graphic, textures);
                CollectGraphic(ls.femaleGraphicData?.Graphic, textures);
                CollectGraphic(ls.dessicatedBodyGraphicData?.Graphic, textures);
                CollectGraphic(ls.femaleDessicatedBodyGraphicData?.Graphic, textures);
            }
        }

        foreach (ThingDef def in plants)
        {
            CollectGraphic(def.graphicData?.Graphic, textures);
            CollectGraphic(def.plant?.leaflessGraphic, textures);
            CollectGraphic(def.plant?.immatureGraphic, textures);
        }

        CollectGraphic(Plant.GraphicSowing, textures);

        return textures;
    }

    private static void InjectPawnKinds(PawnKindDef pawnKindDef)
    {
        for (int i = 0; i < pawnKindDef.lifeStages.Count; i++)
        {
            PawnKindLifeStage lifeStage = pawnKindDef.lifeStages[i];

            try
            {
                if (lifeStage.bodyGraphicData != null && lifeStage.bodyGraphicData.Graphic != null)
                {
                    BoundMap(lifeStage.bodyGraphicData.Graphic, GraphicType.Pawn);
                }
            }
            catch (Exception e)
            {
                throw new Exception(pawnKindDef + ".lifeStages[" + i + "].bodyGraphicData", e);
            }

            try
            {
                if (lifeStage.femaleGraphicData != null && lifeStage.femaleGraphicData.Graphic != null)
                {
                    BoundMap(lifeStage.femaleGraphicData.Graphic, GraphicType.Pawn);
                }
            }
            catch (Exception e)
            {
                throw new Exception(pawnKindDef + ".lifeStages[" + i + "].femaleGraphicData", e);
            }

            try
            {
                if (lifeStage.dessicatedBodyGraphicData != null && lifeStage.dessicatedBodyGraphicData.Graphic != null)
                {
                    BoundMap(lifeStage.dessicatedBodyGraphicData.Graphic, GraphicType.Pawn);
                }
            }
            catch (Exception e)
            {
                throw new Exception(pawnKindDef + ".lifeStages[" + i + "].dessicatedBodyGraphicData", e);
            }

            try
            {
                if (lifeStage.femaleDessicatedBodyGraphicData != null && lifeStage.femaleDessicatedBodyGraphicData.Graphic != null)
                {
                    BoundMap(lifeStage.femaleDessicatedBodyGraphicData.Graphic, GraphicType.Pawn);
                }
            }
            catch (Exception e)
            {
                throw new Exception(pawnKindDef + ".lifeStages[" + i + "].femaleDessicatedBodyGraphicData", e);
            }
        }
    }

    private static void InjectPlants(ThingDef plantDef)
    {
        try
        {
            if (plantDef.graphicData != null && plantDef.graphicData.Graphic != null)
            {
                BoundMap(plantDef.graphicData.Graphic, GraphicType.Plant);
            }
        }
        catch (Exception e)
        {
            throw new Exception(plantDef + ".graphicData", e);
        }

        try
        {
            if (plantDef.plant.leaflessGraphic != null)
            {
                BoundMap(plantDef.plant.leaflessGraphic, GraphicType.Plant);
            }
        }
        catch (Exception e)
        {
            throw new Exception(plantDef + ".plant.leaflessGraphic", e);
        }

        try
        {
            if (plantDef.plant.immatureGraphic != null)
            {
                BoundMap(plantDef.plant.immatureGraphic, GraphicType.Plant);
            }
        }
        catch (Exception e)
        {
            throw new Exception(plantDef + ".plant.immatureGraphic", e);
        }
        Graphic graphicSowing = Plant.GraphicSowing;

        try
        {
            if (graphicSowing != null)
            {
                BoundMap(graphicSowing, GraphicType.Plant);
            }
        }
        catch (Exception e)
        {
            throw new Exception("GraphicSowing", e);
        }
    }

    public static Vector2 ForPawn(Pawn pawn)
    {
        if (pawn.RaceProps.Humanlike)
        {
            if (!Controller.settings.VariedHumanHeight)
            {
                return new Vector2(0.5f, 1);
            }

            //limit humans to never be taller than 3.5m (wall height)
            float height = Mathf.Min(pawn.BodySize, 2.0f);

            //slight height increase so that 13 y.o. can aim over embrasures and 8 y.o. can aim over sandbags
            if (height < 1)
            {
                height = Mathf.Min(height + 0.2f, 1);
            }

            return new Vector2(pawn.BodySize / 2f, height);

            // Disabling sprite bounds for humans for balance and game design reasons -NIA
            /*
            PawnRenderer renderer = pawn.Drawer.renderer;
            PawnGraphicSet graphicSet = renderer.graphics;

            if (!graphicSet.AllResolved)
                graphicSet.ResolveAllGraphics();

            try
            {
                if (pawn.IsDessicated() && graphicSet.dessicatedGraphic != null)
                {
                    return BoundMap(
                        graphicSet.dessicatedGraphic,
                        GraphicType.Pawn,
                        graphicSet.desiccatedHeadGraphic,
                        new Vector2(renderer.BaseHeadOffsetAt(Rot4.South).x,
                                    renderer.BaseHeadOffsetAt(Rot4.East).z));
                }

                return BoundMap(
                    graphicSet.nakedGraphic,
                    GraphicType.Pawn,
                    graphicSet.headGraphic,
                    new Vector2(renderer.BaseHeadOffsetAt(Rot4.South).x,
                                renderer.BaseHeadOffsetAt(Rot4.East).z));
            }
            catch (ArgumentException e) {   throw new ArgumentException(pawn+".graphics."+(pawn.IsDessicated() ? "dessicated/dessicatedHead" : "naked/head")+"Graphic", e); }
            */
        }
        else
        {
            //Revert to old system:
            //return new Vector2(pawn.BodySize, pawn.BodySize);

            PawnKindLifeStage lifeStage = pawn.ageTracker.CurKindLifeStage;

            //Exact mimick of PawnGraphicSet
            GraphicData data = pawn.IsDessicated() && lifeStage.dessicatedBodyGraphicData != null
                               ? (pawn.gender != Gender.Female || lifeStage.femaleDessicatedBodyGraphicData == null)
                               ? lifeStage.dessicatedBodyGraphicData
                               : lifeStage.femaleDessicatedBodyGraphicData
                               : (pawn.gender != Gender.Female || lifeStage.femaleGraphicData == null)
                               ? lifeStage.bodyGraphicData
                               : lifeStage.femaleGraphicData;

            var name = pawn.IsDessicated() && lifeStage.dessicatedBodyGraphicData != null
                       ? (pawn.gender != Gender.Female || lifeStage.femaleDessicatedBodyGraphicData == null)
                       ? "dessicatedBodyGraphicData"
                       : "femaleDessicatedBodyGraphicData"
                       : (pawn.gender != Gender.Female || lifeStage.femaleGraphicData == null)
                       ? "bodyGraphicData"
                       : "femaleGraphicData";

            var graphic = data.Graphic;
            var size = data.drawSize;

            if (!pawn.kindDef.alternateGraphics.NullOrEmpty())
            {
                //TODO: 1.5
                //if (!pawn.Drawer.renderer.graphics.AllResolved)
                //{
                //pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                //}

                //name = "alternateGraphics";
                //graphic = pawn.Drawer.renderer.graphics.nakedGraphic;
            }

            if (graphic == null)
            {
                Log.Error(pawn + ".lifeStage[" + pawn.ageTracker.CurLifeStageIndex + "]." + name + " could not be found");
                return Vector2.zero;
            }
            else
            {
                try
                {
                    return Vector2.Scale(BoundMap(graphic, GraphicType.Pawn), size);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(pawn + ".lifeStage[" + pawn.ageTracker.CurLifeStageIndex + "]." + name, e);
                }
            }
        }
    }

    public static Vector2 ForPlant(Plant plant)
    {
        return plant.def.plant.visualSizeRange.LerpThroughRange(plant.Growth) * BoundMap(plant.Graphic, GraphicType.Plant);
    }

}
