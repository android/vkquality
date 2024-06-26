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
using System.IO.Compression;

namespace vkqlisteditor.editor;

public class AarTemplate
{
    public static void CreateProjectTemplate(string vkqPath, string templatePath)
    {
        var currentDirectory = System.IO.Directory.GetCurrentDirectory();
        var zipPath = $"{currentDirectory}{Path.DirectorySeparatorChar}customqualityprojecttemplate.zip";

        var extractPath = Path.GetFullPath(templatePath);
        if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            extractPath += Path.DirectorySeparatorChar;
        
        using var templateStream = new FileStream(zipPath, FileMode.Open);
        using var templateArchive = new ZipArchive(templateStream, ZipArchiveMode.Read);
        
        templateArchive.ExtractToDirectory(extractPath);

        var vkqFilename = Path.GetFileName(vkqPath);
        var assetPath = $"{extractPath}customquality{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}main{Path.DirectorySeparatorChar}assets{Path.DirectorySeparatorChar}";
        var placeholderPath = $"{assetPath}vkqplaceholder.txt";
        File.Delete(placeholderPath);
        var vkqFinalPath = $"{assetPath}{vkqFilename}";
        File.Copy(vkqPath, vkqFinalPath);
    }
}
