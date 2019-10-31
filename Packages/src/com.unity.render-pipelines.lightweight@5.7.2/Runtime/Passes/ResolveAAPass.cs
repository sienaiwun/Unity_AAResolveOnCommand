using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Perform ResolvePass On a render target
    /// </summary>
    internal class ResolveAAPass : ScriptableRenderPass
    {
        RenderTargetHandle m_Source;
        
        const string k_RenderPostProcessingTag = "Resolve Render Target";

        public ResolveAAPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        /// <summary>
        /// Setup the pass
        /// </summary>
        /// <param name="sourceHandle">Source of rendering to execute the post on</param>
        public void Setup(RenderTargetHandle sourceHandle)
        {
            m_Source = sourceHandle;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("resolved color buffer");
            m_Source.ResolveAA(cmd);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }
    }
}
