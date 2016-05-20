cbuffer Parameters
{
	float4 ptCamera;
	float4 vkCamera;
	float4 vkCameraOrtho;
	float2 rsScreen;
	float2 rsViewPlane;
	float duNear;
}

static const int cmarch = 48;

float DE(float3 pos)
{
	float Power = 8;
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
		float theta = acos(z.z / r);
		float phi = atan(z.y / z.x);
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

float4 ray_marching(float3 ro, float3 rd)
{
	float duPixelRadius = rsViewPlane.x / rsScreen.x;
	float duTotal = 0;
	for (int i = 0; i < cmarch; ++i)
	{
		float d = DE(ro);
		ro += d * rd;
		duTotal += d;
		float duEpsilon = 1 * duTotal / duNear * duPixelRadius;
		if (d < duEpsilon) return float4(ro, i);
	}
	return float4(ro, -1);
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

float4 main(float4 position : SV_POSITION) : SV_TARGET
{
	float3 ptPlaneCenter = ptCamera.xyz + vkCamera.xyz * duNear;

	float3 vkDown = vkCameraOrtho.xyz;
	float3 vkRight = cross(vkDown, vkCamera);

	float2 vkFromScreenCenter = position.xy - rsScreen / 2;
	float2 vkFromPlaneCenter = float2(vkFromScreenCenter.x * rsViewPlane.x / rsScreen.x, vkFromScreenCenter.y * rsViewPlane.y / rsScreen.y);
	float3 planePoint = ptPlaneCenter + vkRight * vkFromPlaneCenter.x + vkDown * vkFromPlaneCenter.y;

	float3 vkRay = normalized(planePoint - ptCamera.xyz);

	float4 red = float4(1, 0, 0, 1);
	float4 green = float4(0, 1, 0, 1);
	float4 blue = float4(0, 0, 1, 1);
	float4 marched = ray_marching(ptCamera.xyz, vkRay);

	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	float4 color = float4(0.2, 0.6, 0.3, 1);

	// ambient occlusion
	color = color * (1 - (marched.w / cmarch));

	return color;
}