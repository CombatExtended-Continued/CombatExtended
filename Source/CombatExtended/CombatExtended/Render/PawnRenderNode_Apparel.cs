using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class PawnRenderNode_Apparel : Verse.PawnRenderNode_Apparel
    {
        public PawnRenderNode_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }
        public PawnRenderNode_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree)
        {
        }
        public PawnRenderNode_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, bool useHeadMesh) : base(pawn, props, tree)
        {
        }
        public override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            yield return GraphicDatabase.Get((apparel.def.graphicData?.graphicClass ?? typeof(Graphic_Multi)), TexPathFor(pawn), ShaderFor(pawn), apparel.def.graphicData.drawSize, apparel.DrawColor, apparel.DrawColorTwo);
        }
        public override string ToString()
        {
            return apparel?.ToString() ?? base.ToString();
        }
    }
}
