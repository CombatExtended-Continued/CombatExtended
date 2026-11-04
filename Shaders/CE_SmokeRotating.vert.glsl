#version 450

layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 2) in vec4 inColor; // @vertex Color optional @default = vec4(1.0, 1.0, 1.0, 1.0)
layout(location = 3) in vec4 inUv0; // @vertex TexCoord0 optional @default = vec4(0.0, 0.0, 0.0, 0.0)

layout(push_constant) uniform Push
{
    mat4 modelMatrix;
} push;

layout(set = 0, binding = 0) uniform VertexUniforms
{
    mat4 ViewProj;
} uVertex;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    float _RotationSpeed; // @default = 1.0
    float _AgeSecsPausable; // @default = 0.0
} u;

layout(location = 0) out vec4 vColor;
layout(location = 1) out vec2 vUv;

// x = cosine of the rotation angle; y = sine.
layout(location = 2) out vec2 vRotationCosSin;

// Produces a repeatable value in [0, 1) from the cell's seed.
float randomFromSeed(float seed)
{
    return abs(fract(sin(seed) * 43758.5453));
}

void main()
{
    // Transform this corner from mesh space to world space, then to clip space.
    vec4 worldPosition = push.modelMatrix * vec4(inPosition, 1.0);
    gl_Position = uVertex.ViewProj * worldPosition;

    // All four corners of a puff must have the same seed.
    float cellSeed = inUv0.z;
    float startingAngle = cellSeed;

    // Negative values reverse direction; values near zero rotate slowly.
    float individualSpin = randomFromSeed(cellSeed) * 2.0 - 1.0;
    float angularSpeed = u._RotationSpeed * individualSpin;

    float rotationAngle = u._AgeSecsPausable * angularSpeed + startingAngle;

    // Compute rotation here so the fragment shader can reuse it.
    vRotationCosSin = vec2(cos(rotationAngle), sin(rotationAngle));

    // Vertex RGB carries tint; alpha carries density-derived opacity.
    vColor = inColor;
    vUv = inUv0.xy;
}