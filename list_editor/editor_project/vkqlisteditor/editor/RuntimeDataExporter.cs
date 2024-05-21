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

public static class RuntimeDataExporter
{
    private const int FileHeaderSizeBytes = (14 * 4);
    private const int ShortcutTableSizeBytes = (27 * 4);
    private const uint FileIdentifier = 0x564b5141;
    private const uint FileFormatVersion = 0x010000;
    private const uint MinimumLibraryVersion = 0x010000;

    private struct RuntimeFileSizes
    {
        public int HeaderSize = 0;
        public int DeviceListSize = 0;
        public int GpuAllowListSize = 0;
        public int GpuDenyListSize = 0;
        public int ShortcutListSize = 0;
        public int StringTableSize = 0;
        public int TotalSize = 0;

        public RuntimeFileSizes()
        {
        }
    }
    public static bool Export(RuntimeData runtimeData, string exportPath)
    {
        if (runtimeData.MainProject == null) return false;
        if (runtimeData.DeviceAllowList.Count == 0) return false;

        var stringTable = GenerateStringTable(runtimeData);
        var deviceTable = GenerateDeviceListExport(runtimeData);
        var gpuAllowTable = GenerateGpuListExport(runtimeData.GpuPredictAllowList);
        var gpuDenyTable = GenerateGpuListExport(runtimeData.GpuPredictDenyList);

        RuntimeFileSizes fileSizes = default;
        CalculateFileSizes(deviceTable, gpuAllowTable, gpuDenyTable, stringTable, ref fileSizes);

        uint gpuAllowListOffset = 0;
        uint gpuDenyListOffset = 0;

        byte[] fileBuffer = new byte[fileSizes.TotalSize];
        Span<byte> fileSpan = fileBuffer;

        var currentBufferOffset = FileHeaderSizeBytes;
        var headerSpan = fileSpan.Slice(0, currentBufferOffset);
        var fileHeader = MemoryMarshal.Cast<byte, uint>(headerSpan);

        var stringTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.StringTableSize);
        stringTable.ExportStringTable(stringTableSpan, FileHeaderSizeBytes);
        var stringTableOffset = (uint) currentBufferOffset;
        currentBufferOffset += fileSizes.StringTableSize;

        var deviceTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.DeviceListSize);
        deviceTable.ExportDeviceTable(deviceTableSpan, stringTable);
        var deviceListOffset = (uint) currentBufferOffset;
        currentBufferOffset += fileSizes.DeviceListSize;

        // TODO: this is currently initialized to 0 (start index), is a future optimization
        var deviceListShortcutsOffset = (uint) currentBufferOffset;
        currentBufferOffset += fileSizes.ShortcutListSize;

        if (gpuAllowTable.GetCount() > 0)
        {
            var gpuAllowTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.GpuAllowListSize);
            gpuAllowTable.ExportDeviceTable(gpuAllowTableSpan, stringTable);
            gpuAllowListOffset = (uint) currentBufferOffset;
            currentBufferOffset += fileSizes.GpuAllowListSize;
        }

        if (gpuDenyTable.GetCount() > 0)
        {
            var gpuDenyTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.GpuDenyListSize);
            gpuDenyTable.ExportDeviceTable(gpuDenyTableSpan, stringTable);
            gpuDenyListOffset = (uint) currentBufferOffset;
        }

        // Populate header information
        // vkquality_file_format.h - VkQualityFileHeader
        fileHeader[0] = FileIdentifier; // file_identifier
        fileHeader[1] = FileFormatVersion; // file_format_version
        fileHeader[2] = MinimumLibraryVersion; // library_minimum_version
        fileHeader[3] = (uint) runtimeData.MainProject.ExportedListFileVersion; // list_version
        fileHeader[4] = (uint) runtimeData.MainProject.MinApiForFutureRecommendation; // min_future_vulkan_recommendation_api
        fileHeader[5] = deviceTable.GetCount(); // device_list_count
        fileHeader[6] = gpuAllowTable.GetCount(); // gpu_allow_predict_count
        fileHeader[7] = gpuDenyTable.GetCount(); // gpu_deny_predict_count
        fileHeader[8] = stringTable.GetCount(); // string_table_count
        fileHeader[9] = stringTableOffset; // string_table_offset
        fileHeader[10] = deviceListOffset; // device_list_offset
        fileHeader[11] = deviceListShortcutsOffset; // device_list_shortcuts_offset
        fileHeader[12] = gpuAllowListOffset; // gpu_allow_predict_offset
        fileHeader[13] = gpuDenyListOffset; // gpu_deny_predict_offset
        
        using var exportStream = new FileStream(exportPath, FileMode.Create);
        exportStream.Write(fileBuffer);

        return true;
    }

    private static void CalculateFileSizes(DeviceListExport deviceTable,
        GpuListExport gpuAllowTable, GpuListExport gpuDenyTable, StringTable stringTable,
        ref RuntimeFileSizes fileSizes)
    {
        fileSizes.HeaderSize = FileHeaderSizeBytes;
        fileSizes.DeviceListSize = deviceTable.CalculateDeviceTableSize();
        fileSizes.GpuAllowListSize = gpuAllowTable.CalculateGpuTableSize();
        fileSizes.GpuDenyListSize = gpuDenyTable.CalculateGpuTableSize();
        fileSizes.ShortcutListSize = ShortcutTableSizeBytes;
        fileSizes.StringTableSize = stringTable.CalculateStringTableSize();
        fileSizes.TotalSize = fileSizes.HeaderSize + fileSizes.DeviceListSize + fileSizes.GpuAllowListSize
                              + fileSizes.GpuDenyListSize + fileSizes.ShortcutListSize 
                              + fileSizes.StringTableSize;
    }

    private static StringTable GenerateStringTable(RuntimeData runtimeData)
    {
        StringTable stringTable = new();

        foreach (var deviceEntry in runtimeData.DeviceAllowList)
        {
            stringTable.AddStringToTable(deviceEntry.Brand);
            stringTable.AddStringToTable(deviceEntry.Device);
        }

        foreach (var gpuAllowEntry in runtimeData.GpuPredictAllowList)
        {
            stringTable.AddStringToTable(gpuAllowEntry.DeviceName);
        }

        foreach (var gpuDenyEntry in runtimeData.GpuPredictDenyList)
        {
            stringTable.AddStringToTable(gpuDenyEntry.DeviceName);
        }

        return stringTable;
    }

    private static DeviceListExport GenerateDeviceListExport(RuntimeData runtimeData)
    {
        DeviceListExport deviceListExport = new();

        foreach (var deviceEntry in runtimeData.DeviceAllowList)
        {
            deviceListExport.AddDeviceToTable(deviceEntry);
        }

        return deviceListExport;
    }

    private static GpuListExport GenerateGpuListExport(List<GpuPredictRecord> gpuList)
    {
        GpuListExport gpuListExport = new();

        foreach (var gpuEntry in gpuList)
        {
            gpuListExport.AddDeviceToTable(gpuEntry);
        }
        return gpuListExport;
    }
}
