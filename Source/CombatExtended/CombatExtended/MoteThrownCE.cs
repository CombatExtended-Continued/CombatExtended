using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    /// <summary>
    /// Used to avoid vanilla limitation regarding rendering with DrawPos.y != 1.
    /// </summary>
    public class MoteThrownCE : MoteThrown
    {
        public Thing attachedAltitudeThing = null;

        private Graphic_Mote _moteGraphic = null;
        private Graphic_Mote MoteGraphic
        {
            get
            {
                if (_moteGraphic == null) _moteGraphic = (Graphic_Mote)Graphic;
                return _moteGraphic;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = base.DrawPos;
                if (attachedAltitudeThing?.Spawned ?? false)
                {
                    pos.y = attachedAltitudeThing.DrawPos.y;
                }
                return pos;
            }
        }

        public float CurAltitude
        {
            get
            {
                if (attachedAltitudeThing?.Spawned ?? false)
                {
                    return attachedAltitudeThing.DrawPos.y;
                }
                return exactPosition.y;
            }
        }

        public override void Draw()
        {
            /*
             * Required to allow rendering without setting y to 0 
             */
            float alpha = this.Alpha;
            if (!(alpha <= 0f))
            {
                Color color = MoteGraphic.Color * this.instanceColor;
                color.a *= alpha;
                Vector3 exactScale = this.exactScale;
                exactScale.x *= MoteGraphic.data.drawSize.x;
                exactScale.z *= MoteGraphic.data.drawSize.y;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(DrawPos, Quaternion.AngleAxis(this.exactRotation, Vector3.up), exactScale);
                Material matSingle = Graphic.MatSingle;
                if (!MoteGraphic.ForcePropertyBlock && color.IndistinguishableFrom(matSingle.color))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0, null, 0);
                    return;
                }
                Graphic_Mote.propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
                Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0, null, 0, Graphic_Mote.propertyBlock);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref attachedAltitudeThing, "attachedAltitudeThing");
        }
    }
}
