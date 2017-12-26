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
    			boundMap[path] = ExtractBounds(graphic, type, headGraphic, headOffset);
    		}
    		return boundMap[path];
    	}
    	
    	public static Vector2 BoundMap(Graphic graphic, GraphicType type)
    	{
    		if (!boundMap.ContainsKey(graphic.path))
    		{
    			boundMap[graphic.path] = ExtractBounds(graphic, type);
    		}
    		return boundMap[graphic.path];
    	}
    	
    	private static Vector2 ExtractBounds(Graphic graphic, GraphicType type, Graphic headGraphic, Vector2 headOffset)
    	{
			int vWidth; int vHeight;
			
			var vBounds = Def_Extensions.CropVertical((graphic.MatSide.mainTexture as Texture2D).GetColorSafe(out vWidth, out vHeight), vWidth, vHeight);
			
			int hWidth; int hHeight;
			
			var hBounds = Def_Extensions.CropHorizontal((graphic.MatFront.mainTexture as Texture2D).GetColorSafe(out hWidth, out hHeight), hWidth, hHeight);
			
			int vWidthHead; int vHeightHead;
			
			var vTexHead = headGraphic.MatSide.mainTexture as Texture2D;
			var vBoundsHead = Def_Extensions.CropVertical(vTexHead.GetColorSafe(out vWidthHead, out vHeightHead), vWidthHead, vHeightHead);
			
			vBoundsHead.min += (int)(headOffset.y * (float)vHeightHead);
			vBoundsHead.max += (int)(headOffset.y * (float)vHeightHead);
			
			vBounds.max = Math.Max(vBounds.max, (int)((float)vBoundsHead.max * (float)vHeight / (float)vHeightHead));
			vBounds.min = Math.Min(vBounds.min, (int)((float)vBoundsHead.min * (float)vHeight / (float)vHeightHead));
			
			int hWidthHead; int hHeightHead;
			
			var hTexHead = headGraphic.MatFront.mainTexture as Texture2D;
			var hBoundsHead = Def_Extensions.CropVertical(hTexHead.GetColorSafe(out hWidthHead, out hHeightHead), hWidthHead, hHeightHead);
			
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
			
			var vBounds = Def_Extensions.CropVertical((graphic.MatSide.mainTexture as Texture2D).GetColorSafe(out vWidth, out vHeight), vWidth, vHeight);
			
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
			
			var hBounds = Def_Extensions.CropHorizontal((graphic.MatFront.mainTexture as Texture2D).GetColorSafe(out hWidth, out hHeight), hWidth, hHeight);
			
			return new Vector2(
					(float)(hBounds.max - hBounds.min) / (float)hWidth,
					(float)(vBounds.max - vBounds.min) / (float)vHeight);
    	}
    	
    	public static void Inject()
    	{
    		IEnumerable<Graphic> allPawnGraphics = DefDatabase<PawnKindDef>.AllDefs
    			.Where(x => !x.RaceProps.Humanlike)
    			.SelectMany<PawnKindDef, Graphic>(
    				x => x.lifeStages.SelectMany<PawnKindLifeStage, Graphic>(
    					y => {
			            	var a = new List<Graphic>();
			            	
			    			if (y.bodyGraphicData != null)
			    				a.Add(y.bodyGraphicData.Graphic);
			    			
			    			if (y.femaleGraphicData != null)
			    				a.Add(y.femaleGraphicData.Graphic);
			    			
			    			if (y.dessicatedBodyGraphicData != null)
			    				a.Add(y.dessicatedBodyGraphicData.Graphic);
			    			
			            	return a;
	    				 }))
    			.Distinct();
    		
    		foreach (Graphic graphic in allPawnGraphics)
    		{
    			boundMap[graphic.path] = ExtractBounds(graphic, GraphicType.Pawn);
    		}
    		
    		IEnumerable<Graphic> allPlantGraphics = DefDatabase<ThingDef>.AllDefs
    			.Where<ThingDef>(x => x.plant != null)
    			.SelectMany<ThingDef, Graphic>(y => {
					    var a = new List<Graphic>();
					    if (y != null)
					    {
						if (y.graphicData != null && y.graphicData.Graphic != null)
						    a.Add(y.graphicData.Graphic);
						if (y.plant.leaflessGraphic != null)
						    a.Add(y.plant.leaflessGraphic);
						if (y.plant.immatureGraphic != null)
						    a.Add(y.plant.immatureGraphic);
					    }
					    return a;
    			            })
    			.Distinct()
    			.Concat(new []{
    			        	(Graphic)(typeof(Plant).GetField("GraphicSowing", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null))
    			        });
    		
    		foreach (Graphic graphic in allPlantGraphics)
    		{
    			boundMap[graphic.path] = ExtractBounds(graphic, GraphicType.Plant);
    		}
    		
    		Log.Message("Combat Extended :: Bounds pre-generated");
    	}
    	
    	public static Vector2 ForPawn(Pawn pawn)
    	{
			if (pawn.RaceProps.Humanlike)
			{
				PawnRenderer renderer = pawn.Drawer.renderer;
				PawnGraphicSet graphicSet = renderer.graphics;
				
				if (!graphicSet.AllResolved)
					graphicSet.ResolveAllGraphics();
				
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
			else
			{
	    		PawnKindLifeStage lifeStage = pawn.ageTracker.CurKindLifeStage;
	    		
	    		GraphicData graphicData;
	    		
	    		if (pawn.IsDessicated() && lifeStage.dessicatedBodyGraphicData != null)
	    		{
	    			graphicData = lifeStage.dessicatedBodyGraphicData;
	    		}
	    		else if (pawn.gender == Gender.Female && lifeStage.femaleGraphicData != null)
	    		{
	    			graphicData = lifeStage.femaleGraphicData;
	    		}
	    		else
	    		{
	    			graphicData = lifeStage.bodyGraphicData;
	    		}
	    		
	    		return Vector2.Scale(BoundMap(graphicData.Graphic, GraphicType.Pawn), graphicData.drawSize);
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
