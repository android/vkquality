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

public class DeviceListExport
{
    private readonly SortedList<string, DeviceAllowListRecord> _deviceTableSorted = new(new DeviceStringSorter());

    public void AddDeviceToTable(DeviceAllowListRecord device)
    {
        // Use Brand+Device for a key
        var key = $"{device.Brand},{device.Device}";
        _deviceTableSorted.TryAdd(key, device);
    }

    public int GetDeviceIndex(DeviceAllowListRecord device)
    {
        // Use Brand+Device for a key
        var key = $"{device.Brand},{device.Device}";
        if (_deviceTableSorted.ContainsKey(key))
        {
            return _deviceTableSorted.IndexOfKey(key);
        }

        return -1;
    }

    public int CalculateDeviceTableSize()
    {
        // vkquality_file_format.h VkQualityDeviceAllowListEntry
        // Currently 4 x uint32
        return _deviceTableSorted.Count * 16;
    }

    public int ExportDeviceTable(Span<byte> tableBuffer, StringTable stringTable)
    {
        int exportDeviceCount = 0;
        var deviceTable = MemoryMarshal.Cast<byte, uint>(tableBuffer);
        int spanOffset = 0;
        foreach (var currentDevice in _deviceTableSorted)
        {
            int brandIndex = stringTable.GetStringIndex(currentDevice.Value.Brand);
            int deviceIndex = stringTable.GetStringIndex(currentDevice.Value.Device);
            if (brandIndex >= 0 && deviceIndex >= 0)
            {
                deviceTable[spanOffset] = (uint) brandIndex;
                deviceTable[spanOffset + 1] = (uint) deviceIndex;
                deviceTable[spanOffset + 2] = (uint) currentDevice.Value.MinApi;
                deviceTable[spanOffset + 3] = currentDevice.Value.DriverVersion;
                spanOffset += 4;
                ++exportDeviceCount;
            }
        }

        return exportDeviceCount;
    }

    public uint GetCount()
    {
        return (uint) _deviceTableSorted.Count;
    }
}
