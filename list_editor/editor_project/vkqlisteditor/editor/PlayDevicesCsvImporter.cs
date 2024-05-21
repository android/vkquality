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
using System.Globalization;
using CsvHelper;

namespace vkqlisteditor.editor;

public static class PlayDevicesCsvImporter
{
    public static void ImportDeviceCsvFile(RuntimeData runtimeData)
    {
        // If a project path exists, look for a 'devices.csv' file in the same directory and
        // load it if it exists
        if (string.IsNullOrEmpty(runtimeData.ProjectPath)) return;
        var csvPath = $"{Path.GetDirectoryName(runtimeData.ProjectPath)}{Path.DirectorySeparatorChar}devices.csv";
        if (!File.Exists(csvPath)) return;
        
        var deviceDictionary = new Dictionary<string, DeviceRecord>();
        
        using (var reader = new StreamReader(csvPath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var deviceRecord = new DeviceRecord(csv.GetField(CsvConstants.Brand),
                    csv.GetField(CsvConstants.Device),
                    csv.GetField(CsvConstants.Manufacturer),
                    csv.GetField(CsvConstants.Model),
                    csv.GetField(CsvConstants.RAM),
                    csv.GetField(CsvConstants.FormFactor),
                    csv.GetField(CsvConstants.SOC),
                    csv.GetField(CsvConstants.GPU),
                    csv.GetField(CsvConstants.ScreenSizes),
                    csv.GetField(CsvConstants.ScreenDensities),
                    csv.GetField(CsvConstants.ABIs),
                    csv.GetField(CsvConstants.SDKs),
                    csv.GetField(CsvConstants.GLES),
                    csv.GetField(CsvConstants.InstallBase),
                    csv.GetField(CsvConstants.UserANR),
                    csv.GetField(CsvConstants.UserCrash),
                    csv.GetField(CsvConstants.Slow30),
                    csv.GetField(CsvConstants.Slow20));
                // Use Brand+Device for a key
                var dictionaryKey = $"{deviceRecord.Brand},{deviceRecord.Device}";
                deviceDictionary.Add(dictionaryKey, deviceRecord);
            }
        }

        runtimeData.PlayDeviceList = deviceDictionary;
    }
}

