//#define SPHERE
//#define ACCUNORMAL
#define SHADOWS
#define LIGHTING

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

	float fogA;
}

static const int cmarch = 148;

#ifdef SPHERE
static const float duRadiusSphere = 0.3;
float DE_sphere(float3 pos)
{
	return
		min(
		length(pos) - duRadiusSphere,
		length(pos - float3(0.7, 0.0, 2.0)) - 0.5
		);
}
#endif

float DE(float3 pos)
{
#ifdef SPHERE
	return DE_sphere(pos);
#endif

	float Power = param;
	int iterations = 10;
	float Bailout = 5;

	float3 z = pos;
	float dr = 1.0;
	float r = 0.0;
	for (int i = 0; i < iterations; i++)
	{
		r = length(z);
		if (r > Bailout)
			break;

		// convert to polar coordinates
		float theta = acos(z.z / r) / param2;
		float phi = atan(z.y / z.x) * param2;
		dr = pow(r, Power - 1.0) * Power * dr + 1.0;

		// scale and rotate the point
		float zr = pow(r, Power);
		theta = theta * Power;
		phi = phi * Power;

		// convert back to cartesian coordinates
		z = zr * float3(
			sin(theta) * cos(phi),
			sin(phi) * sin(theta),
			cos(theta));
		z += pos;
	}
	return 0.5 * log(r) * r / dr;
}

float3 VkNormalSphere(float3 pt)
{
	return normalize(pt);
}

float3 VkNormal(float3 pt, float duEpsilon)
{
#ifdef SPHERE
	return VkNormalSphere(pt);
#endif
#ifdef ACCUNORMAL
	float3 vkX = float3(duEpsilon, 0, 0);
	float3 vkY = float3(0, duEpsilon, 0);
	float3 vkZ = float3(0, 0, duEpsilon);
	return normalize(float3(
		DE(pt + vkX) - DE(pt - vkX),
		DE(pt + vkY) - DE(pt - vkY),
		DE(pt + vkZ) - DE(pt - vkZ)));
#else
	float duDEPt = DE(pt);
	float dx = DE(pt + float3(duEpsilon, 0, 0)) - duDEPt;
	float dy = DE(pt + float3(0, duEpsilon, 0)) - duDEPt;
	float dz = DE(pt + float3(0, 0, duEpsilon)) - duDEPt;
	return normalize(float3(dx, dy, dz));
#endif
}

static float duPixelRadius = rsViewPlane.x / rsScreen.x;
float DuEpsilon(float sfEpsilon, float duMarched)
{
	return sfEpsilon * 0.5 * duMarched / duNear * duPixelRadius;
}

float4 PtMarch(float3 pt, float3 vk, float sfEpsilon, out float duEpsilon)
{
	duEpsilon = -1;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = 0.99 * DE(pt);
		pt += du * vk;
		duTotal += du;
		duEpsilon = DuEpsilon(sfEpsilon, duTotal);
		if (du < duEpsilon)
			return float4(pt, i);
	}
	return float4(pt, -1);
}

// Basic orbit-trapping color
float3 ColorOT(float4 marched)
{
#ifdef SPHERE
	return float3(1.0, 0.0, 0.0);
#endif
	return normalize(
		float3(
			length(float3(-3, 0, 0) - marched.xyz) / 4.0, 
			length(float3(3, 0, 0) - marched.xyz) / 4.0, 
			0.5 + length(-marched.xyz) / 2.0));
}

// Ambient Occlusion
float3 ColorAO(float3 color, float steps)
{
	// ambient occlusion
	// return color * (1 - (0.5 * (1 - (steps / cmarch)))); // cool effect
	// float base = cmarch;
	// float3 colorAO = color * (1 - 0.3 * (1 - (log(steps) / log(base)) / base));
	float3 colorAO = color * (1 - (steps / cmarch));
	return colorAO;
}

float3 ColorFog(float3 color, float3 fogColor, float duMarched)
{
	float frFog = 1.0 - fogA * exp(-duMarched / 5);
	return lerp(color, fogColor, frFog);
}

static const float3 ptLight = float3(2, 0, 3);
static const float3 colorDiffuse = float3(0.5, 0.0, 0.0);
static const float3 colorSpecular = float3(1.0, 1.0, 1.0);
static const float shininess = 16.0;
static const float sfEpsilonShadow = 2.0;
float3 ColorBP(float3 color, float3 ptSurface, float duEpsilon)
{
	float3 vkNormal = VkNormal(ptSurface, duEpsilon);
	float3 vkLightDir = normalize(ptLight - ptSurface);

	float lambertian = max(dot(vkLightDir, vkNormal), 0.0);
	float specular = 0.0;

	if (lambertian > 0.0)
	{
		float3 vkHalf = normalize(vkLightDir + vkCamera);
		float agrSpecular = max(dot(vkHalf, vkNormal), 0.0);
		specular = pow(agrSpecular, shininess);
	}

	color =
		0.4 * color
		+ 0.6 * lambertian * colorDiffuse
		;// +specular * colorSpecular;

#ifdef SHADOWS
	float dummy;
	float3 vkCameraDir = normalize(ptCamera - ptSurface);
	float3 ptShadowStart = ptSurface + vkCameraDir * 1.1 * DuEpsilon(sfEpsilonShadow, length(ptCamera - ptSurface));
	float4 shadow = PtMarch(ptShadowStart, vkLightDir, sfEpsilonShadow, dummy);

	// TODO: Should probably check if marched at least dist to light
	if (shadow.w != -1)
		color *= 0.4;
#endif

	return color;
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

	float duEpsilon;
	float4 ptMarched = PtMarch(ptCamera, vkRay, 1.0, duEpsilon);

	float duMarched = length(ptMarched.xyz - ptCamera);

	if (ptMarched.w == -1)
		return float4(0, 0, 0, 1);

	float3 color = ColorOT(ptMarched);

#ifndef SPHERE
	color = ColorAO(color, ptMarched.w);

	color = ColorFog(color, float3(0.5, 0.6, 0.7), duMarched);
#endif

#ifdef LIGHTING
	color = ColorBP(color, ptMarched, duEpsilon);
#endif

	return float4(color.x, color.y, color.z, 1.0);
}