#version 450

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec2 vRotationCosSin;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    float _RotationSpeed; // @default = 1.0
    float _AgeSecsPausable; // @default = 0.0
} u;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

// A radius of 0.5 reaches the midpoint of each side of the quad.
// Multiplying squared distance by 4 normalizes that radius to 1.
const float INVERSE_RADIUS_SQUARED = 4.0;

// Larger values keep more of the center opaque and narrow the fade.
const float EDGE_FADE_SHARPNESS = 3.0;

float circularEdgeOpacity(vec2 offsetFromCenter)
{
    float distanceSquared = dot(offsetFromCenter, offsetFromCenter);
    float normalizedDistanceSquared = distanceSquared * INVERSE_RADIUS_SQUARED;

    return clamp((1.0 - normalizedDistanceSquared) * EDGE_FADE_SHARPNESS, 0.0, 1.0);
}

vec2 rotateSamplingOffset(vec2 offset, vec2 rotationCosSin)
{
    float cosine = rotationCosSin.x;
    float sine = rotationCosSin.y;

    return vec2(
        cosine * offset.x + sine * offset.y,
        cosine * offset.y - sine * offset.x
    );
}

void main()
{
    // Center the coordinates so rotation happens around the puff's center.
    vec2 offsetFromCenter = vUv - vec2(0.5);

    float edgeOpacity = circularEdgeOpacity(offsetFromCenter);

    // Rotate the texture coordinates, then return to texture space.
    vec2 rotatedUv = rotateSamplingOffset(offsetFromCenter, vRotationCosSin) + vec2(0.5);

    vec4 textureColor = texture(_MainTex, rotatedUv);
    vec4 tintedColor = textureColor * u._Color * vColor;

    // Fade alpha only, leaving RGB unchanged for straight-alpha blending.
    outColor = tintedColor * vec4(1.0, 1.0, 1.0, edgeOpacity);
}