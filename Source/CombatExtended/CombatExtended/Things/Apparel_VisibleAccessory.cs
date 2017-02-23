using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public abstract class Apparel_VisibleAccessory : Apparel
    {
    	const float _HeadOffset = 0.032f;
    	const float _BodyOffset = 0.0269999985f;
    	static Dictionary<string, bool> _OnHeadCache = new Dictionary<string, bool>();
    	
    	public override void DrawWornExtras()
        {
            if (wearer == null || !wearer.Spawned) return;
            Building_Bed bed = wearer.CurrentBed();
            if (bed != null && !bed.def.building.bed_showSleeperBody && !onHead) return;
            
            //Since I haven't a head apparel item to test the drawing code against for now we throw an error (ONCE) and exit.
            if (this.onHead)
            {
            	Log.ErrorOnce(string.Concat("CombatExtended :: Apparel_VisibleAccessory: The head drawing code is incomplete and the apparel '",
            	                            this.Label, "' will not be drawn."), this.def.debugRandomId);
            	return;
            }
            
            // compute angle and Rot4 vars
            Rot4 rotation;
            Rot4 bedRotation = new Rot4();
            float angle = 0;
            Vector3 drawVec = wearer.Drawer.DrawPos;
            if (wearer.GetPosture() != PawnPosture.Standing) 
            {
                rotation = LayingFacing();
                if (bed != null)
                {
                    bedRotation = bed.Rotation;
                    bedRotation.AsInt += 2;
                    angle = bedRotation.AsAngle;
		            AltitudeLayer altitude = (AltitudeLayer)((byte)Mathf.Max((int)bed.def.altitudeLayer, 14));
		            drawVec.y = wearer.Position.ToVector3ShiftedWithAltitude(altitude).y;
		            drawVec += bedRotation.FacingCell.ToVector3() * (-wearer.Drawer.renderer.BaseHeadOffsetAt(Rot4.South).z);
                } else {
            		drawVec.y = wearer.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.LayingPawn).y;
	                if (wearer.Downed)  // wearer.Spawned == false when Pawn.Dead == true.
	                {
	                    float? newAngle = (((((wearer.Drawer == null) ? null : wearer.Drawer.renderer) == null) ? null : wearer.Drawer.renderer.wiggler) == null) ? (float?)null : wearer.Drawer.renderer.wiggler.downedAngle;
	                    if (newAngle != null)
	                        angle = newAngle.Value;
	                }
	                else
	                {
	                    angle = rotation.FacingCell.AngleFlat;
	                }
                }
                drawVec.y += 0.005f;
            } else {
            	rotation = wearer.Rotation;
            	drawVec.y = wearer.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Pawn).y;
            }
            
            drawVec.y += GetAltitudeOffset(rotation);
            
            //if (!onHead)
            //{
            //	if (rotation == Rot4.North)
            //		drawVec.y += _HeadOffset;
            //	else
            //		drawVec.y += _BodyOffset;
            //} else if (onHead) {
            //	if (rotation == Rot4.North)
            //		drawVec.y += _BodyOffset;
            //	else
            //		drawVec.y += _HeadOffset;
            //} else {
            //	Log.ErrorOnce(string.Concat("CombatExtended :: Apparel_VisibleAccessory: Undefined state drawing apparel '", Label, "'."), def.debugRandomId);
            //}
            
            // Get the graphic path
            string path = def.graphicData.texPath + "_" + ((wearer == null) ? null : wearer.story.bodyType.ToString());
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, def.graphicData.drawSize, DrawColor);
            ApparelGraphicRecord apparelGraphic = new ApparelGraphicRecord(graphic, this);

            Material mat = apparelGraphic.graphic.MatAt(rotation);
            Vector3 s = new Vector3(1.5f, 1.5f, 1.5f);

            mat.shader = ShaderDatabase.CutoutComplex;
            mat.color = DrawColor;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawVec, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(rotation == Rot4.West ? MeshPool.plane10Flip : MeshPool.plane10, matrix, mat, 0);
        }

        protected abstract float GetAltitudeOffset(Rot4 rotation);

        // Copied from PawnRenderer
        private Rot4 LayingFacing()
        {
            if (wearer == null)
            {
                return Rot4.Random;
            }
            if (wearer.GetPosture() == PawnPosture.LayingFaceUp)
            {
                return Rot4.South;
            }
            if (wearer.RaceProps.Humanlike)
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.South;
                    case 2:
                        return Rot4.East;
                    case 3:
                        return Rot4.West;
                }
            }
            else
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.East;
                    case 2:
                        return Rot4.West;
                    case 3:
                        return Rot4.West;
                }
            }
            return Rot4.Random;
        }
		
		//Utility, return if the apparel is worn on the head/body.        
       	protected bool onHead
    	{
       		get {
       			if (!_OnHeadCache.ContainsKey(def.defName))
       			{
		   			List<BodyPartRecord> parts = wearer.RaceProps.body.AllParts.Where(def.apparel.CoversBodyPart).ToList();
		   			bool gotHit = false;
		   			foreach (BodyPartRecord part in parts)
		   			{
		   				BodyPartRecord p = part;
		   				while (p != null)
		   				{
		   					if (p.groups.Contains(BodyPartGroupDefOf.Torso))
			   				{
		   						_OnHeadCache.Add(def.defName, false);
		   						gotHit = true;
		   						break;
			   				}
		   					if (p.groups.Contains(BodyPartGroupDefOf.FullHead))
		   				    {
			   					_OnHeadCache.Add(def.defName, true);
		   						gotHit = true;
			   					break;
		   				    }
			   				p = p.parent;
		   				}
		   				if (gotHit)
		   					break;
		   			}
		   			if (!_OnHeadCache.ContainsKey(def.defName))
		   			{
		   				Log.ErrorOnce(string.Concat("CombatExtended :: ", this.GetType(), " was unable to determine if body or head on item '", Label,
		   				                            "', might the wearer be non-human?  Assuming apparel is on body."), def.debugRandomId);
		   				_OnHeadCache.Add(def.defName, false);
		   			}
       			}
       			bool ret;
       			_OnHeadCache.TryGetValue(def.defName, out ret);  // is there a better way? Dictionary.Item isn't there.  Didn't bother with try/catch as by now it should have the key.
       			return ret;
       		}
    	}
    }
}
