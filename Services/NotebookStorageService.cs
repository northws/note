using System.Text.Json;
using HandwrittenNotes.Models;
using Windows.Storage;

namespace HandwrittenNotes.Services;

/// <summary>
/// 笔记本数据持久化服务
/// </summary>
public class NotebookStorageService
{
    private static readonly string StorageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HandwrittenNotes", "Notebooks");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotebookStorageService()
    {
        Directory.CreateDirectory(StorageFolder);
    }

    /// <summary>
    /// 保存笔记本
    /// </summary>
    public async Task SaveNotebookAsync(Notebook notebook)
    {
        notebook.ModifiedAt = DateTime.Now;
        var json = JsonSerializer.Serialize(notebook, JsonOptions);
        var filePath = GetFilePath(notebook.Id);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 加载笔记本
    /// </summary>
    public async Task<Notebook?> LoadNotebookAsync(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath)) return null;
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Notebook>(json, JsonOptions);
    }

    /// <summary>
    /// 加载所有笔记本元数据（用于列表显示）
    /// </summary>
    public async Task<List<Notebook>> LoadAllNotebooksAsync()
    {
        var notebooks = new List<Notebook>();
        if (!Directory.Exists(StorageFolder)) return notebooks;

        foreach (var file in Directory.GetFiles(StorageFolder, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var notebook = JsonSerializer.Deserialize<Notebook>(json, JsonOptions);
                if (notebook != null)
                {
                    notebooks.Add(notebook);
                }
            }
            catch
            {
                // 跳过损坏的文件
            }
        }

        return notebooks.OrderByDescending(n => n.ModifiedAt).ToList();
    }

    /// <summary>
    /// 删除笔记本
    /// </summary>
    public void DeleteNotebook(string id)
    {
        var filePath = GetFilePath(id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string GetFilePath(string id) =>
        Path.Combine(StorageFolder, $"{id}.json");
}
