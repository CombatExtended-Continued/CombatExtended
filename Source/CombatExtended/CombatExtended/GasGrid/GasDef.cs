using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;

public class GasDef : Def
{
    public float maxDensity = 12800f;             // Maximum density per cell.
    public float dissipationThreshold = 30f;      // At or below this, gas expires randomly.
    public float minSpreadDensity = 10f;          // Minimum spreading density; keep below dissipationThreshold.
    public float dissipationRate = 0.00001f;      // Density fraction lost per tick above the threshold.
    public float minDissipationPerTick = 0.001f;  // Minimum loss per tick above the threshold.
    public float outdoorLossPerInterval = 60f;    // Density lost per interval without a roof.
    public float expireChancePerInterval = 0.05f; // Chance gases below dissipationThreshold will disappear completely
    public float fireDensityPerInterval = 1800f;  // Density added per by fires, should we look at moving this perhaps? Does this need to be XML touchable?
    public float ppmPerDensity = 0.1f;            // Displayed PPM per density unit.
    public float lethalDensity = 10000f;          // Density targeting 100% inhalation severity.
    public float inhalationPerSecond = 0.045f;    // Severity gained per second at full density, before modifiers.
    public bool confusesInsectoids = false;       // Thick gas sends insectoids into a confused wander.
    public float bugConfusePercent = 0.15f;       // Fraction of maxDensity that confuses insectoids.
    public bool insectoidsOnlyConfused = false;   // Insectoids are confused by the gas but never build up the hediff.

    public bool leaveFilth = false; // Dissipating thick gas may leave filth.
    public ThingDef filthDef;       // Filth spawned when leaveFilth is enabled.

    public HediffDef hediff;          // Gas's hediff to apply to pawns
    public StatDef sensitivityStat;   // Pawn sensitivity multiplier; unset uses 1.
    public bool notifyGasMask = true; // Prompt AI pawns to equip gas masks.

    public ShaderDef shader;
    public string texPath = "Things/Gas/Smoke";
    public Color color = Color.black;
    public float drawSize = 3f;
    public float rotationSpeed = 50f; // Rotation in degrees per second.
    public float minDrawDensity = 0f; // Draw only above this density.

    private const int BaseRenderQueue = 3151;
    private const float MinOpacity = 0.05f;
    private const float OpacityRange = 1f - MinOpacity;

    // Cached material with a per-gas render queue and spin speed.
    public Material Material
    {
        get
        {
            if (field == null)
            {
                var request = new MaterialRequest(ContentFinder<Texture2D>.Get(texPath), shader.Shader, Color.white)
                {
                    renderQueue = BaseRenderQueue + index
                };
                field = MaterialPool.MatFrom(request);
                field.SetFloat("_RotationSpeed", rotationSpeed * Mathf.Deg2Rad);
            }
            return field;
        }
    }

    public bool IsDrawn(float cellDensity) => cellDensity > minDrawDensity;

    public Color32 ColorFor(float cellDensity)
    {
        Color tint = color;
        tint.a *= MinOpacity + (OpacityRange * (cellDensity / maxDensity));
        return tint;
    }

    public ushort ToSaved(float cellDensity) => (ushort)Mathf.RoundToInt(cellDensity / maxDensity * ushort.MaxValue);

    public float FromSaved(ushort saved) => saved / (float)ushort.MaxValue * maxDensity;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        string[] rangeErrors =
        [
            MustBePositive(nameof(maxDensity), maxDensity),
            MustBePositive(nameof(lethalDensity), lethalDensity),
            MustBePositive(nameof(drawSize), drawSize),

            MustNotBeNegative(nameof(dissipationThreshold), dissipationThreshold),
            MustNotBeNegative(nameof(minSpreadDensity), minSpreadDensity),
            MustNotBeNegative(nameof(dissipationRate), dissipationRate),
            MustNotBeNegative(nameof(minDissipationPerTick), minDissipationPerTick),
            MustNotBeNegative(nameof(outdoorLossPerInterval), outdoorLossPerInterval),
            MustNotBeNegative(nameof(fireDensityPerInterval), fireDensityPerInterval),
            MustNotBeNegative(nameof(ppmPerDensity), ppmPerDensity),
            MustNotBeNegative(nameof(inhalationPerSecond), inhalationPerSecond),
            MustNotBeNegative(nameof(minDrawDensity), minDrawDensity),

            MustBeBetweenZeroAndOne(nameof(expireChancePerInterval), expireChancePerInterval),
            MustBeBetweenZeroAndOne(nameof(bugConfusePercent), bugConfusePercent),

            float.IsFinite(rotationSpeed) ? null : $"{nameof(rotationSpeed)} must be finite"
        ];
        foreach (string error in rangeErrors)
        {
            if (error != null)
            {
                yield return error;
            }
        }

        if (minSpreadDensity >= dissipationThreshold)
        {
            yield return "minSpreadDensity should be lower than dissipationThreshold or thin clouds can get stuck indoors";
        }
        if (leaveFilth && filthDef == null)
        {
            yield return "leaveFilth needs a filthDef";
        }
        if (shader == null)
        {
            yield return "shader is required";
        }
        if (string.IsNullOrEmpty(texPath))
        {
            yield return "texPath is required";
        }
    }

    private static string MustBePositive(string name, float value) => float.IsFinite(value) && value > 0f ? null : $"{name} must be finite and greater than 0";

    private static string MustNotBeNegative(string name, float value) => float.IsFinite(value) && value >= 0f ? null : $"{name} must be finite and at least 0";

    private static string MustBeBetweenZeroAndOne(string name, float value) => float.IsFinite(value) && value is >= 0f and <= 1f ? null : $"{name} must be between 0 and 1 inclusive";
}
