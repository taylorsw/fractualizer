﻿#include "parameters.hlsl"

float DuDeFractal(float3 pos)
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
		/*float phi = atan2(z.x, z.y) * param2;*/
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

#include "rayTracer.hlsl"