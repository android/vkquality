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
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Terminal.Gui;
using vkqlisteditor.editor;

var runtimeData = new RuntimeData();

// Passing a project file path is optional, but if one is passed it has to be
// a valid project file in JSON format
if (args.Length == 1)
{
    var projectPath = args[0];
    if (!File.Exists(projectPath))
    {
        projectPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
        if (!File.Exists(projectPath))
        {
            Console.WriteLine("Invalid path to project file");
            return;
        }
    }

    var projectString = File.ReadAllText(projectPath);
    try
    {
        runtimeData.MainProject = JsonSerializer.Deserialize<DeviceListProject>(projectString);
        runtimeData.ProjectPath = projectPath;
        PlayDevicesCsvImporter.ImportDeviceCsvFile(runtimeData);
    }
    catch (JsonException)
    {
        Console.WriteLine("Invalid json project file");
        return;
    }
}

if (Debugger.IsAttached)
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

Application.Init();

var guiElements = new EditorGuiElements(Application.Top);
guiElements.CreateMenuBar(runtimeData);
guiElements.CreateEditorWindow(runtimeData);
Application.Run(guiElements.EditorTopLevel);
Application.Shutdown();
