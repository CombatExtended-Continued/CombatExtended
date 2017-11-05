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
		private static FieldInfo sowingPlantMatInfo = typeof(Plant).GetField("GraphicSowing", BindingFlags.NonPublic | BindingFlags.Static);
		
    	public class TextureBoundFactor
    	{
    		enum GraphicType
    		{
    			Male,
    			Female,
    			Dessicated,
    			Plant,
    			LeaflessPlant,
    			ImmaturePlant
    		}
    		
    		public Dictionary<string, Pair<float, float>> dict = new Dictionary<string, Pair<float, float>>();
    		
    		public TextureBoundFactor(PawnKindDef pawnKindDef)
    		{
    			int i = 0;
    			foreach (PawnKindLifeStage lifeStage in pawnKindDef.lifeStages)
    			{
	    			if (lifeStage.bodyGraphicData != null)
	    				ExtractBounds(lifeStage.bodyGraphicData.Graphic, GraphicType.Male, i);
	    			
	    			if (lifeStage.femaleGraphicData != null)
	    				ExtractBounds(lifeStage.femaleGraphicData.Graphic, GraphicType.Female, i);
	    			
	    			if (lifeStage.dessicatedBodyGraphicData != null)
	    				ExtractBounds(lifeStage.dessicatedBodyGraphicData.Graphic, GraphicType.Dessicated, i);
    				i++;
    			}
    		}
    		
    			//Used for the sowing plant graphic
    		public TextureBoundFactor(Graphic graphic)
    		{
    			if (graphic != null)
    				ExtractBounds(graphic, GraphicType.Plant);
    		}
    		
    		public TextureBoundFactor(ThingDef plantDef)
    		{
    			if (plantDef.graphicData.Graphic != null)
	    			ExtractBounds(plantDef.graphicData.Graphic, GraphicType.Plant);
    			
    			if (plantDef.plant.leaflessGraphic != null)
	    			ExtractBounds(plantDef.plant.leaflessGraphic, GraphicType.LeaflessPlant);
    			
    			if (plantDef.plant.immatureGraphic != null)
	    			ExtractBounds(plantDef.plant.immatureGraphic, GraphicType.ImmaturePlant);
    		}
    		
    		public Pair<float, float> PawnFor(Pawn pawn)
    		{
    			int i = pawn.ageTracker.CurLifeStageIndex;
    			if (pawn.IsDessicated() && pawn.kindDef.lifeStages[i].dessicatedBodyGraphicData != null)
    			{
    				return dict[GraphicType.Dessicated.ToString() + "." + i];
    			}
    			if (pawn.gender == Gender.Female && pawn.kindDef.lifeStages[i].femaleGraphicData != null)
    			{
    				return dict[GraphicType.Female.ToString() + "." + i];
    			}
    			return dict[GraphicType.Male.ToString() + "." + i];
    		}
    		
    		public Pair<float, float> PlantFor(Plant plant)
    		{
    			Pair<float, float> pair;
    			if (plant.LifeStage == PlantLifeStage.Sowing)
    			{
    				return new Pair<float, float>
    					(0f, plant.def.plant.visualSizeRange.min
    					 * boundMap[ThingDefOf.Fire.defName].dict[GraphicType.Plant.ToString()].Second);
    			}
    			
    			if (plant.LeaflessNow && plant.def.plant.leaflessGraphic != null)
    			{
    				pair = dict[GraphicType.LeaflessPlant.ToString()];
    			}
    			else if (!plant.HarvestableNow && plant.def.plant.immatureGraphic != null)
    			{
    				pair = dict[GraphicType.ImmaturePlant.ToString()];
    			}
    			else
    			{
    				pair = dict[GraphicType.Plant.ToString()];
    			}
    			
    			float size = plant.def.plant.visualSizeRange.LerpThroughRange(plant.Growth);
    			
				return new Pair<float, float>(pair.First * size, pair.Second * size);
    		}
    		
    		private void ExtractBounds(Graphic graphic, GraphicType type, int lifeStageIndex = 0)
    		{
    			int vW; int vH;
    			var vertTex = graphic.MatSide.mainTexture as Texture2D;
    			var vertBounds = Def_Extensions.CropVertical(vertTex.GetColorSafe(out vW, out vH), vW, vH);
    			
					//Plants only care for verts
					//This is assuming PLANTS TAKE UP A FULL TILE!!
					// TODO : Refactor
    			if (type == GraphicType.Plant || type == GraphicType.LeaflessPlant || type == GraphicType.ImmaturePlant)
    			{
    				dict.Add(type.ToString(),
    			         new Pair<float, float>(1f, (float)(vertBounds.max - vertBounds.min) / (float)vH));
    				return;
    			}
    			
    			int hW; int hH;
    			var horzTex = graphic.MatFront.mainTexture as Texture2D;
    			var horzBounds = Def_Extensions.CropHorizontal(horzTex.GetColorSafe(out hW, out hH), hW, hH);
    			
    				//drawSize is added (and not Lerped!) because it is constant with lifeStageIndex (A17)
    			dict.Add(type.ToString() + "." + lifeStageIndex,
    			         new Pair<float, float>(
    				         	graphic.drawSize.x * (float)(horzBounds.max - horzBounds.min) / (float)hW,
    				         	graphic.drawSize.y * (float)(vertBounds.max - vertBounds.min) / (float)vH));
    		}
    	}
    	
    	private static Dictionary<string, TextureBoundFactor> boundMap = new Dictionary<string, TextureBoundFactor>();
    	
    	public static void Inject()
    	{
    		foreach (PawnKindDef kindDef in DefDatabase<PawnKindDef>.AllDefs.Where(x => !x.race.race.Humanlike))
    		{
    			boundMap[kindDef.defName] = new TextureBoundFactor(kindDef);
    			//Log.Message(kindDef.ToString() + ", " + kindDef.race.race.baseBodySize +", " + String.Join(", ", boundMap[kindDef.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Key+"="+x.Value.Second).ToArray()));
    		}
    		
    		//Fire is dummy name for a sowing plant's texture
    		boundMap[ThingDefOf.Fire.defName] = new TextureBoundFactor((Graphic)(sowingPlantMatInfo.GetValue(null)));
    		//Log.Message("Sowing Plant, " + String.Join(", ", boundMap[ThingDefOf.Fire.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Key+"="+x.Value.Second).ToArray()));
    		
    		
    		foreach (ThingDef plantDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null))
    		{
    			boundMap[plantDef.defName] = new TextureBoundFactor(plantDef);
    			//Log.Message(plantDef.ToString() + ", " + plantDef.fillPercent + ", " +plantDef.plant.visualSizeRange.ToString() +", " + String.Join(", ", boundMap[plantDef.defName].dict.Select(x => x.Key+"="+x.Value.First+","+x.Key+"="+x.Value.Second).ToArray()));
    		}
    		
    		Log.Message("Combat Extended :: Bounds injected");
    	}
    	
    	public static Pair<float, float> ForPawn(Pawn pawn)
    	{
    		return boundMap[pawn.kindDef.defName].PawnFor(pawn);
    	}
    	
    	public static Pair<float, float> ForPlant(Plant plant)
    	{
    		return boundMap[plant.def.defName].PlantFor(plant);
    	}
    	
    	public static void LogDatabase()
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
    	}
	}
}
