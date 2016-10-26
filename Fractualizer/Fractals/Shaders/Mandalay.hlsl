#include "parameters.hlsl"
float DBFold(float3 p, float fo, float g, float w)
{
	if (p.z>p.y)
	{
		float zTemp = p.z;
		p.z=p.y;
		p.y=zTemp;
	}
	float vx = p.x-2.0*fo;
	float vy = p.y-4.0*fo;
	float v = max(abs(vx+fo)-fo, vy);
	float v1 = max(vx-g, p.y-w);
	v=min(v, v1);
	v1=max(v1, -abs(p.x));
	return min(v, p.x);
}


float3 DBFoldParallel(float3 p, float3 fo, float3 g, float3 w)
{
	float3 p1 = p;
	p.x=DBFold(p1, fo.x, g.x, w.x);
	p.y=DBFold(float3(p1.y, p1.z, p1.x), fo.y, g.y, w.y);
	p.z=DBFold(float3(p1.z, p1.x, p1.y), fo.z, g.z, w.z);
	return p;
}


static const float3 fo = float3(0.7, 0.9528, 0.9);
static const float3 gh = float3(0.8, 0.7, 0.5638);
static const float3 gw = float3(0.3, 0.5, 0.2);
static const float4 X = float4(0.1, 0.5, 0.1, 0.3);
static const float4 Y = float4(0.1, 0.8, 0.1, 0.1);
static const float4 Z = float4(0.2, 0.2, 0.2, 0.45902);
static const float4 R = float4(0.19, 0.1, 0.1, 0.2);
static const float sr = 20.0;
static const float Scale = 4.0;
static const float MinRad2 = 0.25;
float DuDeFractalI(float3 p)
{
	float3 JC = p;
	float JCw = 1;
	float r2 = dot(p, p);
	float dd = 1;
	for (int i = 0; i<6; i++)
	{
		p=p-clamp(float3(p.x, p.y, p.z), -1.0, 1.0)*2.0;
		float3 signs = float3(sign(p.x), sign(p.y), sign(p.z));
		p=abs(p);
		p=DBFoldParallel(p, fo, gh, gw);
		p*=signs;
		r2=dot(p, p);
		float t = clamp(1.0/r2, 1, 1.0/MinRad2);
		p*=t;
		dd*=t;
		p=p*Scale+JC;
		dd=dd*Scale+JCw;
		p=float3(1.0, 1.0, 0.92)*p;
		r2=dot(p, p);
	}
	dd=abs(dd);
	float val = (sqrt(r2)-sr)/dd;
	return val;
}


float DuDeFractal(float3 pos)
{
	return DuDeFractalI(10*pos)/10;
}
#include "rayTracer.hlsl"
