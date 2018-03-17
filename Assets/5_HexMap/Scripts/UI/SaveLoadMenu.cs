using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    public Text MenuLabel, ActionButtonLabel;
    public InputField NameInput;
    public RectTransform ListContent;
    public SaveLoadItem ItemPrefab;
    public HexGrid HexGrid;

    private const int MapFileVersion = 5;

    private bool _saveMode;

    public void Open(bool saveMode)
    {
        _saveMode = saveMode;
        if (saveMode)
        {
            MenuLabel.text = "Save Map";
            ActionButtonLabel.text = "Save";
        }
        else
        {
            MenuLabel.text = "Load Map";
            ActionButtonLabel.text = "Load";
        }

        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void Delete()
    {
        var path = GetSelectedPath();
        if (path == null)
        {
            return;
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        NameInput.text = "";
        FillList();
    }

    public void Action()
    {
        var path = GetSelectedPath();
        if (path == null)
        {
            return;
        }

        if (_saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }

        Close();
    }

    public void SelectItem(string name)
    {
        NameInput.text = name;
    }

    private void Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }

        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            var header = reader.ReadInt32();
            if (header <= MapFileVersion)
            {
                HexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }

    private void Save(string path)
    {
        using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(MapFileVersion);
            HexGrid.Save(writer);
        }
    }

    private string GetSelectedPath()
    {
        var mapName = NameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    private void FillList()
    {
        for (var i = 0; i < ListContent.childCount; i++)
        {
            Destroy(ListContent.GetChild(i).gameObject);
        }

        var paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for (var i = 0; i < paths.Length; i++)
        {
            var item = Instantiate(ItemPrefab);
            item.Menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(ListContent, false);
        }
    }
}