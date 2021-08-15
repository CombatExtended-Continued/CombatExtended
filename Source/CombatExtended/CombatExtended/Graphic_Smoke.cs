using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Graphic_Smoke : Graphic_Gas
    {
        private const float DistinctAlphaLevels = 128f;

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Rand.PushState();
            Rand.Seed = thing.thingIDNumber.GetHashCode();
            Smoke smoke = thing as Smoke;

            // round alpha to avoid creating too many new materials
            var alpha = Mathf.Round(smoke.GetOpacity() * DistinctAlphaLevels) / DistinctAlphaLevels;
            var materialColor = new Color(color.r, color.g, color.r, color.a * alpha);
            var material = MaterialPool.MatFrom(new MaterialRequest((Texture2D)MatSingle.mainTexture, MatSingle.shader, materialColor));

            float angle = Rand.Range(0, 360) + smoke.graphicRotation;

            Vector3 pos = thing.TrueCenter() + new Vector3(Rand.Range(-0.45f, 0.45f), 0f, Rand.Range(-0.45f, 0.45f));
            Vector3 s = new Vector3(Rand.Range(0.8f, 1.2f) * this.drawSize.x, 0f, Rand.Range(0.8f, 1.2f) * this.drawSize.y);
            Matrix4x4 matrix = default(Matrix4x4);

            matrix.SetTRS(pos, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            Rand.PopState();
        }
    }
}