using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;

namespace Clockwork.Graphics
{
    public class RenderTargetBinding
    {
        public bool InheritDepthStencil;

        public DepthStencilView DepthStencil;

        public RenderTargetView[] RenderTargets;

        public RenderTargetBinding(DepthStencilView depthStencil, bool inheritDepthStencil, params RenderTargetView[] renderTargets)
        {
            InheritDepthStencil = inheritDepthStencil;
            DepthStencil = depthStencil;
            RenderTargets = renderTargets;
        }

        public void Bind(GraphicsDevice device)
        {
            DepthStencilView depthStencilView;
            if (InheritDepthStencil)
            {
                device.GetRenderTargets(out depthStencilView);
            }
            else
            {
                depthStencilView = DepthStencil;
            }

            device.SetRenderTargets(depthStencilView, RenderTargets);

            if (device.AutoViewportFromRenderTargets && (RenderTargets == null || RenderTargets.Length == 0))
            {
                var buffer = depthStencilView.Tag as TextureView;
                if (buffer != null)
                    device.SetViewport(0, 0, buffer.Width, buffer.Height);
            }
        }
    }

    public class RenderTargetStack
    {
        private readonly Stack<RenderTargetBinding> bindings = new Stack<RenderTargetBinding>();
        private readonly GraphicsDevice graphics;

        public RenderTargetStack(GraphicsDevice graphics)
        {
            this.graphics = graphics;
        }

        public void Pop()
        {
            bindings.Pop();

            if (bindings.Count > 0)
            {
                bindings.Peek().Bind(graphics);
            }
            else
            {
                graphics.SetRenderTargets(graphics.DepthStencilBuffer, graphics.BackBuffer);
            }
        }

        public RenderTargetScope Push(DepthStencilView depthStencilView, params RenderTargetView[] renderTargetViews)
        {
            return Push(new RenderTargetBinding(depthStencilView, false, renderTargetViews));
        }

        public RenderTargetScope Push(params RenderTargetView[] renderTargetViews)
        {
            return Push(new RenderTargetBinding(null, false, renderTargetViews));
        }

        public RenderTargetScope Push(bool inheritDepthStencil, params RenderTargetView[] renderTargetViews)
        {
            return Push(new RenderTargetBinding(null, inheritDepthStencil, renderTargetViews));
        }

        private RenderTargetScope Push(RenderTargetBinding binding)
        {
            bindings.Push(binding);
            binding.Bind(graphics);
            return new RenderTargetScope(this);
        }
    }

    public class RenderTargetScope : IDisposable
    {
        private readonly RenderTargetStack stack;

        public RenderTargetScope(RenderTargetStack stack)
        {
            this.stack = stack;
        }

        public void Dispose()
        {
            stack.Pop();
        }
    }
}
