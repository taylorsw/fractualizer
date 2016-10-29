using System;
using Fractals;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
namespace Fractals
{
	public class Mandelbulb : Fractal3d
	{
		[StructLayout(LayoutKind.Explicit, Size=16)]
		public struct _Mandelbulb
		{
			public static readonly _Mandelbulb I = new _Mandelbulb(param: 8.0f, param2: 1.0f);
			[FieldOffset(0)]
			public float param;
			[FieldOffset(4)]
			public float param2;
			public _Mandelbulb(float param = 8.0f, float param2 = 1.0f)
			{
				this.param = param;
				this.param2 = param2;
			}
		}
		private _Mandelbulb _mandelbulb;
		private SharpDX.Direct3D11.Buffer buffer;
		public Mandelbulb()
		{
			this._mandelbulb = _Mandelbulb.I;
		}
		public Mandelbulb(_Mandelbulb _mandelbulb)
		{
			this._mandelbulb = _mandelbulb;
		}
		public override void InitializeBuffer(Device device, DeviceContext deviceContext)
		{
			buffer = Util.BufferCreate(device, deviceContext, 1, ref _mandelbulb);
		}
		public override void UpdateBuffer(Device device, DeviceContext deviceContext)
		{
			Util.UpdateBuffer(device, deviceContext, buffer, ref _mandelbulb);
		}
		public override void ResetInputs() { _mandelbulb = _Mandelbulb.I; }
		public override int cinputFloat => 2;
		public override float GetInputFloat(int iinput)
		{
			if (iinput == 0) return _mandelbulb.param;
			if (iinput == 1) return _mandelbulb.param2;
			return base.GetInputFloat(iinput);
		}
		public override void SetInputFloat(int iinput, float val)
		{
			if (iinput == 0) _mandelbulb.param = val;
			if (iinput == 1) _mandelbulb.param2 = val;
			base.SetInputFloat(iinput, val);
		}
		protected override void DisposeI() { buffer.Dispose(); }
		protected override double DuEstimateI(Vector3d pos)
		{
			double Power = _mandelbulb.param;
			int iterations = 10;
			double Bailout = 5;
			Vector3d z = pos;
			double dr = 1.0;
			double r = 0.0;
			for (int i = 0; i<iterations; i++)
			{
				r=Vector3d.Length(z);
				if (r>Bailout)
					break;
				double theta = Math.Acos(z.z/r)/_mandelbulb.param2;
				double phi = Util.Atan(z.y/z.x)*_mandelbulb.param2;
				dr=Math.Pow(r, Power-1.0)*Power*dr+1.0;
				double zr = Math.Pow(r, Power);
				theta=theta*Power;
				phi=phi*Power;
				z=zr*new Vector3d(Math.Sin(theta)*Math.Cos(phi), Math.Sin(phi)*Math.Sin(theta), Math.Cos(theta));
				z+=pos;
			}
			return 0.5*Math.Log(r)*r/dr;
		}
	}
}
