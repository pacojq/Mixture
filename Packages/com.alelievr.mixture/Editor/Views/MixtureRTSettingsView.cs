using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using GraphProcessor;
using System.Linq;
using System;

namespace Mixture
{
	public class MixtureRTSettingsView : VisualElement
	{
        EnumField outputWidthMode;
		EnumField outputHeightMode;
		EnumField outputDepthMode;
		EnumField outputDimension;
		EnumField outputFormat;
        EnumField wrapMode;
        EnumField filterMode;

        IntegerField outputWidth;
		FloatField outputWidthPercentage;
        IntegerField outputHeight;
		FloatField outputHeightPercentage;
        IntegerField outputDepth;
		FloatField outputDepthPercentage;

		Toggle doubleBuffered;

        event Action onChanged;

        MixtureGraphView    owner;
        MixtureNode         node;
        MixtureGraph        graph;

        public MixtureRTSettingsView(MixtureNode node, MixtureGraphView owner)
        {
            this.graph = owner.graph as MixtureGraph;
            this.node = node;
            this.owner = owner;

            ReloadSettingsView();
            
            onChanged += ReloadSettingsView;
        }

        void ReloadSettingsView()
        {
            // Remove all old fields
            Clear();

            var title = new Label("Node Output Settings");
            var dimension = node.rtSettings.GetTextureDimension(graph);
            title.AddToClassList("PropertyEditorTitle");
            this.Add(title);

            // Wrap and Filter Modes
            var smpHeader = new Label("Sampler States");
            smpHeader.AddToClassList("PropertyEditorHeader");
            this.Add(smpHeader);

            wrapMode = new EnumField(node.rtSettings.wrapMode)
            {
                label = "Wrap Mode",
            };
            wrapMode.RegisterValueChangedCallback(e =>
            {
                owner.RegisterCompleteObjectUndo("Updated Wrap Mode " + e.newValue);
                node.rtSettings.wrapMode = (TextureWrapMode)e.newValue;
                onChanged?.Invoke();
            });

            filterMode = new EnumField(node.rtSettings.filterMode)
            {
                label = "Filter Mode",
            };
            filterMode.RegisterValueChangedCallback(e =>
            {
                owner.RegisterCompleteObjectUndo("Updated Filter Mode " + e.newValue);
                node.rtSettings.filterMode = (FilterMode)e.newValue;
                onChanged?.Invoke();
            });

            this.Add(wrapMode);
            this.Add(filterMode);

            // Size Modes
            var sizeHeader = new Label("Size Properties");
            sizeHeader.AddToClassList("PropertyEditorHeader");
            this.Add(sizeHeader);

            outputWidthMode = new EnumField(node.rtSettings.widthMode) {
                label = "Width Mode",
            };
            outputWidthMode.RegisterValueChangedCallback(e => {
                owner.RegisterCompleteObjectUndo("Updated Texture Dimension " + e.newValue);
                node.rtSettings.widthMode = (OutputSizeMode)e.newValue;
                onChanged?.Invoke();
                UpdateFieldVisibility(node);
            });
            this.Add(outputWidthMode);

            if (dimension != TextureDimension.Cube)
            {
                outputHeightMode = new EnumField(node.rtSettings.heightMode) {
                    label = "Height Mode",
                };
                outputHeightMode.RegisterValueChangedCallback(e => {
                    owner.RegisterCompleteObjectUndo("Updated Texture Dimension " + e.newValue);
                    node.rtSettings.heightMode = (OutputSizeMode)e.newValue;
                    onChanged?.Invoke();
                    UpdateFieldVisibility(node);
                });
                this.Add(outputHeightMode);

                if (dimension == TextureDimension.Tex3D)
                {
                    outputDepthMode = new EnumField(node.rtSettings.depthMode) {
                        label = "Depth Mode",
                    };
                    outputDepthMode.RegisterValueChangedCallback(e => {
                        owner.RegisterCompleteObjectUndo("Updated Texture Dimension " + e.newValue);
                        node.rtSettings.depthMode = (OutputSizeMode)e.newValue;
                        onChanged?.Invoke();
                        UpdateFieldVisibility(node);
                    });
                    this.Add(outputDepthMode);
                }
            }

            outputWidth = new IntegerField()
            {
                value = node.rtSettings.width,
                label = "Width",
                isDelayed = true,
            };
            outputWidth.RegisterValueChangedCallback(e =>
            {
                owner.RegisterCompleteObjectUndo("Updated Width " + e.newValue);
                node.rtSettings.width = e.newValue;
                onChanged?.Invoke();
            });
            this.Add(outputWidth);

            outputWidthPercentage = new FloatField()
            {
                value = node.rtSettings.widthPercent,
                label = "Width Percentage",
                isDelayed = true,
            };
            outputWidthPercentage.RegisterValueChangedCallback(e =>
            {
                owner.RegisterCompleteObjectUndo("Updated Width " + e.newValue);
                node.rtSettings.widthPercent = e.newValue;
                onChanged?.Invoke();
            });
            this.Add(outputWidthPercentage);

            if (dimension != TextureDimension.Cube)
            {
                outputHeight = new IntegerField()
                {
                    value = node.rtSettings.height,
                    label = "Height",
                    isDelayed = true,
                };
                outputHeight.RegisterValueChangedCallback(e =>
                {
                    owner.RegisterCompleteObjectUndo("Updated Height " + e.newValue);
                    node.rtSettings.height = e.newValue;
                    onChanged?.Invoke();
                });
                this.Add(outputHeight);

                outputHeightPercentage = new FloatField()
                {
                    value = node.rtSettings.heightPercent,
                    label = "Height Percentage",
                    isDelayed = true,
                };
                outputHeightPercentage.RegisterValueChangedCallback(e =>
                {
                    owner.RegisterCompleteObjectUndo("Updated Height " + e.newValue);
                    node.rtSettings.heightPercent = e.newValue;
                    onChanged?.Invoke();
                });
                this.Add(outputHeightPercentage);

                if (dimension == TextureDimension.Tex3D)
                {
                    outputDepth = new IntegerField()
                    {
                        value = node.rtSettings.sliceCount,
                        label = "Depth",
                        isDelayed = true,
                    };
                    outputDepth.RegisterValueChangedCallback(e =>
                    {
                        owner.RegisterCompleteObjectUndo("Updated Depth " + e.newValue);
                        node.rtSettings.sliceCount = e.newValue;
                        onChanged?.Invoke();
                    });
                    this.Add(outputDepth);

                    outputDepthPercentage = new FloatField()
                    {
                        value = node.rtSettings.depthPercent,
                        label = "Depth Percentage",
                        isDelayed = true,
                    };
                    outputDepthPercentage.RegisterValueChangedCallback(e =>
                    {
                        owner.RegisterCompleteObjectUndo("Updated Depth " + e.newValue);
                        node.rtSettings.depthPercent = e.newValue;
                        onChanged?.Invoke();
                    });
                    this.Add(outputDepthPercentage);
                }
            }

            // Dimension and Pixel Format
            var formatHeader = new Label("Format");
            formatHeader.AddToClassList("PropertyEditorHeader");
            this.Add(formatHeader);

            outputDimension = new EnumField(node.rtSettings.dimension) {
                label = "Dimension",
            };
            outputDimension.RegisterValueChangedCallback(e => {
                owner.RegisterCompleteObjectUndo("Updated Texture Dimension " + e.newValue);
                node.rtSettings.dimension = (OutputDimension)e.newValue;
                onChanged?.Invoke();
            });

            outputFormat = new EnumField(node.rtSettings.targetFormat) {
                label = "Pixel Format",
            };
            outputFormat.RegisterValueChangedCallback(e => {
                owner.RegisterCompleteObjectUndo("Updated Graphics Format " + e.newValue);
                node.rtSettings.targetFormat = (OutputFormat)e.newValue;
                onChanged?.Invoke();
            });

			this.Add(outputDimension);
			this.Add(outputFormat);

            UpdateFieldVisibility(node);

			if (owner.graph.isRealtime)
				AddRealtimeFields(node, owner);
        }

