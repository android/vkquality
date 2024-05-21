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
using System.Runtime.InteropServices;

namespace vkqlisteditor.editor;

public class GpuListExport
{
    private readonly SortedList<string, GpuPredictRecord> _gpuTableSorted = new(new DeviceStringSorter());

    public void AddDeviceToTable(GpuPredictRecord device)
    {
        _gpuTableSorted.TryAdd(device.DeviceName, device);
    }

    public int GetDeviceIndex(GpuPredictRecord device)
    {
        if (_gpuTableSorted.ContainsKey(device.DeviceName))
        {
            return _gpuTableSorted.IndexOfKey(device.DeviceName);
        }

        return -1;
    }

    public int CalculateGpuTableSize()
    {
        // vkquality_file_format.h VkQualityGpuPredictEntry
        // Currently 5 x uint32
        return _gpuTableSorted.Count * 20;
    }

    public int ExportDeviceTable(Span<byte> tableBuffer, StringTable stringTable)
    {
        int exportDeviceCount = 0;
        var deviceTable = MemoryMarshal.Cast<byte, uint>(tableBuffer);
        int spanOffset = 0;
        foreach (var currentGpu in _gpuTableSorted)
        {
            var nameIndex = stringTable.GetStringIndex(currentGpu.Value.DeviceName);
            if (nameIndex < 0) continue;
            deviceTable[spanOffset] = (uint) nameIndex;
            deviceTable[spanOffset + 1] = (uint) currentGpu.Value.MinApi;
            deviceTable[spanOffset + 2] = currentGpu.Value.DeviceId;
            deviceTable[spanOffset + 3] = currentGpu.Value.VendorId;
            deviceTable[spanOffset + 4] = currentGpu.Value.DriverVersion;
            spanOffset += 5;
            ++exportDeviceCount;
        }

        return exportDeviceCount;
    }

    public uint GetCount()
    {
        return (uint) _gpuTableSorted.Count;
    }
}
