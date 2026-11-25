using UnityEngine;
using System;
using Verse;

namespace CombatExtended;

public static class CE_Math
{
    static readonly float sqrt2 = Mathf.Sqrt(2f);
    static readonly float deg2rad_sqrt2 = Mathf.Deg2Rad / Mathf.Sqrt(2f);

    /*
      The Statistical Math region is based on the public domain work of John D. Cook.
      The underlying algorithm comes from Abramowitz and Stegun
      See https://www.johndcook.com/stand_alone_code.html for details.
    */

    #region Statistical Math
    private static double RationalApproximation(double t)
    {
        // Abramowitz and Stegun formula 26.2.23.
        // The absolute value of the error should be less than 4.5 e-4.
        double[] c = { 2.515517, 0.802853, 0.010328 };
        double[] d = { 1.432788, 0.189269, 0.001308 };
        return t - ((c[2] * t + c[1]) * t + c[0]) /
            (((d[2] * t + d[1]) * t + d[0]) * t + 1.0);
    }

    private static double NormalCdfInverse(double p)
    {
        if (p >= 1.0)
        {
            return 0.0;
        }

        if (p <= 0.0)
        {
            return float.MaxValue;
        }

        // See article above for explanation of this section.
        if (p <= 0.5)
        {
            // F^-1(p) = - G^-1(p)
            return -RationalApproximation(Math.Sqrt(-2.0 * Math.Log(p)));
        }
        else
        {
            // F^-1(p) = G^-1(1-p)
            return RationalApproximation(Math.Sqrt(-2.0 * Math.Log(1.0 - p)));
        }
    }

    private static float erf(float x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        int sign = 1;

        if (x < 0)
        {
            sign = -1;
            x = -x;
        }

        // A&S formula 7.1.26
        double t = 1.0 / (1.0 + p * x);
        float y = (float)(1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x));

        return sign * y;
    }

    private static float normal_cdf(float x)
    {
        return (1 + erf(x / sqrt2)) / 2.0f;
    }

    #endregion
    public static float CalculateHitPercent(float dist, Bounds bounds, float offset, float shotSpeed, float shotAngle, float swayDegrees, float spreadDegrees, float visibilityShift, float gravity)
    {
        float w = Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z);
        return CalculateHitPercent(dist, w, bounds.size.y, offset, shotSpeed, shotAngle, swayDegrees, spreadDegrees, visibilityShift, gravity);
    }

    public static float CalculateHitPercent(float dist, float w, float h, float offset, float shotSpeed, float shotAngle, float swayDegrees, float spreadDegrees, float visibilityShift, float gravity)
    {
        // calculate 1 standard deviation and variance
        float sigma_sway_xz = swayDegrees * deg2rad_sqrt2;
        float sigma_sway_theta = sigma_sway_xz / 4.0f;

        float sigma_spread = spreadDegrees * deg2rad_sqrt2 / 2.0f;
        float sigma_spread_sq = sigma_spread * sigma_spread;

        float sigma_target_xz = visibilityShift / dist / sqrt2;

        //Add uncertanties in quadrature.
        float sigma_xz = Mathf.Sqrt(sigma_sway_xz * sigma_sway_xz + sigma_spread_sq + sigma_target_xz * sigma_target_xz);
        float sigma_theta = Mathf.Sqrt(sigma_sway_theta * sigma_sway_theta + sigma_spread_sq);

        float sigma_dist = visibilityShift / sqrt2;
        float d_prime = Mathf.Sqrt(dist * dist + sigma_dist * sigma_dist);

        float sigma_horizontal = sigma_xz * d_prime;
        float sigma_y = sigma_theta * d_prime;

        float cos_theta = Mathf.Cos(shotAngle);

        float sigma_gravity = (gravity * dist * sigma_dist) / (2 * shotSpeed * shotSpeed * cos_theta * cos_theta);
        float sigma_vertical = Mathf.Sqrt(sigma_y * sigma_y + sigma_gravity * sigma_gravity);

        float half_w = w / 2;
        float p_horizontal = 1.0f;

        if (sigma_horizontal > 0)
        {
            p_horizontal = 2 * normal_cdf(half_w / sigma_horizontal) - 1;
        }

        float p_vertical = 1.0f;
        if (sigma_vertical > 0)
        {
            // Becaue we might not be shooting at center of visible area, we have to calculate odds of missing high and the odds of missing low.
            // The chance of hitting is the probability of the bullet passing between the upper and lower limit.
            float higher = (h - offset) / sigma_vertical;
            float lower = (-offset) / sigma_vertical;

            p_vertical = normal_cdf(higher) - normal_cdf(lower);
        }
        return p_horizontal * p_vertical;
    }

    public static float CalculateMaxDistance(float threshold, Bounds bounds, float shotSpeed, float swayAmplitude, float spreadDegrees, float sightsEfficiency, float aimingAccuracy, float gravity)
    {
        float h = bounds.size.y;
        float w = Mathf.Sqrt(bounds.size.x * bounds.size.x + bounds.size.z * bounds.size.z);
        return CalculateMaxDistance(threshold, w, h, shotSpeed, swayAmplitude, spreadDegrees, sightsEfficiency, aimingAccuracy, gravity);
    }

    public static float CalculateMaxDistance(float threshold, float w, float h, float shotSpeed, float swayAmplitude, float spreadDegrees, float sightsEfficiency, float aimingAccuracy, float gravity)
    {
        if (threshold <= 0)
        {
            return float.MaxValue;
        }

        if (threshold >= 1)
        {
            return 0;
        }

        if (sightsEfficiency < 0.02f)
        {
            sightsEfficiency = 0.02f;
        }

        // constants are reverse-extracted from Verb_LaunchProjectileCE's target position code, and then combined (3.5f / 50)
        float SE = 0.07f * (2 - aimingAccuracy) / sightsEfficiency;
        float spdsq = (2 * shotSpeed * shotSpeed);

        float zScore_target = Mathf.Abs((float)NormalCdfInverse(threshold));

        float sigmaSwayPhi = swayAmplitude * deg2rad_sqrt2;
        float sigmaSwayTheta = sigmaSwayPhi / 4.0f;
        float sigmaSpread = spreadDegrees * deg2rad_sqrt2;

        float sigmaTheta = Mathf.Sqrt(sigmaSwayTheta * sigmaSwayTheta + sigmaSpread * sigmaSpread);

        float d_max = 200;
        float d_mid = Mathf.Min(d_max, (h / 2.0f) * zScore_target / sigmaTheta);
        float d_min = 1;
        float p_near = 1;
        float p_far = 0;
        int i;

        for (i = 0; i < 10; i++)
        {
            float theta = Mathf.Atan(((h / 2) / d_mid) + (gravity * d_mid) / spdsq);
            float p = CalculateHitPercent(d_mid, w, h, 0, shotSpeed, theta, swayAmplitude, spreadDegrees, SE * CE_Utility.LightingRangeMultiplier(d_mid), gravity);
            if (p >= threshold)
            {
                d_min = d_mid;
                p_near = p;
            }
            else
            {
                d_max = d_mid;
                p_far = p;
            }

            if (d_max - d_min < Mathf.Max(3, d_mid / 15f))
            {
                break;
            }

            float f = 0.5f - 0.25f * (2f * threshold - 1f) * Mathf.Abs(2f * threshold - 1f);
            d_mid = d_min + f * (d_max - d_min);
        }
        return d_min;
    }
}