		void AddRealtimeFields(MixtureNode node, MixtureGraphView owner)
		{
			doubleBuffered = new Toggle("Double Buffered") {
				value = node.rtSettings.doubleBuffered,
			};
			doubleBuffered.RegisterValueChangedCallback(e => {
				owner.RegisterCompleteObjectUndo("Set Double Buffered " + e.newValue);
				node.rtSettings.doubleBuffered = e.newValue;
                onChanged?.Invoke();
			});

			Add(doubleBuffered);
		}
        
		void SetVisible(VisualElement element, bool visible)
		{
            if (element == null)
                return;

            element.style.display = visible? DisplayStyle.Flex: DisplayStyle.None;
        }

		void UpdateFieldVisibility(MixtureNode node)
		{
            var editFlags = node.rtSettings.editFlags;
            var rtSettings = node.rtSettings;
            SetVisible(outputWidthMode, rtSettings.CanEdit(EditFlags.WidthMode));
            SetVisible(outputHeightMode, rtSettings.CanEdit(EditFlags.HeightMode));
            SetVisible(outputDepthMode, rtSettings.CanEdit(EditFlags.DepthMode));
            SetVisible(outputWidth, rtSettings.CanEdit(EditFlags.Width) && node.rtSettings.widthMode == OutputSizeMode.Fixed);
            SetVisible(outputWidthPercentage, rtSettings.CanEdit(EditFlags.Width) && node.rtSettings.widthMode == OutputSizeMode.PercentageOfOutput);
			SetVisible(outputHeight, rtSettings.CanEdit(EditFlags.Height) && node.rtSettings.heightMode == OutputSizeMode.Fixed);
            SetVisible(outputHeightPercentage, rtSettings.CanEdit(EditFlags.Height) && node.rtSettings.heightMode == OutputSizeMode.PercentageOfOutput);
			SetVisible(outputDepth, rtSettings.CanEdit(EditFlags.Depth) && node.rtSettings.depthMode == OutputSizeMode.Fixed);
            SetVisible(outputDepthPercentage, rtSettings.CanEdit(EditFlags.Depth) && node.rtSettings.depthMode == OutputSizeMode.PercentageOfOutput);
            SetVisible(outputDimension, rtSettings.CanEdit(EditFlags.Dimension));
            SetVisible(outputFormat, rtSettings.CanEdit(EditFlags.TargetFormat));
        }

        public void RegisterChangedCallback(Action callback) => onChanged += callback;
    }
}