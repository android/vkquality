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
namespace vkqlisteditor.editor;

public class UserPreferences
{
    public UserPreferences()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectoryPath = Path.Combine(appDataPath, "vkqlisteditor");
        if (!Directory.Exists(appDirectoryPath))
        {
            Directory.CreateDirectory(appDirectoryPath);
        }

        _preferencesFilePath = Path.Combine(appDirectoryPath, "vkqlisteditor.preferences");
        if (File.Exists(_preferencesFilePath))
        {
            _lastUsedPath = File.ReadAllText(_preferencesFilePath);
        }
    }
    public string GetLastUsedPath()
    {
        if (string.IsNullOrEmpty(_lastUsedPath))
        {
            return Environment.CurrentDirectory;
        }

        return _lastUsedPath;
    }
    
    public void SetLastUsedPath(string newPath)
    {
        _lastUsedPath = newPath;
        File.WriteAllText(_preferencesFilePath, _lastUsedPath);
    }

    private string _lastUsedPath = "";
    private string _preferencesFilePath;
}
