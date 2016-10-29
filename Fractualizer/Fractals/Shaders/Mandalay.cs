using System;
using Fractals;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
namespace Fractals
{
	public class Mandalay : Fractal3d
	{
		double DBFold(Vector3d p, double fo, double g, double w)
		{
			if (p.z>p.y)
			{
				double zTemp = p.z;
				p.z=p.y;
				p.y=zTemp;
			}
			double vx = p.x-2.0*fo;
			double vy = p.y-4.0*fo;
			double v = Math.Max(Util.Abs(vx+fo)-fo, vy);
			double v1 = Math.Max(vx-g, p.y-w);
			v=Math.Min(v, v1);
			v1=Math.Max(v1, -Util.Abs(p.x));
			return Math.Min(v, p.x);
		}


		Vector3d DBFoldParallel(Vector3d p, Vector3d fo, Vector3d g, Vector3d w)
		{
			Vector3d p1 = p;
			p.x=DBFold(p1, fo.x, g.x, w.x);
			p.y=DBFold(new Vector3d(p1.y, p1.z, p1.x), fo.y, g.y, w.y);
			p.z=DBFold(new Vector3d(p1.z, p1.x, p1.y), fo.z, g.z, w.z);
			return p;
		}


		static Vector3d fo = new Vector3d(0.7, 0.9528, 0.9);
		static Vector3d gh = new Vector3d(0.8, 0.7, 0.5638);
		static Vector3d gw = new Vector3d(0.3, 0.5, 0.2);
		static Vector4d X = new Vector4d(0.1, 0.5, 0.1, 0.3);
		static Vector4d Y = new Vector4d(0.1, 0.8, 0.1, 0.1);
		static Vector4d Z = new Vector4d(0.2, 0.2, 0.2, 0.45902);
		static Vector4d R = new Vector4d(0.19, 0.1, 0.1, 0.2);
		static double sr = 20.0;
		static double Scale = 4.0;
		static double MinRad2 = 0.25;
		double DuDeFractalI(Vector3d p)
		{
			Vector3d JC = p;
			double JCw = 1;
			double r2 = Vector3d.Dot(p, p);
			double dd = 1;
			for (int i = 0; i<6; i++)
			{
				p=p-Util.Clamp(new Vector3d(p.x, p.y, p.z), -1.0, 1.0)*2.0;
				Vector3d signs = new Vector3d(Math.Sign(p.x), Math.Sign(p.y), Math.Sign(p.z));
				p=Util.Abs(p);
				p=DBFoldParallel(p, fo, gh, gw);
				p*=signs;
				r2=Vector3d.Dot(p, p);
				double t = Util.Clamp(1.0/r2, 1, 1.0/MinRad2);
				p*=t;
				dd*=t;
				p=p*Scale+JC;
				dd=dd*Scale+JCw;
				p=new Vector3d(1.0, 1.0, 0.92)*p;
				r2=Vector3d.Dot(p, p);
			}
			dd=Util.Abs(dd);
			double val = (Math.Sqrt(r2)-sr)/dd;
			return val;
		}


		public override double DuEstimate(Vector3d pos)
		{
			return DuDeFractalI(10*pos)/10;
		}
	}
}
