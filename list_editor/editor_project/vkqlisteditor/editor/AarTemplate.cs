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

public class AarTemplate
{
    public static void CreateAarTemplateFile(string aarPath)
    {
        byte[] data = Convert.FromBase64String(AarTemplateBase64);
        File.WriteAllBytes(aarPath, data);
    }

    // Base64 encoded string of an empty .aar (zip file format) library, we will inject
    // our custom data file into the assets/ directory. This is necessary because to
    // include it in the Unity plugins/android directory, it has to be built into
    // an .aar file to be merged into the final app bundle's assets/ directory.
    private const string AarTemplateBase64 = "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";
}