using System;
using System.Drawing;
using SharpDX.Windows;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX;
using SharpDX.D3DCompiler;

namespace Render
{
    public class Renderer : IDisposable
    {
        private readonly IHaveScene ihs;
        private Scene scene => ihs.scene;

        private D3D11.Device device;
        private D3D11.DeviceContext deviceContext;
        private SwapChain swapChain;
        private D3D11.RenderTargetView renderTargetView;
        private readonly Vector3[] vertices =
        {
            new Vector3(-1f, -1f, 0.0f), new Vector3(-1f, 1f, 0.0f), new Vector3(1f, -1f, 0.0f),
            new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0)
        };

        private D3D11.Buffer triangleVertexBuffer;
        private D3D11.VertexShader vertexShader;
        private ShaderSignature inputSignature;
        private D3D11.InputLayout inputLayout;
        private Viewport viewport;

        private readonly D3D11.InputElement[] inputElements =
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0)
        };

        public Renderer(IHaveScene ihs, RenderForm renderForm)
        {
            this.ihs = ihs;
            InitializeDeviceResources(renderForm);
            InitializeShaders();
        }

        public void Render()
        {
            deviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Color(32, 103, 178));

            deviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(triangleVertexBuffer, Utilities.SizeOf<Vector3>(), 0));

            scene.UpdateBuffers(device, deviceContext);

            deviceContext.Draw(vertices.Length, 0);

            swapChain.Present(1, PresentFlags.None);
        }

        private void InitializeShaders()
        {
            triangleVertexBuffer = D3D11.Buffer.Create<Vector3>(device, D3D11.BindFlags.VertexBuffer, vertices);

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("ShadersKludge/vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug))
            {
                inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                vertexShader = new D3D11.VertexShader(device, vertexShaderByteCode);
            }
            
            // Set as current vertex and pixel shaders
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            inputLayout = new D3D11.InputLayout(device, inputSignature, inputElements);
            deviceContext.InputAssembler.InputLayout = inputLayout;

            scene.Initialize(device, deviceContext);
        }

        private void InitializeDeviceResources(RenderForm renderForm)
        {
            ModeDescription backBufferDesc = new ModeDescription(renderForm.Width, renderForm.Height, new Rational(120, 1), Format.R8G8B8A8_UNorm);
            SwapChainDescription swapChainDesc = new SwapChainDescription
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true,
                Flags = SwapChainFlags.AllowModeSwitch
            };
            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDesc, out device, out swapChain);
            deviceContext = device.ImmediateContext;

            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0))
            {
                renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
            }

            deviceContext.OutputMerger.SetRenderTargets(renderTargetView);
            
            // Set viewport
            viewport = new Viewport(0, 0, renderForm.Width, renderForm.Height);
            deviceContext.Rasterizer.SetViewport(viewport);
        }

        public void Dispose()
        {
            inputLayout.Dispose();
            inputSignature.Dispose();
            triangleVertexBuffer.Dispose();
            vertexShader.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            device.Dispose();
            deviceContext.Dispose();
            scene.Dispose();
        }
    }
}
