cbuffer Parameters
{
	float4 cameraPos;
	float4 cameraView;
	float4 cameraOrtho;
	float nearDist;
	float roll;
	float2 viewDimension;
}

float DE(float3 p)
{
	float3 c = p;
	float r = length(c);
	float dr = 1;
	for (int i = 0; i < 4 && r < 3; ++i)
	{
		float xr = pow(r, 7);
		dr = 6 * xr * dr + 1;

		float theta = atan2(c.y, c.x) * 8;
		float phi = asin(c.z / r) * 8;
		r = xr * r;
		c = r * float3(cos(phi) * cos(theta), cos(phi) * sin(theta), sin(phi));

		c += p;
		r = length(c);
	}
	return 0.35 * log(r) * r / dr;
}

float4 ray_marching(float3 ro, float3 rd)
{
	for (int i = 0; i < 64; ++i)
	{
		float d = DE(ro);
		ro += d * rd;
		if (d < 0.001) return float4(ro, i);
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
	float3 planeCenter = cameraPos.xyz + cameraView.xyz * nearDist;

	float3 vkDown = cross(cameraOrtho, cameraView);
	float3 vkRight = cross(vkDown, cameraView);

	float2 pixelPos = position.xy - viewDimension / 2;
	float3 planePoint = planeCenter + vkRight * pixelPos.x + vkDown * pixelPos.y;

	float3 r0 = planePoint - cameraPos.xyz;

	float4 marched = ray_marching(cameraPos.xyz, r0);
	if (marched.w == -1)
		return float4(0, 0, 0, 1);

	return float4(1, 1, 1, 1);
}