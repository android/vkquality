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
using System.Net.Mime;
using System.Text;

namespace vkqlisteditor.editor;

using Terminal.Gui;

public class EditorGuiElements
{
    public EditorGuiElements(Toplevel topLevel)
    {
        EditorTopLevel = topLevel;
    }
    public Toplevel EditorTopLevel { get; private set; }
    public MenuBar? EditorMenuBar { get; private set; }
    public EditorWindow? EditorMainWindow { get; private set; }

    public void CreateMenuBar(RuntimeData runtimeData)
    {
        var aboutMessage = CreateAboutText();
        
        EditorMenuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", CreateFileMenuItems(runtimeData)),
            new MenuBarItem("_Import", CreateImportMenuItems(runtimeData)),
            new MenuBarItem("_Help", new MenuItem[]
            {
                new MenuItem("_About...", "About VkQualityListEditor", () => MessageBox.Query("About vkQuality Editor", aboutMessage, "_Ok"), null, null, 0)
            })
        });
        EditorTopLevel.Add(EditorMenuBar);
    }

    public void CreateEditorWindow(RuntimeData runtimeData)
    {
        int margin = 3;
        EditorMainWindow = new EditorWindow(runtimeData)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - margin,
            Height = Dim.Fill() - margin
        };
        EditorTopLevel.Add(EditorMainWindow);
    }

    private List<MenuItem []> CreateFileMenuItems(RuntimeData runtimeData)
    {
        List<MenuItem []> menuItems = new List<MenuItem []> ();
        menuItems.Add (CreateProjectMenuItems(runtimeData));
        menuItems.Add (new MenuItem [] { null });
        menuItems.Add (CreateExportMenuItems(runtimeData));
        menuItems.Add (new MenuItem [] { null });
        menuItems.Add (CreateQuitMenuItems(runtimeData));
        return menuItems;
    }
    
    private MenuItem [] CreateProjectMenuItems(RuntimeData runtimeData)
    {
        List<MenuItem> menuItems = new List<MenuItem> ();
        menuItems.Add(new MenuItem("_New Project", "", () => MenuCommands.NewProject(runtimeData, EditorMainWindow),
            null, null, Key.N | Key.CtrlMask));
        menuItems.Add(new MenuItem("_Open Project", "", () => MenuCommands.OpenProject(runtimeData, EditorMainWindow), null, null, Key.O | Key.CtrlMask));
        menuItems.Add(new MenuItem("_Save Project", "", () => MenuCommands.SaveProject(runtimeData), null, null, Key.S | Key.CtrlMask));
        return menuItems.ToArray ();
    }

    private MenuItem [] CreateExportMenuItems (RuntimeData runtimeData)
    {
        List<MenuItem> menuItems = new List<MenuItem> ();
        menuItems.Add(new MenuItem("_Export .AAR Library", "", () => MenuCommands.ExportAar(runtimeData), null, null,
            Key.E | Key.CtrlMask));
        return menuItems.ToArray ();
    }

    private MenuItem [] CreateQuitMenuItems (RuntimeData runtimeData)
    {
        List<MenuItem> menuItems = new List<MenuItem> ();
        menuItems.Add(new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask));
        return menuItems.ToArray ();
    }

    private List<MenuItem[]> CreateImportMenuItems(RuntimeData runtimeData)
    {
        List<MenuItem []> menuItems = new List<MenuItem []> ();
        menuItems.Add (CreateImportCsvMenuItems(runtimeData));
        return menuItems;
    }

    private MenuItem [] CreateImportCsvMenuItems(RuntimeData runtimeData)
    {
        List<MenuItem> menuItems = new List<MenuItem> ();
        menuItems.Add(new MenuItem("_Import Device List CSV", "", () => MenuCommands.ImportDeviceListCsv(runtimeData, EditorMainWindow),
            null, null, Key.D1 | Key.CtrlMask));
        menuItems.Add(new MenuItem("_Import GPU Allow CSV", "", () => MenuCommands.ImportGpuAllowListCsv(runtimeData, EditorMainWindow), null, null, Key.D2 | Key.CtrlMask));
        menuItems.Add(new MenuItem("_Import GPU Deny CSV", "", () => MenuCommands.ImportGpuDenyListCsv(runtimeData, EditorMainWindow), null, null, Key.D3 | Key.CtrlMask));
        return menuItems.ToArray ();
    }
    
    private static string CreateAboutText()
    {
        var guiVersion = FileVersionInfo.GetVersionInfo(typeof(Terminal.Gui.Application).Assembly.Location)
            .ProductVersion;
        var delimiterIndex = guiVersion.IndexOf('+');
        if (delimiterIndex >= 0)
        {
            guiVersion = guiVersion.Substring(0, delimiterIndex);
        }
        
        var aboutMessage = new StringBuilder();
        aboutMessage.AppendLine("VkQuality Device List Editor");
        aboutMessage.AppendLine("-------------------------------------");
        aboutMessage.AppendLine("A tool for creating and customizing  ");
        aboutMessage.AppendLine("device and GPU allow lists for the   ");
        aboutMessage.AppendLine("VkQuality Android library.           ");
        aboutMessage.AppendLine("");
        aboutMessage.AppendLine("Copyright (c) 2024 Google LLC");
        aboutMessage.AppendLine($"Version: {typeof(Program).Assembly.GetName().Version}");
        aboutMessage.AppendLine($"Using Terminal.Gui Version: {guiVersion}");
        aboutMessage.AppendLine("");
        return aboutMessage.ToString();
    }
}
