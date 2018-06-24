﻿using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
    private const float TransitionSpeed = 255f;

    private Texture2D _cellTexture;
    private Color32[] _cellTextureData;
    private bool _needsVisibilityReset;

    private List<HexCell> _transitioningCells = new List<HexCell>();

    public bool ImmediateMode { get; set; }
    public HexGrid Grid { get; set; }

    private void LateUpdate()
    {
        if (_needsVisibilityReset)
        {
            _needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        var delta = (int) (Time.deltaTime * TransitionSpeed);
        if (delta == 0)
        {
            delta = 1;
        }

        for (var i = 0; i < _transitioningCells.Count; i++)
        {
            if (!UpdateCellData(_transitioningCells[i], delta))
            {
                _transitioningCells[i--] = _transitioningCells[_transitioningCells.Count - 1];
                _transitioningCells.RemoveAt(_transitioningCells.Count - 1);
            }
        }

        _cellTexture.SetPixels32(_cellTextureData);
        _cellTexture.Apply();
        enabled = _transitioningCells.Count > 0;
    }

    public void Initialize(int x, int z)
    {
        if (_cellTexture)
        {
            _cellTexture.Resize(x, z);
        }
        else
        {
            _cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
            _cellTexture.filterMode = FilterMode.Point;
            _cellTexture.wrapModeU = TextureWrapMode.Repeat;
            _cellTexture.wrapModeV = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", _cellTexture);
        }

        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (_cellTextureData == null || _cellTextureData.Length != x * z)
        {
            _cellTextureData = new Color32[x * z];
        }
        else
        {
            for (var i = 0; i < _cellTextureData.Length; i++)
            {
                _cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        _transitioningCells.Clear();
        enabled = true;
    }

    public void RefreshTerrain(HexCell cell)
    {
        _cellTextureData[cell.Index].a = (byte) cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell cell)
    {
        var index = cell.Index;
        if (ImmediateMode)
        {
            _cellTextureData[index].r = cell.IsVisible ? (byte) 255 : (byte) 0;
            _cellTextureData[index].g = cell.IsExplored ? (byte) 255 : (byte) 0;
        }
        else if (_cellTextureData[index].b != 255)
        {
            _cellTextureData[index].b = 255;
            _transitioningCells.Add(cell);
        }

        enabled = true;
    }

    public void ViewElevationChanged()
    {
        _needsVisibilityReset = true;
        enabled = true;
    }

    public void SetMapData(HexCell cell, float data)
    {
        _cellTextureData[cell.Index].b = data < 0f ? (byte) 0 : (data < 1f ? (byte) (data * 254f) : (byte) 254);
        enabled = true;
    }

    private bool UpdateCellData(HexCell cell, int delta)
    {
        var index = cell.Index;
        var data = _cellTextureData[index];
        var stillUpdating = false;

        if (cell.IsExplored && data.g < 255)
        {
            stillUpdating = true;
            var t = data.g + delta;
            data.g = t >= 255 ? (byte) 255 : (byte) t;
        }

        if (cell.IsVisible)
        {
            if (data.r < 255)
            {
                stillUpdating = true;
                var t = data.r + delta;
                data.r = t >= 255 ? (byte) 255 : (byte) t;
            }
        }
        else if (data.r > 0)
        {
            stillUpdating = true;
            var t = data.r - delta;
            data.r = t < 0 ? (byte) 0 : (byte) t;
        }

        if (!stillUpdating)
        {
            data.b = 0;
        }

        _cellTextureData[index] = data;
        return stillUpdating;
    }
}