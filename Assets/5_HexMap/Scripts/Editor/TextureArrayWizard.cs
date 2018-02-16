using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard
{
    public Texture2D[] Textures;

    [MenuItem("Assets/Create/Texture Array")]
    private static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<TextureArrayWizard>(
            "Create Texture Array", "Create"
        );
    }

    private void OnWizardCreate()
    {
        if (Textures.Length == 0)
        {
            return;
        }

        var path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "Texture Array", "asset",
            "Save Texture Array");
        if (path.Length == 0)
        {
            return;
        }

        var t = Textures[0];
        var textureArray = new Texture2DArray(t.width, t.height, Textures.Length, t.format, t.mipmapCount > 1);
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        for (int i = 0; i < Textures.Length; i++)
        {
            for (int m = 0; m < t.mipmapCount; m++)
            {
                Graphics.CopyTexture(Textures[i], 0, m, textureArray, i, m);
            }
        }

        AssetDatabase.CreateAsset(textureArray, path);
    }
}