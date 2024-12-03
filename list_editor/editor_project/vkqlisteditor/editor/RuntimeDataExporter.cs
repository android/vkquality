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
    private const int FileHeaderSizeBytes = (22 * 4);
    private const int ShortcutTableSizeBytes = (27 * 4);
    private const uint FileIdentifier = 0x564b5141;
    private const uint FileFormatVersion = 0x010200;
    private const uint MinimumLibraryVersion = 0x010200;

    private struct RuntimeFileSizes
    {
        public int HeaderSize = 0;
        public int DeviceListSize = 0;
        public int DriverAllowListSize = 0;
        public int DriverDenyListSize = 0;
        public int GpuAllowListSize = 0;
        public int GpuDenyListSize = 0;
        public int ShortcutListSize = 0;
        public int SocAllowListSize = 0;
        public int SocDenyListSize = 0;
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
        var driverAllowTable = GenerateDriverFingerprintExport(runtimeData.DriverAllowList);
        var driverDenyTable = GenerateDriverFingerprintExport(runtimeData.DriverDenyList);
        var gpuAllowTable = GenerateGpuListExport(runtimeData.GpuPredictAllowList);
        var gpuDenyTable = GenerateGpuListExport(runtimeData.GpuPredictDenyList);

        RuntimeFileSizes fileSizes = default;
        CalculateFileSizes(deviceTable, driverAllowTable, driverDenyTable, gpuAllowTable,
            gpuDenyTable, stringTable, ref fileSizes);

        uint gpuAllowListOffset = 0;
        uint gpuDenyListOffset = 0;
        uint driverAllowListOffset = 0;
        uint driverDenyListOffset = 0;
        uint socAllowListOffset = 0;
        uint socDenyListOffset = 0;

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
            currentBufferOffset += fileSizes.GpuDenyListSize;
        }

        if (driverAllowTable.GetFingerprintTableCount() > 0 && driverAllowTable.GetSocTableCount() > 0)
        {
            var socAllowTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.SocAllowListSize);
            driverAllowTable.ExportSocTable(socAllowTableSpan, stringTable);
            socAllowListOffset = (uint) currentBufferOffset;
            currentBufferOffset += fileSizes.SocAllowListSize;
            var driverAllowTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.DriverAllowListSize);
            driverAllowTable.ExportFingerprintTable(driverAllowTableSpan, stringTable);
            driverAllowListOffset = (uint) currentBufferOffset;
            currentBufferOffset += fileSizes.DriverAllowListSize;
        }

        if (driverDenyTable.GetFingerprintTableCount() > 0 && driverDenyTable.GetSocTableCount() > 0)
        {
            var socDenyTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.SocDenyListSize);
            driverDenyTable.ExportSocTable(socDenyTableSpan, stringTable);
            socDenyListOffset = (uint) currentBufferOffset;
            currentBufferOffset += fileSizes.SocDenyListSize;
            var driverDenyTableSpan = fileSpan.Slice(currentBufferOffset, fileSizes.DriverDenyListSize);
            driverDenyTable.ExportFingerprintTable(driverDenyTableSpan, stringTable);
            driverDenyListOffset = (uint) currentBufferOffset;
        }
        
        // Populate header information
        // vkquality_file_format.h - VkQualityFileHeader
        fileHeader[0] = FileIdentifier; // file_identifier
        fileHeader[1] = FileFormatVersion; // file_format_version
        fileHeader[2] = MinimumLibraryVersion; // library_minimum_version
        fileHeader[3] = (uint) runtimeData.MainProject.ExportedListFileVersion; // list_version
        fileHeader[4] = (uint) runtimeData.MainProject.MinApiForFutureRecommendation; // min_future_vulkan_recommendation_api
        fileHeader[5] = deviceTable.GetCount(); // device_list_count
        fileHeader[6] = driverAllowTable.GetFingerprintTableCount(); // driver_allow_count
        fileHeader[7] = driverDenyTable.GetFingerprintTableCount(); // driver_deny_count
        fileHeader[8] = gpuAllowTable.GetCount(); // gpu_allow_predict_count
        fileHeader[9] = gpuDenyTable.GetCount(); // gpu_deny_predict_count
        fileHeader[10] = driverAllowTable.GetSocTableCount(); // soc_allow_count
        fileHeader[11] = driverDenyTable.GetSocTableCount(); // soc_deny_count
        fileHeader[12] = stringTable.GetCount(); // string_table_count
        fileHeader[13] = deviceListOffset; // device_list_offset
        fileHeader[14] = deviceListShortcutsOffset; // device_list_shortcuts_offset
        fileHeader[15] = driverAllowListOffset; // driver_allow_offset
        fileHeader[16] = driverDenyListOffset; // driver_deny_offset
        fileHeader[17] = gpuAllowListOffset; // gpu_allow_predict_offset
        fileHeader[18] = gpuDenyListOffset; // gpu_deny_predict_offset
        fileHeader[19] = socAllowListOffset; // soc_allow_offset
        fileHeader[20] = socDenyListOffset; // soc_deny_offset
        fileHeader[21] = stringTableOffset; // string_table_offset
        
        using var exportStream = new FileStream(exportPath, FileMode.Create);
        exportStream.Write(fileBuffer);

        return true;
    }

    private static void CalculateFileSizes(DeviceListExport deviceTable, DriverFingerprintExport driverAllowTable,
        DriverFingerprintExport driverDenyTable,
        GpuListExport gpuAllowTable, GpuListExport gpuDenyTable, StringTable stringTable,
        ref RuntimeFileSizes fileSizes)
    {
        fileSizes.HeaderSize = FileHeaderSizeBytes;
        fileSizes.DeviceListSize = deviceTable.CalculateDeviceTableSize();
        fileSizes.DriverAllowListSize = driverAllowTable.GetFingerprintTableSize();
        fileSizes.DriverDenyListSize = driverDenyTable.GetFingerprintTableSize();
        fileSizes.GpuAllowListSize = gpuAllowTable.CalculateGpuTableSize();
        fileSizes.GpuDenyListSize = gpuDenyTable.CalculateGpuTableSize();
        fileSizes.ShortcutListSize = ShortcutTableSizeBytes;
        fileSizes.SocAllowListSize = driverAllowTable.GetSocTableSize();
        fileSizes.SocDenyListSize = driverDenyTable.GetSocTableSize();
        fileSizes.StringTableSize = stringTable.CalculateStringTableSize();
        fileSizes.TotalSize = fileSizes.HeaderSize + fileSizes.DeviceListSize + fileSizes.DriverAllowListSize
                              + fileSizes.DriverDenyListSize + fileSizes.SocAllowListSize
                              + fileSizes.SocDenyListSize + fileSizes.GpuAllowListSize
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

        foreach (var driverAllowEntry in runtimeData.DriverAllowList)
        {
            stringTable.AddStringToTable(driverAllowEntry.Soc);
            stringTable.AddStringToTable(driverAllowEntry.DriverFingerprint);
        }

        foreach (var driverDenyEntry in runtimeData.DriverDenyList)
        {
            stringTable.AddStringToTable(driverDenyEntry.Soc);
            stringTable.AddStringToTable(driverDenyEntry.DriverFingerprint);
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

    private static DriverFingerprintExport GenerateDriverFingerprintExport(
        List<DriverFingerprintRecord> fingerprintRecords)
    {
        DriverFingerprintExport driverFingerprintExport = new();

        foreach (var fingerprintRecord in fingerprintRecords)
        {
            driverFingerprintExport.AddDriverFingerprint(fingerprintRecord.Soc, fingerprintRecord.DriverFingerprint);
        }
        driverFingerprintExport.GenerateLists();
        return driverFingerprintExport;
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
