// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    [FilePathAttribute("Library/TerrainTools/Stamp", FilePathAttribute.Location.ProjectFolder)]
    public class StampTool : TerrainPaintTool<StampTool>
    {
        [SerializeField]
        private float m_StampHeightWorldSpace = 0.0f;

        [SerializeField]
        private float m_MaxBlendAdd = 0.0f;

        public override string GetName()
        {
            return "Stamp Terrain";
        }

        public override string GetDesc()
        {
            return "Left click to stamp the brush onto the terrain.\n\nHold control and mousewheel to adjust height.";
        }

        private void ApplyBrushInternal(PaintContext paintContext, float brushStrength, Texture brushTexture, BrushTransform brushXform, Terrain terrain, bool negate)
        {
            Material mat = TerrainPaintUtility.GetBuiltinPaintMaterial();

            Vector4 brushParams = new Vector4(brushStrength, 0.0f, m_StampHeightWorldSpace / terrain.terrainData.size.y, m_MaxBlendAdd);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);

            TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)TerrainPaintUtility.BuiltinPaintMaterialPasses.StampHeight);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            // ignore mouse drags
            if (Event.current.type == EventType.MouseDrag)
                return true;

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.uv, editContext.brushSize, 0.0f);
            PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());
            ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain, Event.current.control);
            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Stamp");
            return true;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            Event evt = Event.current;
            if (evt.control && (evt.type == EventType.ScrollWheel))
            {
                m_StampHeightWorldSpace += Event.current.delta.y * (-0.0004f) * editContext.raycastHit.distance;
                evt.Use();
                editContext.Repaint();
            }

            // We're only doing painting operations, early out if it's not a repaint
            if (evt.type != EventType.Repaint)
                return;

            if (editContext.hitValidTerrain)
            {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, editContext.raycastHit.textureCoord, editContext.brushSize, 0.0f);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

                Material material = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

                TerrainPaintUtilityEditor.DrawBrushPreview(
                    paintContext, TerrainPaintUtilityEditor.BrushPreview.SourceRenderTexture, editContext.brushTexture, brushXform, material, 0);

                // draw result preview
                {
                    ApplyBrushInternal(paintContext, editContext.brushStrength, editContext.brushTexture, brushXform, terrain, evt.control);

                    // restore old render target
                    RenderTexture.active = paintContext.oldRenderTexture;

                    material.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

                    TerrainPaintUtilityEditor.DrawBrushPreview(
                        paintContext, TerrainPaintUtilityEditor.BrushPreview.DestinationRenderTexture, editContext.brushTexture, brushXform, material, 1);
                }

                TerrainPaintUtility.ReleaseContextResources(paintContext);
            }
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            EditorGUI.BeginChangeCheck();
            m_StampHeightWorldSpace = EditorGUILayout.Slider(new GUIContent("Stamp Height", "You can set the Stamp Height manually or you can hold control and mouse wheel on the terrain to adjust it."), m_StampHeightWorldSpace, 0, terrain.terrainData.size.y);
            m_MaxBlendAdd = EditorGUILayout.Slider(new GUIContent("Max <--> Add", "Blend between adding the heights, and taking the maximum."), m_MaxBlendAdd, 0.0f, 1.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);

            // show built-in brushes
            editContext.ShowBrushesGUI(5);
            base.OnInspectorGUI(terrain, editContext);
        }
    }
}
