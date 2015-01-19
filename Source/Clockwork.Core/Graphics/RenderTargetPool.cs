using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;

namespace Clockwork.Graphics
{
    /// <summary>
    /// Represents a pool of render targets.
    /// </summary>
    public class RenderTargetPool
    {
        private RenderTargetPoolKey sharedKey = new RenderTargetPoolKey();
        private Dictionary<RenderTargetPoolKey, List<RenderTarget>> renderTargetPools
            = new Dictionary<RenderTargetPoolKey, List<RenderTarget>>(new RenderTargetPoolKeyEqualityComparer());

        private Dictionary<RenderTarget, int> referenceCounts = new Dictionary<RenderTarget, int>();

        public GraphicsDevice GraphicsDevice { get; private set; }

        public RenderTargetPool(GraphicsDevice deivce)
        {
            GraphicsDevice = deivce;
        }

        /// <summary>
        /// Creates a render target from the pool without locking it.
        /// </summary>
        public RenderTarget GetRenderTarget(int width, int height, PixelFormat format)
        {
            var key = sharedKey;
            key.Width = width;
            key.Height = height;
            key.PixelFormat = format;

            List<RenderTarget> tags;
            if (!renderTargetPools.TryGetValue(key, out tags))
            {
                key = new RenderTargetPoolKey();
                key.Width = width;
                key.Height = height;
                key.PixelFormat = format;

                var texture = Texture2D.New(GraphicsDevice, width, height, format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                var tag = texture.ToRenderTarget();
                tags = new List<RenderTarget>();
                tags.Add(tag);
                renderTargetPools.Add(key, tags);
                referenceCounts[tag] = 0;
                return tag;
            }

            foreach (var tag in tags)
            {
                if (referenceCounts[tag] <= 0)
                    return tag;
            }

            {
                var texture = Texture2D.New(GraphicsDevice, width, height, format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                var tag = texture.ToRenderTarget();
                tags.Add(tag);
                referenceCounts[tag] = 0;
                return tag;
            }
        }

        /// <summary>
        /// Locks the target render target to prevent it from being created from the pool.
        /// </summary>
        public void DoLock(RenderTarget target)
        {
            if (target == null)
                return;

            if (referenceCounts.ContainsKey(target))
            {
                referenceCounts[target]++;
            }
            else
            {
                referenceCounts[target] = 1;
            }
        }

        /// <summary>
        /// Unlocks the target render target.
        /// </summary>
        public void Unlock(RenderTarget target)
        {
            if (target == null)
                return;

            referenceCounts[target]--;
        }

        public void Crear()
        {
            foreach (var pool in renderTargetPools.Values)
            {
                foreach (var target in pool)
                {
                    target.Texture.Dispose();
                    target.Dispose();
                }
            }

            renderTargetPools.Clear();
            referenceCounts.Clear();
        }

        public RenderTargetLock Lock(RenderTarget resource)
        {
            return new RenderTargetLock(this, resource);
        }

        public struct RenderTargetLock : IDisposable
        {
            RenderTarget renderTarget;
            RenderTargetPool pool;

            public RenderTargetLock(RenderTargetPool pool, RenderTarget renderTarget)
            {
                this.renderTarget = renderTarget;
                this.pool = pool;
                pool.DoLock(renderTarget);
            }

            public void Dispose()
            {
                pool.Unlock(renderTarget);
            }
        }
    }

    class RenderTargetPoolKey
    {
        public int Width;
        public int Height;
        public PixelFormat PixelFormat;
    }

    class RenderTargetPoolKeyEqualityComparer : IEqualityComparer<RenderTargetPoolKey>
    {
        public bool Equals(RenderTargetPoolKey x, RenderTargetPoolKey y)
        {
            return x.Width == y.Width &&
                   x.Height == y.Height &&
                   x.PixelFormat == y.PixelFormat;
        }

        public int GetHashCode(RenderTargetPoolKey obj)
        {
            return obj.Width.GetHashCode() ^
                   obj.Height.GetHashCode() ^
                   (int)obj.PixelFormat;
        }
    }
}
