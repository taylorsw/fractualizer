cbuffer Parameters
{
	float3 ptCamera;
	float3 vkCamera;
	float3 vkCameraOrtho;
	float2 rsScreen;
	float2 rsViewPlane;
	float duNear;
	float param;
	float param2;
}

static const int cmarch = 48;

void sphereFold(inout float3 z, inout float dz) {
	float minRadius2 = 0.25;
	float fixedRadius2 = 1;

	float r2 = dot(z, z);
	if (r2 < minRadius2) {
		// linear inner scaling
		float temp = (fixedRadius2 / minRadius2);
		z *= temp;
		dz *= temp;
	}
	else if (r2 < fixedRadius2) {
		// this is the actual sphere inversion
		float temp = fixedRadius2 / r2;
		z *= temp;
		dz *= temp;
	}
}

void boxFold(inout float3 z, inout float dz) {
	float foldingLimit = 1;
	z = clamp(z, -foldingLimit, foldingLimit) * 2.0 - z;
}

float DE(float3 z)
{
	float sf = 2;
	float sfNormalizing = 2 * (sf + 1) / (sf - 1);
	z = z * sfNormalizing;
	int Iterations = 10;
	float3 offset = z;
	float dr = 1.0;
	for (int n = 0; n < Iterations; n++) {
		boxFold(z, dr);       // Reflect
		sphereFold(z, dr);    // Sphere Inversion

		z = sf * z + offset;  // Scale & Translate
		dr = dr*abs(sf) + 1.0;
	}
	float r = length(z);
	return r / abs(dr) / sfNormalizing;
}


float4 ray_marching(float3 pt, float3 vk)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = DE(pt);
		pt += du * vk;
		duTotal += du;
		float duEpsilon = 0.25 * duTotal / duNear * duPixelRadius;
		if (du < duEpsilon)
			return float4(pt, i);
	}
	return float4(pt, -1);
}


// Basic orbit-trapping color
float3 ColorOT(float4 marched)
{
	return normalize(
		float3(
			length(float3(-3, 0, 0) - marched.xyz) / 4.0,
			length(float3(3, 0, 0) - marched.xyz) / 4.0,
			0.5 + length(-marched.xyz) / 2.0));
}

float3 ColorAO(float3 color, float steps)
{
	// ambient occlusion
	// return color * (1 - (0.5 * (1 - (steps / cmarch)))); // cool effect
	// float base = cmarch;
	// float3 colorAO = color * (1 - 0.3 * (1 - (log(steps) / log(base)) / base));
	float3 colorAO = color * (1 - (steps / cmarch));
	return colorAO;
}

float4 main(float4 position : SV_POSITION) : SV_TARGET
{
	// position.x is from 0.5 to rsScreen.x + 6.5
	float4 red = float4(1, 0, 0, 1);
	float4 green = float4(0, 1, 0, 1);
	float4 blue = float4(0, 0, 1, 1);
	float4 black = float4(0, 0, 0, 1);

	float2 ptScreen = position.xy - float2(0.5, 0.5);

	float3 ptPlaneCenter = ptCamera + vkCamera * duNear;

	float3 vkDown = vkCameraOrtho;
	float3 vkRight = cross(vkDown, vkCamera);

	float2 vkFromScreenCenter = ptScreen - rsScreen / 2;

	// -0.5 <= frx <= 0.5
	float frx = vkFromScreenCenter.x / rsScreen.x;
	float fry = vkFromScreenCenter.y / rsScreen.y;

	float2 vkfrFromPlaneCenter = float2(vkFromScreenCenter.x * rsViewPlane.x / rsScreen.x, vkFromScreenCenter.y * rsViewPlane.y / rsScreen.y);
	float3 ptPlane = ptPlaneCenter + vkRight * rsViewPlane.x * frx + vkDown * rsViewPlane.y * fry;

	float3 vkRay = normalize(ptPlane - ptCamera);

	float4 marched = ray_marching(ptCamera, vkRay);

	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	float3 colorOT = ColorOT(marched);

	float3 colorAO = ColorAO(colorOT, marched.w);

	return float4(colorAO.x, colorAO.y, colorAO.z, 1.0);
}