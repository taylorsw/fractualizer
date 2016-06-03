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

float DE_Bulb(float3 pos)
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

float4 ray_marching(float3 pt, float3 vk)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float du = DE_Bulb(pt);
		pt += du * vk;
		duTotal += du;
		float duEpsilon = 0.5 * duTotal / duNear * duPixelRadius;
		if (du < duEpsilon)
			return float4(pt, i);
	}
	return float4(pt, -1);
}

float3 cross(float3 a, float3 b)
{
	return float3(a.y * b.z - a.z * b.x, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
}

float length(float3 v)
{
	return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

float3 normalized(float3 v)
{
	return v / length(v);
}

// Basic orbit-trapping color
float3 ColorOT(float4 marched)
{
	return normalized(
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
	float3 ptPlaneCenter = ptCamera + vkCamera * duNear;

	float3 vkDown = vkCameraOrtho;
	float3 vkRight = cross(vkDown, vkCamera);

	float2 vkFromScreenCenter = position.xy - rsScreen / 2;
	float2 vkFromPlaneCenter = float2(vkFromScreenCenter.x * rsViewPlane.x / rsScreen.x, vkFromScreenCenter.y * rsViewPlane.y / rsScreen.y);
	float3 planePoint = ptPlaneCenter + vkRight * vkFromPlaneCenter.x + vkDown * vkFromPlaneCenter.y;

	float3 vkRay = normalized(planePoint - ptCamera);

	float4 red = float4(1, 0, 0, 1);
	float4 green = float4(0, 1, 0, 1);
	float4 blue = float4(0, 0, 1, 1);
	float4 marched = ray_marching(ptCamera, vkRay);

	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	float3 colorOT = ColorOT(marched);

	float3 colorAO = ColorAO(colorOT, marched.w);

	return float4(colorAO.x, colorAO.y, colorAO.z, 1.0);
}