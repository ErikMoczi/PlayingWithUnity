using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
    private Texture2D _cellTexture;
    private Color32[] _cellTextureData;

    private void LateUpdate()
    {
        _cellTexture.SetPixels32(_cellTextureData);
        _cellTexture.Apply();
        enabled = false;
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
            _cellTexture.wrapMode = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", _cellTexture);
        }

        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (_cellTextureData == null || _cellTextureData.Length != x * z)
        {
            _cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < _cellTextureData.Length; i++)
            {
                _cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        enabled = true;
    }

    public void RefreshTerrain(HexCell cell)
    {
        _cellTextureData[cell.Index].a = (byte) cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell cell)
    {
        _cellTextureData[cell.Index].r = cell.IsVisible ? (byte) 255 : (byte) 0;
        enabled = true;
    }
}