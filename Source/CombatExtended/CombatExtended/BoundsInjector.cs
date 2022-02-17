using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
	public static class BoundsInjector
	{
		public enum GraphicType
		{
			Pawn,
			Plant
		}
    	
    	private static Dictionary<string, Vector2> boundMap = new Dictionary<string, Vector2>();
    	
    	public static Vector2 BoundMap(Graphic graphic, GraphicType type, Graphic headGraphic, Vector2 headOffset)
    	{
    		string path = graphic.path + (headGraphic == null ? "" : "+"+headGraphic.path);
    		if (!boundMap.ContainsKey(path))
    		{
    			try {	boundMap[path] = ExtractBounds(graphic, type, headGraphic, headOffset);	}
    			catch (Exception e) {	throw new Exception("BoundMap(,,,)", e);	}
    		}
    		return boundMap[path];
    	}
    	
    	public static Vector2 BoundMap(Graphic graphic, GraphicType type)
    	{
    		if (!boundMap.ContainsKey(graphic.path))
    		{
    			try {	boundMap[graphic.path] = ExtractBounds(graphic, type);	}
    			catch (Exception e) {	throw new Exception("BoundMap(,)", e);	}
    			
    		}
    		return boundMap[graphic.path];
    	}
    	
    	private static Vector2 ExtractBounds(Graphic graphic, GraphicType type, Graphic headGraphic, Vector2 headOffset)
    	{
			int vWidth; int vHeight;
			
			IntRange vBounds;
			
			try {	vBounds = Def_Extensions.CropVertical((graphic.MatEast.mainTexture as Texture2D).GetColorSafe(out vWidth, out vHeight), vWidth, vHeight);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropVertical error while cropping Textures/" + graphic.path+"_side",ex);	}
			
			int hWidth; int hHeight;
			
			IntRange hBounds;
			
			try {	hBounds = Def_Extensions.CropHorizontal((graphic.MatSouth.mainTexture as Texture2D).GetColorSafe(out hWidth, out hHeight), hWidth, hHeight);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropHorizontal error while cropping Textures/" + graphic.path+"_front",ex);	}
			
			int vWidthHead; int vHeightHead;
			
			IntRange vBoundsHead;
			
    		try {	vBoundsHead = Def_Extensions.CropVertical((headGraphic.MatEast.mainTexture as Texture2D).GetColorSafe(out vWidthHead, out vHeightHead), vWidthHead, vHeightHead);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropVertical error while cropping Textures/" + headGraphic.path+"_side",ex);	}
			
			vBoundsHead.min -= (int)(headOffset.y * (float)vHeightHead);
			vBoundsHead.max -= (int)(headOffset.y * (float)vHeightHead);
			
			vBounds.min = Math.Min(vBounds.min, (int)((float)vBoundsHead.min * (float)vHeight / (float)vHeightHead));
			vBounds.max = Math.Max(vBounds.max, (int)((float)vBoundsHead.max * (float)vHeight / (float)vHeightHead));
			
			int hWidthHead; int hHeightHead;
			
			IntRange hBoundsHead;
			
    		try {	hBoundsHead = Def_Extensions.CropHorizontal((headGraphic.MatSouth.mainTexture as Texture2D).GetColorSafe(out hWidthHead, out hHeightHead), hWidthHead, hHeightHead);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropHorizontal error while cropping Textures/" + headGraphic.path+"_front",ex);	}
			
			hBoundsHead.min += (int)(headOffset.x * (float)hWidthHead);
			hBoundsHead.max += (int)(headOffset.x * (float)hWidthHead);
			
			hBounds.max = Math.Max(hBounds.max, (int)((float)hBoundsHead.max * (float)hWidth / (float)hWidthHead));
			hBounds.min = Math.Min(hBounds.min, (int)((float)hBoundsHead.min * (float)hWidth / (float)hWidthHead));
			
			return new Vector2(
					(float)(hBounds.max - hBounds.min) / (float)hWidth,
					(float)(vBounds.max - vBounds.min) / (float)vHeight);
    	}
    	
    	private static Vector2 ExtractBounds(Graphic graphic, GraphicType type)
    	{
			int vWidth; int vHeight;
			
			IntRange vBounds;
			
			try {	vBounds = Def_Extensions.CropVertical((graphic.MatEast.mainTexture as Texture2D).GetColorSafe(out vWidth, out vHeight), vWidth, vHeight);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropVertical error while cropping Textures/" + graphic.path+"_side",ex);	}
			
				//Plants only care for verts
				//This is assuming PLANTS TAKE UP A FULL TILE!!
				// TODO : Refactor
			if (type == GraphicType.Plant)
			{
				return new Vector2(
					1f,
					(float)(vBounds.max - vBounds.min) / (float)vHeight);
			}
			
			int hWidth; int hHeight;
			
			IntRange hBounds;
			
			try {	hBounds = Def_Extensions.CropHorizontal((graphic.MatSouth.mainTexture as Texture2D).GetColorSafe(out hWidth, out hHeight), hWidth, hHeight);	}
    		catch(Exception ex) {	throw new Exception("Combat Extended :: CropHorizontal error while cropping Textures/" + graphic.path+"_front",ex);	}
			
			return new Vector2(
					(float)(hBounds.max - hBounds.min) / (float)hWidth,
					(float)(vBounds.max - vBounds.min) / (float)vHeight);
    	}
    	
    	public static void Inject()
    	{
    		foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefs.Where(x => !x.RaceProps.Humanlike))
    		{
    			for (int i = 0; i < def.lifeStages.Count; i++)
    			{
    				PawnKindLifeStage lifeStage = def.lifeStages[i];
    				
    				try {	if (lifeStage.bodyGraphicData != null && lifeStage.bodyGraphicData.Graphic != null)
    						BoundMap(lifeStage.bodyGraphicData.Graphic, GraphicType.Pawn);	}
    				catch (Exception e) {	throw new Exception(def+".lifeStages["+i+"].bodyGraphicData", e);	}
			    	
    				try {	if (lifeStage.femaleGraphicData != null && lifeStage.femaleGraphicData.Graphic != null)
    						BoundMap(lifeStage.femaleGraphicData.Graphic, GraphicType.Pawn);	}
    				catch (Exception e) {	throw new Exception(def+".lifeStages["+i+"].femaleGraphicData", e);	}
			    	
    				try {	if (lifeStage.dessicatedBodyGraphicData != null && lifeStage.dessicatedBodyGraphicData.Graphic != null)
    						BoundMap(lifeStage.dessicatedBodyGraphicData.Graphic, GraphicType.Pawn);	}
    				catch (Exception e) {	throw new Exception(def+".lifeStages["+i+"].dessicatedBodyGraphicData", e); }

                    try {   if (lifeStage.femaleDessicatedBodyGraphicData != null && lifeStage.femaleDessicatedBodyGraphicData.Graphic != null)
                            BoundMap(lifeStage.femaleDessicatedBodyGraphicData.Graphic, GraphicType.Pawn);    }
                    catch (Exception e) {   throw new Exception(def+".lifeStages["+i+"].femaleDessicatedBodyGraphicData", e); }
                }
    		}
    		
    		foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where<ThingDef>(x => x.plant != null))
    		{
				try {	if (def.graphicData != null && def.graphicData.Graphic != null)
    					BoundMap(def.graphicData.Graphic, GraphicType.Plant);	}
				catch (Exception e) {	throw new Exception(def+".graphicData", e);	}
				
				try {	if (def.plant.leaflessGraphic != null)
    					BoundMap(def.plant.leaflessGraphic, GraphicType.Plant);	}
				catch (Exception e) {	throw new Exception(def+".plant.leaflessGraphic", e);	}
				
				try {	if (def.plant.immatureGraphic != null)
    					BoundMap(def.plant.immatureGraphic, GraphicType.Plant);	}
				catch (Exception e) {	throw new Exception(def+".plant.immatureGraphic", e);	}
    		}
    		
    		Graphic graphicSowing = Plant.GraphicSowing;
    		
			try {	if (graphicSowing != null)
					BoundMap(graphicSowing, GraphicType.Plant);	}
			catch (Exception e) {	throw new Exception("GraphicSowing", e);	}
    		
    		Log.Message("Combat Extended :: Bounds pre-generated");
    	}
    	
    	public static Vector2 ForPawn(Pawn pawn)
    	{
			if (pawn.RaceProps.Humanlike)
			{
                return new Vector2(0.5f,1);

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
				catch (ArgumentException e) {	throw new ArgumentException(pawn+".graphics."+(pawn.IsDessicated() ? "dessicated/dessicatedHead" : "naked/head")+"Graphic", e);	}
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
                    if (!pawn.Drawer.renderer.graphics.AllResolved)
                        pawn.Drawer.renderer.graphics.ResolveAllGraphics();

                    name = "alternateGraphics";
                    graphic = pawn.Drawer.renderer.graphics.nakedGraphic;
                }
                
                if (graphic == null)
                {
                    Log.Error(pawn + ".lifeStage[" + pawn.ageTracker.CurLifeStageIndex + "]."+name+" could not be found");
                    return Vector2.zero;
                }
                else
                {
                    try { return Vector2.Scale(BoundMap(graphic, GraphicType.Pawn), size); }
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
    	
    	/*public static void LogDatabase()
    	{
    		var str = new StringBuilder();
    		
    		str.AppendLine("PAWNS");
    		
    		foreach (PawnKindDef kindDef in DefDatabase<PawnKindDef>.AllDefs.Where(x => !x.race.race.Humanlike))
    		{
    			str.AppendLine(kindDef.ToString() + ", " + kindDef.race.race.baseBodySize +", " + String.Join(", ", boundMap[kindDef.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Key+"="+x.Value.Second).ToArray()));
    		}
    		
    		str.AppendLine();
    		str.AppendLine("PLANTS");
    		
    		//Fire is dummy name for a sowing plant's texture
    		str.AppendLine("Sowing Plant, " + String.Join(", ", boundMap[ThingDefOf.Fire.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Key+"="+x.Value.Second).ToArray()));
    		
    		foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null))
    		{
    			str.AppendLine(plantDef.ToString() + ", " + plantDef.fillPercent + ", " +plantDef.plant.visualSizeRange.ToString() +", " + String.Join(", ", boundMap[plantDef.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Value.Second).ToArray()));
    		}
    		
    		Log.Message(str.ToString());
    	}*/
	}
}
