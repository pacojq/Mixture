﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Rendering;

namespace Mixture
{
	[System.Serializable, NodeMenuItem("Operators/Distance")]
	public class Distance : ComputeShaderNode
	{
		[Input("Input")]
		public Texture input;

		[Output("Output")]
		public CustomRenderTexture output;

		public float threshold = 0.5f;
		public float distance = 50;

		public override string name => "Distance";

		protected override string computeShaderResourcePath => "Mixture/Distance";

		public override bool showDefaultInspector => true;

		public override Texture previewTexture => output;

		public override List<OutputDimension> supportedDimensions => new List<OutputDimension>() {
			OutputDimension.Texture2D,
			OutputDimension.Texture3D,
		};

		int fillUvKernel;
		int jumpFloodingKernel;
		int finalPassKernel;

		[CustomPortBehavior(nameof(input))]
		protected IEnumerable< PortData > ChangeInputPortType(List< SerializableEdge > edges)
		{
			yield return new PortData{
				displayName = "output",
				displayType = TextureUtils.GetTypeFromDimension(rtSettings.GetTextureDimension(graph)),
				identifier = "output",
				acceptMultipleEdges = true,
			};
		}

		[CustomPortBehavior(nameof(output))]
		protected IEnumerable< PortData > ChangeOutputPortType(List< SerializableEdge > edges)
		{
			yield return new PortData{
				displayName = "output",
				displayType = TextureUtils.GetTypeFromDimension(rtSettings.GetTextureDimension(graph)),
				identifier = "output",
				acceptMultipleEdges = true,
			};
		}

		protected override void Enable()
		{
			base.Enable();

			rtSettings.outputChannels = OutputChannel.RGBA;
			rtSettings.outputPrecision = OutputPrecision.Full;
			rtSettings.editFlags = EditFlags.Dimension | EditFlags.Size;

			UpdateTempRenderTexture(ref output);

			fillUvKernel = computeShader.FindKernel("FillUVMap");
			jumpFloodingKernel = computeShader.FindKernel("JumpFlooding");
			finalPassKernel = computeShader.FindKernel("FinalPass");
		}

		protected override bool ProcessNode(CommandBuffer cmd)
		{
			// Force the double buffering for multi-pass flooding
			rtSettings.doubleBuffered = true;

			if (!base.ProcessNode(cmd) || input == null)
				return false;

			UpdateTempRenderTexture(ref output);

			cmd.SetComputeFloatParam(computeShader, "_Threshold", threshold);
			cmd.SetComputeVectorParam(computeShader, "_Size", new Vector4(output.width, 1.0f / output.width));
			cmd.SetComputeFloatParam(computeShader, "_Distance", distance / 100.0f);

			output.doubleBuffered = true;
			output.EnsureDoubleBufferConsistency();
			var rt = output.GetDoubleBufferRenderTexture();
			rt.Release();
			rt.enableRandomWrite = true;
			rt.Create();

			MixtureUtils.SetupComputeDimensionKeyword(computeShader, input.dimension);

			cmd.SetComputeTextureParam(computeShader, fillUvKernel, "_Input", input);
			cmd.SetComputeTextureParam(computeShader, fillUvKernel, "_Output", output);
			cmd.SetComputeTextureParam(computeShader, fillUvKernel, "_FinalOutput", rt);
			cmd.SetComputeFloatParam(computeShader, "_InputScaleFactor", (float)input.width / (float)output.width);
			DispatchCompute(cmd, fillUvKernel, output.width, output.height, output.volumeDepth);

			int maxLevels = (int)Mathf.Log(input.width, 2);
			for (int i = 0; i <= maxLevels; i++)
			{
				float offset = 1 << (maxLevels - i);
				cmd.SetComputeFloatParam(computeShader, "_InputScaleFactor", 1);
				cmd.SetComputeFloatParam(computeShader, "_Offset", offset);
				cmd.SetComputeTextureParam(computeShader, jumpFloodingKernel, "_Input", output);
				cmd.SetComputeTextureParam(computeShader, jumpFloodingKernel, "_Output", rt);
				DispatchCompute(cmd, jumpFloodingKernel, output.width, output.height, output.volumeDepth);
				cmd.CopyTexture(rt, output);
			}

			cmd.SetComputeFloatParam(computeShader, "_InputScaleFactor", (float)input.width / (float)output.width);
			cmd.SetComputeTextureParam(computeShader, finalPassKernel, "_Input", input);
			cmd.SetComputeTextureParam(computeShader, finalPassKernel, "_Output", rt);
			cmd.SetComputeTextureParam(computeShader, finalPassKernel, "_FinalOutput", output);
			DispatchCompute(cmd, finalPassKernel, output.width, output.height, output.volumeDepth);

			return true;
		}

        protected override void Disable() => CoreUtils.Destroy(output);

		public CustomRenderTexture GetCustomRenderTexture() => output;
	}
}