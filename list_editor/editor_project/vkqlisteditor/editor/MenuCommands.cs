/*
 * Copyright 2024 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO.Compression;

namespace vkqlisteditor.editor;

using System.Text.Json;
using Terminal.Gui;

public static class MenuCommands
{
    private static bool ExportVkq(RuntimeData runtimeData)
    {
        bool success = false;
        if (runtimeData is {MainProject: not null, ProjectPath: not null})
        {
            var exportPath = Path.ChangeExtension(runtimeData.ProjectPath, "vkq");
            success = RuntimeDataExporter.Export(runtimeData, exportPath);
        }

        return success;
    }

    public static void ExportAar(RuntimeData runtimeData)
    {
        if (runtimeData is {MainProject: not null, ProjectPath: not null})
        {
            // Make sure we save the current project state
            SaveProject(runtimeData);            

            // Generate the .vkq file to copy into the .aar
            if (!ExportVkq(runtimeData)) return;

            var templateFile = Path.GetDirectoryName(runtimeData.ProjectPath);
            templateFile = $"{templateFile}{Path.DirectorySeparatorChar}aar-template.zip";
            AarTemplate.CreateAarTemplateFile(templateFile);
            using var templateStream = new FileStream(templateFile, FileMode.Open);
            using var templateArchive = new ZipArchive(templateStream, ZipArchiveMode.Read);
            
            var dataFilename = Path.ChangeExtension(Path.GetFileName(runtimeData.ProjectPath), "vkq"); 
            var exportPath = Path.ChangeExtension(runtimeData.ProjectPath, "aar");
            using var aarFile = new FileStream(exportPath, FileMode.Create);
            using var archive = new ZipArchive(aarFile, ZipArchiveMode.Create);

            var trimString = $"aar-template{Path.DirectorySeparatorChar}";
            var newRoot = Path.ChangeExtension(Path.GetFileName(runtimeData.ProjectPath), null); 
            
            // Copy the template files
            foreach (var entry in templateArchive.Entries)
            {
                if (!entry.FullName.StartsWith(trimString)) continue;
                var trimmedName = entry.FullName.Substring(trimString.Length);
                if (trimmedName.Contains(".DS_Store")) continue;
                if (string.IsNullOrEmpty(trimmedName)) continue;
                var newName = $"{newRoot}{Path.DirectorySeparatorChar}{trimmedName}";
                var copyEntry = archive.CreateEntry(newName);
                using var copyToStream = copyEntry.Open();
                var copyFromStream = entry.Open();
                copyFromStream.CopyTo(copyToStream);
            }
            
            // Copy our new asset file
            var assetPath = $"{newRoot}{Path.DirectorySeparatorChar}assets{Path.DirectorySeparatorChar}{dataFilename}";
            var exportEntry = archive.CreateEntry(assetPath);
            var vkqPath = Path.ChangeExtension(runtimeData.ProjectPath, "vkq");
            using var vkqStream = new FileStream(vkqPath, FileMode.Open);
            using var vkqReader = new StreamReader(vkqStream);
            vkqStream.CopyTo(exportEntry.Open());
            
            File.Delete(templateFile);
        }
    }
    
    private static void DoSaveChangesDialog(RuntimeData runtimeData)
    {
        if (runtimeData is {MainProject: not null, ProjectNeedsSave: true})
        {
            var savePressed = false;
            var save = new Button ("Save", is_default: true);
            save.Clicked += () => { savePressed = true; Application.RequestStop (); };
            var dontSave = new Button ("Don't Save");
            dontSave.Clicked += () => { Application.RequestStop (); };
            var d = new Dialog ("Save changes before closing?", 60, 20, save, dontSave);
            Application.Run(d);
            if (savePressed)
            {
                SaveProject(runtimeData);
            }
        }
    }
    public static void NewProject(RuntimeData runtimeData, EditorWindow? editorWindow)
    {
        DoSaveChangesDialog(runtimeData);
        
        var aTypes = new List<string> () { ".json" };
        var sd = new SaveDialog ("Save project file", "Choose a directory and filename for the project", aTypes);
        sd.DirectoryPath = runtimeData.UserPreferences.GetLastUsedPath();
        sd.FilePath = Path.Combine (runtimeData.UserPreferences.GetLastUsedPath(), "vkqualityproject.json");
        Application.Run (sd);

        var createNewProject = false;
        if (sd.Canceled || sd.FilePath == null) return;
        string? pathString = sd.FilePath.ToString();
        if (pathString == null) return;
        var pathDirectory = Path.GetDirectoryName(pathString);
        if (pathDirectory != null) runtimeData.UserPreferences.SetLastUsedPath(pathDirectory);

        if (File.Exists(pathString)) {
            if (MessageBox.Query ("Save File",
                    "File already exists. Overwrite anyway?", "No", "Ok") == 1)
            {
                createNewProject = true;
            }
        } 
        else
        {
            createNewProject = true;
        }

        if (!createNewProject) return;
        runtimeData.ProjectPath = pathString;
        runtimeData.MainProject = new DeviceListProject();
        NewProjectDefaults.SetNewProjectDefaults(runtimeData.MainProject);
        editorWindow?.UpdatePaths(runtimeData);
    }

    public static void OpenProject(RuntimeData runtimeData, EditorWindow? editorWindow)
    {
        DoSaveChangesDialog(runtimeData);
        
        var aTypes = new List<string> () { ".json" };
        var open = new OpenDialog ("Open project file", "Open a project file", aTypes) { AllowsMultipleSelection = false };
        open.DirectoryPath = runtimeData.UserPreferences.GetLastUsedPath();
        var openPath = Path.Combine (runtimeData.UserPreferences.GetLastUsedPath(), "vkqualityproject.json");
        open.FilePath = openPath;

        Application.Run (open);

        if (!open.Canceled) {
            foreach (var path in open.FilePaths) {
                if (string.IsNullOrEmpty (path) || !File.Exists (path)) {
                    continue;
                }

                var pathDirectory = Path.GetDirectoryName(path);
                if (pathDirectory != null) runtimeData.UserPreferences.SetLastUsedPath(pathDirectory);

                var jsonString = File.ReadAllText(path);
                try
                {
                    runtimeData.MainProject = JsonSerializer.Deserialize<DeviceListProject>(jsonString);
                    runtimeData.ProjectPath = path;
                    PlayDevicesCsvImporter.ImportDeviceCsvFile(runtimeData);
                }
                catch (JsonException)
                {
                    MessageBox.ErrorQuery ("Invalid JSON project", "JSON was not a valid project", "Ok");
                    return;
                }

                if (runtimeData.MainProject == null) return;
                if (runtimeData.MainProject.DeviceAllowList != null)
                {
                    runtimeData.DeviceAllowList = new List<DeviceAllowListRecord>(runtimeData.MainProject.DeviceAllowList);
                }
                if (runtimeData.MainProject.GpuPredictAllowList != null)
                {
                    runtimeData.GpuPredictAllowList = new List<GpuPredictRecord>(runtimeData.MainProject.GpuPredictAllowList);
                }
                if (runtimeData.MainProject.GpuPredictDenyList != null)
                {
                    runtimeData.GpuPredictDenyList = new List<GpuPredictRecord>(runtimeData.MainProject.GpuPredictDenyList);
                }
                editorWindow?.UpdatePaths(runtimeData);
            }
        }
    }

    public static void SaveProject(RuntimeData runtimeData)
    {
        if (runtimeData.MainProject != null)
        {
            runtimeData.MainProject.DeviceAllowList = runtimeData.DeviceAllowList.ToArray();
            runtimeData.MainProject.GpuPredictAllowList = runtimeData.GpuPredictAllowList.ToArray();
            runtimeData.MainProject.GpuPredictDenyList = runtimeData.GpuPredictDenyList.ToArray();
            var jsonString = JsonSerializer.Serialize(runtimeData.MainProject);
            File.WriteAllText(runtimeData.ProjectPath, jsonString);
        }
        runtimeData.ProjectNeedsSave = false;
    }

    private static string? SelectCsvFile(RuntimeData runtimeData, EditorWindow? editorWindow, string message)
    {
        var aTypes = new List<string>() {".csv"};
        var open = new OpenDialog("Open CSV file", message, aTypes) {AllowsMultipleSelection = false};
        open.DirectoryPath = runtimeData.UserPreferences.GetLastUsedPath();

        Application.Run(open);

        if (!open.Canceled)
        {
            foreach (var path in open.FilePaths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                if (File.Exists(path)) return path;
            }
        }

        return null;
    }

    public static void ImportDeviceListCsv(RuntimeData runtimeData, EditorWindow? editorWindow)
    {
        var csvPath = SelectCsvFile(runtimeData, editorWindow, "Open a device list CSV file");
        if (string.IsNullOrEmpty(csvPath)) return;
        DeviceListCsvImporter.ImportDeviceListCsvFile(runtimeData, csvPath);
    }

    public static void ImportGpuAllowListCsv(RuntimeData runtimeData, EditorWindow? editorWindow)
    {
        var csvPath = SelectCsvFile(runtimeData, editorWindow, "Open a GPU allow list CSV file");
        if (string.IsNullOrEmpty(csvPath)) return;
        GpuListCsvImporter.ImportGpuListCsvFile(runtimeData, csvPath, true);
    }

    public static void ImportGpuDenyListCsv(RuntimeData runtimeData, EditorWindow? editorWindow)
    {
        var csvPath = SelectCsvFile(runtimeData, editorWindow, "Open a GPU deny list CSV file");
        if (string.IsNullOrEmpty(csvPath)) return;
        GpuListCsvImporter.ImportGpuListCsvFile(runtimeData, csvPath, false);
    }
}
