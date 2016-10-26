#include "parameters.hlsl"
float DuDeFractal(float3 pos)
{
	float Power = 8;
	int iterations = 10;
	float Bailout = 5;
	float3 z = pos;
	float dr = 1.0;
	float r = 0.0;
	for (int i = 0; i<iterations; i++)
	{
		r=length(z);
		if (r>Bailout)
			break;
		float theta = acos(z.z/r);
		float phi = atan(z.y/z.x);
		dr=pow(r, Power-1.0)*Power*dr+1.0;
		float zr = pow(r, Power);
		theta=theta*Power;
		phi=phi*Power;
		z=zr*float3(sin(theta)*cos(phi), sin(phi)*sin(theta), cos(theta));
		z+=pos;
	}
	return 0.5*log(r)*r/dr;
}
#include "rayTracer.hlsl"
