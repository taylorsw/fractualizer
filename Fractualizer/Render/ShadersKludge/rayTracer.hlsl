//#define SPHERE
//#define ACCUNORMAL
#define SHADOWS
#define LIGHTING

static const int cmarch = 148;
static const float3 ptOrigin = float3(0, 0, 0);

float DuDeSphere(float3 ptPos, float3 ptCenter, float duRadius)
{
	return length(ptPos - ptCenter) - duRadius;
}

static const float duRadiusLight = 0.01;
float DuDeLight(float3 ptPos, int iLight)
{
	return DuDeSphere(ptPos, rgptLight[iLight], duRadiusLight);
}

float DuDeObject(float3 ptPos)
{
#ifdef SPHERE
	return DuDeSphere(ptPos, ptOrigin, 0.3);
#endif
	return DuDeFractal(ptPos);
}

static const int ID_FRACTAL = -1;
float DuDeScene(float3 ptPos, out int idHit)
{
	idHit = ID_FRACTAL;
	float du = DuDeObject(ptPos);
	for (int iLight = 0; iLight < cLight; iLight++)
	{
		float duDeLight = DuDeLight(ptPos, iLight);
		if (duDeLight < du)
		{
			du = duDeLight;
			idHit = iLight;
		}
	}

	return du;
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
		DuDeObject(pt + vkX) - DuDeObject(pt - vkX),
		DuDeObject(pt + vkY) - DuDeObject(pt - vkY),
		DuDeObject(pt + vkZ) - DuDeObject(pt - vkZ)));
#else
	float duDEPt = DuDeObject(pt);
	float dx = DuDeObject(pt + float3(duEpsilon, 0, 0)) - duDEPt;
	float dy = DuDeObject(pt + float3(0, duEpsilon, 0)) - duDEPt;
	float dz = DuDeObject(pt + float3(0, 0, duEpsilon)) - duDEPt;
	return normalize(float3(dx, dy, dz));
#endif
}

static float duPixelRadius = rsViewPlane.x / rsScreen.x;
float DuEpsilon(float sfEpsilon, float duMarched)
{
	return sfEpsilon * 0.8 * duMarched / duNear * duPixelRadius;
}

const static float MARCHED_TIMEOUT = -1;
const static float MARCHED_LIMIT = -2;
float4 PtMarch(float3 pt, float3 vk, float sfEpsilon, float duMax, bool fIncludeLights, out int idHit, out float duEpsilon)
{
	duEpsilon = -1;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		idHit = ID_FRACTAL;
		float du = 0.999 * fIncludeLights
			? DuDeScene(pt, idHit)
			: DuDeObject(pt);

		pt += du * vk;
		duTotal += du;
		duEpsilon = DuEpsilon(sfEpsilon, duTotal);
		if (du < duEpsilon)
			return float4(pt, i);

		if (du > duMax)
			return float4(pt, MARCHED_LIMIT);
	}
	return float4(pt, MARCHED_TIMEOUT);
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

//static const float3 ptLight = float3(100, 0, 0);
static const float3 WHITE = float3(1, 1, 1);
static const float3 colorDiffuse = WHITE;
static const float3 colorSpecular = WHITE;
static const float shininess = 18.0;
static const float sfEpsilonShadow = 5.0;
float3 ColorBP(float3 color, float3 ptSurface, int idHit, float duEpsilon)
{
	if (idHit != ID_FRACTAL)
		return WHITE;

	color = 0.4 * color;

	for (int iLight = 0; iLight < cLight; iLight++)
	{
		float3 vkNormal = VkNormal(ptSurface, duEpsilon);
		float3 vkSurfaceToLight = rgptLight[iLight] - ptSurface;
		float3 vkLightDir = normalize(vkSurfaceToLight);
		float3 vkCameraDir = normalize(ptCamera - ptSurface);
		bool fInShadow = false;

#ifdef SHADOWS
		float epsilonIgnore;
		int idHitIgnore;
		float3 ptShadowStart = ptSurface + vkCameraDir * 1.1 * DuEpsilon(sfEpsilonShadow, length(ptCamera - ptSurface));
		float duToLight = length(vkSurfaceToLight);
		float4 shadow = PtMarch(ptShadowStart, vkLightDir, sfEpsilonShadow, duToLight, false, idHitIgnore, epsilonIgnore);

		// TODO: Should probably check if marched at least dist to light
		if (shadow.w != MARCHED_LIMIT)
			fInShadow = true;
#endif

		if (fInShadow)
			continue;

		float lambertian = max(dot(vkLightDir, vkNormal), 0.0);
		float specular = 0.0;

		if (lambertian > 0.0)
		{
			float3 vkHalf = normalize(vkLightDir + vkCameraDir);
			float cos = max(dot(vkHalf, vkNormal), 0.0);
			specular = pow(cos, shininess);
		}

		color = color
			+ 0.2 * lambertian * colorDiffuse
			+ 0.2 * specular * colorSpecular;

	}
	return color;
}

static const float duMarchLimit = 20;
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
	int idHit;
	float sfEpsilon = 2.0;
	float4 ptMarched = PtMarch(ptCamera, vkRay, sfEpsilon, duMarchLimit, true, idHit, duEpsilon);

	float duMarched = length(ptMarched.xyz - ptCamera);

	if (ptMarched.w == MARCHED_LIMIT || ptMarched.w == MARCHED_TIMEOUT)
		return float4(0, 0, 0, 1);

	float3 color = ColorOT(ptMarched);

#ifndef SPHERE
	color = ColorAO(color, ptMarched.w);

	color = ColorFog(color, float3(0.5, 0.6, 0.7), duMarched);
#endif

#ifdef LIGHTING
	color = ColorBP(color, ptMarched, idHit, duEpsilon);
#endif

	return float4(color.x, color.y, color.z, 1.0);
}