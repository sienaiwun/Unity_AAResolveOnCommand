
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.LWRP
{
    public class RenderTargetHandle
    {
        public readonly int id;

        private readonly string m_renderTextureName;

        private RenderTexture m_colorHandle;

        private RenderTexture m_resolveHandle;

        private bool m_hasMsaa;

        private bool m_resolved;

        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle(-1);

        private RenderTargetHandle(int id)
        {
            this.id = id;
        }

        public RenderTargetHandle(string name, bool asRendertarget = true)
        {
            id = Shader.PropertyToID(name);
            if (!asRendertarget)
                m_renderTextureName = name;
        }

        public RenderTargetIdentifier GetShaderResource()
        {
            if (IsRenderTexuture())
            {
                if (m_hasMsaa && m_resolved)
                {
                    Assert.IsNotNull(m_resolveHandle);
                    return m_resolveHandle;
                }
                Assert.IsNotNull(m_colorHandle);
                return m_colorHandle;
            }
            if (id == -1)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }
            return new RenderTargetIdentifier(id);
        }


        public RenderTargetIdentifier Identifier()
        {
            if (IsRenderTexuture())
            {
                Assert.IsNotNull(m_colorHandle);
                return m_colorHandle;
            }
            if (id == -1)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }
            return new RenderTargetIdentifier(id);
        }

        public bool Equals(RenderTargetHandle other)
        {
            return id == other.id && m_colorHandle == other.m_colorHandle;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetHandle && Equals((RenderTargetHandle)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator==(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator!=(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return !c1.Equals(c2);
        }

        public void GetTemporary(CommandBuffer cmd, RenderTextureDescriptor desc,FilterMode mode)
        {
            if(IsRenderTexuture())
            {
                if(desc.msaaSamples >1)
                {
                    m_hasMsaa = true;
                    desc.bindMS = true;
                }
                m_colorHandle = RenderTexture.GetTemporary(desc);
                m_colorHandle.filterMode = mode;
                m_colorHandle.name = m_renderTextureName;
                if (m_hasMsaa)
                {
                    desc.bindMS = false;
                    desc.msaaSamples = 1;
                    m_resolveHandle = RenderTexture.GetTemporary(desc);
                    m_resolveHandle.filterMode = mode;
                    m_resolveHandle.name = m_renderTextureName + "_resolved";
                    m_resolveHandle.Create();
                    m_resolved = false;
                }
            }
            else
                cmd.GetTemporaryRT(id, desc, mode);
        }

        public void ResolveAA(CommandBuffer cmd)
        {
            if(m_hasMsaa)
            {
                cmd.ResolveAntiAliasedSurface(m_colorHandle, m_resolveHandle);
                m_resolved = true;
            }
        }

        public void ReleaseTemporary(CommandBuffer cmd)
        {
            if (IsRenderTexuture())
            {
                RenderTexture.ReleaseTemporary(m_colorHandle);
                if(m_hasMsaa)
                {
                    RenderTexture.ReleaseTemporary(m_resolveHandle);
                }
            }
            else
            { 
                Assert.AreNotEqual(id, -1);
                cmd.ReleaseTemporaryRT(id);
            }
        }

        private bool IsRenderTexuture()
        {
            return m_renderTextureName != null;
        }
    }
}
