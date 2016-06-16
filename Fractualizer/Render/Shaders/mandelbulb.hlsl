﻿cbuffer Parameters
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

float DE_sphere(float3 pos)
{
	return length(pos) - 1;
}

float DE(float3 pos)
{
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

float3 Normal(float3 pt, float duDEPt, float duEpsilon)
{
	float dx = DE(pt + float3(duEpsilon, 0, 0)) - duDEPt;
	float dy = DE(pt + float3(0, duEpsilon, 0)) - duDEPt;
	float dz = DE(pt + float3(0, 0, duEpsilon)) - duDEPt;
	return normalize(float3(dx, dy, dz));
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
		float duEpsilon = 0.5 * duTotal / duNear * duPixelRadius;
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

static const int fogB = 1.0;
static const int fogA = 1.0;
float3 ColorFog(float3 color, float3 fogColor, float duMarched)
{
	float frFog = 1.0 - fogA * exp(-duMarched / 5);
	return lerp(color, fogColor, frFog);
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

	float4 ptMarched = ray_marching(ptCamera, vkRay);
	float duMarched = length(ptMarched.xyz - ptCamera);

	if (ptMarched.w == -1)
		return float4(0, 0, 0, 1);

	float3 color = ColorOT(ptMarched);

	color = ColorAO(color, ptMarched.w);

	color = ColorFog(color, float3(0.5, 0.6, 0.7), duMarched);

	return float4(color.x, color.y, color.z, 1.0);
}