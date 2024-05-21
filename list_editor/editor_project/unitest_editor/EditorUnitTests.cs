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
using vkqlisteditor.editor;

namespace unitest_editor;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [Test]
    public void StringTableSortTest()
    {
        StringTable stringTable = new();

        stringTable.AddStringToTable("#blessed");
        stringTable.AddStringToTable("CatDog");
        stringTable.AddStringToTable("zabc123");
        stringTable.AddStringToTable("ABD1000");
        stringTable.AddStringToTable("8675309Abc");
        stringTable.AddStringToTable("abe3000");
        stringTable.AddStringToTable("C@gsmore");
        stringTable.AddStringToTable("Xabc123");
        stringTable.AddStringToTable("(not a llama)");
        stringTable.AddStringToTable("abaaaaa");
        stringTable.AddStringToTable("2345");

        Assert.That(stringTable.GetStringIndex(""), Is.EqualTo(0));
        Assert.That(stringTable.GetStringIndex("none"), Is.EqualTo(0));
        Assert.That(stringTable.GetStringIndex("abaaaaa"), Is.EqualTo(1));
        Assert.That(stringTable.GetStringIndex("ABD1000"), Is.EqualTo(2));
        Assert.That(stringTable.GetStringIndex("abe3000"), Is.EqualTo(3));
        Assert.That(stringTable.GetStringIndex("C@gsmore"), Is.EqualTo(4));
        Assert.That(stringTable.GetStringIndex("CatDog"), Is.EqualTo(5));
        Assert.That(stringTable.GetStringIndex("Xabc123"), Is.EqualTo(6));
        Assert.That(stringTable.GetStringIndex("zabc123"), Is.EqualTo(7));
        Assert.That(stringTable.GetStringIndex("2345"), Is.EqualTo(8));
        Assert.That(stringTable.GetStringIndex("8675309Abc"), Is.EqualTo(9));
        Assert.That(stringTable.GetStringIndex("(not a llama)"), Is.EqualTo(10));
        Assert.That(stringTable.GetStringIndex("#blessed"), Is.EqualTo(11));
    }

    [Test]
    public void StringTableSizeCalculationTest()
    {
        StringTable stringTable = new();
        
        stringTable.AddStringToTable("0123456789");
        stringTable.AddStringToTable("ABCDE");
        stringTable.AddStringToTable("ZYX");

        // offset table, plus UTF8 null terminated strings
        // internally adds a null string
        int expectedSize = (4 * 4) + 1 + 10 + 1 + 5 + 1 + 3 + 1;
        
        Assert.That(stringTable.CalculateStringTableSize(), Is.EqualTo(expectedSize));
    }

    [Test]
    public void StringTableExportTest()
    {
        StringTable stringTable = new();

        stringTable.AddStringToTable("ABCD");
        stringTable.AddStringToTable("12345");
        stringTable.AddStringToTable(".f!");

        int exportSize = stringTable.CalculateStringTableSize();
        int fakeHeaderSize = 10;
        byte[] exportBuffer = new byte[exportSize + fakeHeaderSize];
        Span<byte> exportSpan = exportBuffer;
        exportSpan = exportSpan[fakeHeaderSize..];
        stringTable.ExportStringTable(exportSpan, (uint)fakeHeaderSize);
        Span<UInt32> offsetSpan = MemoryMarshal.Cast<byte, uint>(exportSpan);
        
        Assert.That(offsetSpan[0], Is.EqualTo(26));
        Assert.That(offsetSpan[1], Is.EqualTo(27));
        Assert.That(offsetSpan[2], Is.EqualTo(32));
        Assert.That(offsetSpan[3], Is.EqualTo(38));

        Span<byte> stringSpan = exportSpan.Slice(16); // actual start of string data is only 16 bytes in

        ReadOnlySpan<byte> compareSpan = new ReadOnlySpan<byte>(new byte[]
        {
            0x00,
            0x41, 0x42, 0x43, 0x44, 0x00, 
            0x31, 0x32, 0x33, 0x34, 0x35, 0x00, 
            0x2e, 0x66, 0x21, 0x00, 
        });
        Assert.That(stringSpan.SequenceEqual(compareSpan), Is.EqualTo(true));
    }

    [Test]
    public void DeviceTableExportTest()
    {
        var brandA = "APhoneBrand";
        var deviceA = "APhoneDevice";
        var brandB = "BPhoneBrand";
        var deviceB = "BPhoneDevice";
        
        StringTable stringTable = new();
        stringTable.AddStringToTable(brandB);
        stringTable.AddStringToTable(deviceB);
        stringTable.AddStringToTable(brandA);
        stringTable.AddStringToTable(deviceA);

        DeviceAllowListRecord recordA = new(brandA, deviceA, 30, 0x1000);
        DeviceAllowListRecord recordB = new(brandB, deviceB, 31, 0x2000);
            
        DeviceListExport deviceListExport = new();
        deviceListExport.AddDeviceToTable(recordB);
        deviceListExport.AddDeviceToTable(recordA);

        int tableSize = deviceListExport.CalculateDeviceTableSize();
        Assert.That(tableSize, Is.EqualTo(32));

        byte[] exportBuffer = new byte[tableSize];
        Span<byte> exportSpan = exportBuffer;
        int exportCount = deviceListExport.ExportDeviceTable(exportSpan, stringTable);
        Assert.That(exportCount, Is.EqualTo(2));
        
        var deviceTable = MemoryMarshal.Cast<byte, uint>(exportSpan);
        Assert.That(deviceTable[0], Is.EqualTo(1));
        Assert.That(deviceTable[1], Is.EqualTo(2));
        Assert.That(deviceTable[2], Is.EqualTo(30));
        Assert.That(deviceTable[3], Is.EqualTo(0x1000));
        
        Assert.That(deviceTable[4], Is.EqualTo(3));
        Assert.That(deviceTable[5], Is.EqualTo(4));
        Assert.That(deviceTable[6], Is.EqualTo(31));
        Assert.That(deviceTable[7], Is.EqualTo(0x2000));
    }

    [Test]
    public void GpuTableExportTest()
    {
        string deviceNameA = "ZanyGpu 500"; // 1
        string deviceNameB = "^Fake Gpu 3"; // 3
        string deviceNameC = "9dfx 9000";   // 2

        StringTable stringTable = new();
        stringTable.AddStringToTable(deviceNameA);
        stringTable.AddStringToTable(deviceNameB);
        stringTable.AddStringToTable(deviceNameC);

        GpuPredictRecord gpuA = new("ZanyGpu", deviceNameA, 0x123, 0xA00, 30, 0x1000); // 0
        GpuPredictRecord gpuB = new("Fake", deviceNameB, 0xbbb, 0xB01, 31, 0x2000);   // 2
        GpuPredictRecord gpuC = new("9dfx", deviceNameC, 0xa0a0, 0xDDD, 33, 0x1010);  // 1

        GpuListExport gpuListExport = new();
        gpuListExport.AddDeviceToTable(gpuA);
        gpuListExport.AddDeviceToTable(gpuB);
        gpuListExport.AddDeviceToTable(gpuC);

        int tableSize = gpuListExport.CalculateGpuTableSize();
        Assert.That(tableSize, Is.EqualTo(60));

        byte[] exportBuffer = new byte[tableSize];
        Span<byte> exportSpan = exportBuffer;
        int exportCount = gpuListExport.ExportDeviceTable(exportSpan, stringTable);
        Assert.That(exportCount, Is.EqualTo(3));
        
        var gpuTable = MemoryMarshal.Cast<byte, uint>(exportSpan);
        Assert.That(gpuTable[0], Is.EqualTo(1));
        Assert.That(gpuTable[1], Is.EqualTo(30));
        Assert.That(gpuTable[2], Is.EqualTo(0x123));
        Assert.That(gpuTable[3], Is.EqualTo(0xA00));
        Assert.That(gpuTable[4], Is.EqualTo(0x1000));

        Assert.That(gpuTable[5], Is.EqualTo(2));
        Assert.That(gpuTable[6], Is.EqualTo(33));
        Assert.That(gpuTable[7], Is.EqualTo(0xa0a0));
        Assert.That(gpuTable[8], Is.EqualTo(0xDDD));
        Assert.That(gpuTable[9], Is.EqualTo(0x1010));

        Assert.That(gpuTable[10], Is.EqualTo(3));
        Assert.That(gpuTable[11], Is.EqualTo(31));
        Assert.That(gpuTable[12], Is.EqualTo(0xbbb));
        Assert.That(gpuTable[13], Is.EqualTo(0xB01));
        Assert.That(gpuTable[14], Is.EqualTo(0x2000));
        
    }

    [Test]
    public void DeviceListCsvImportTest()
    {
        var currentDirectory = System.IO.Directory.GetCurrentDirectory();
        var csvPath = $"{currentDirectory}{Path.DirectorySeparatorChar}testdata_devicelist.csv";
        
        Assert.That(File.Exists(csvPath), Is.EqualTo(true));

        RuntimeData runtimeData = new();
        DeviceListCsvImporter.ImportDeviceListCsvFile(runtimeData, csvPath);

        Assert.That(runtimeData.DeviceAllowList.Count, Is.EqualTo(3));

        Assert.That(runtimeData.DeviceAllowList[0].Brand, Is.EqualTo("google"));
        Assert.That(runtimeData.DeviceAllowList[0].Device, Is.EqualTo("bluejay"));
        Assert.That(runtimeData.DeviceAllowList[0].MinApi, Is.EqualTo(34));
        Assert.That(runtimeData.DeviceAllowList[0].DriverVersion, Is.EqualTo(0));
        
        Assert.That(runtimeData.DeviceAllowList[1].Brand, Is.EqualTo("google"));
        Assert.That(runtimeData.DeviceAllowList[1].Device, Is.EqualTo("raven"));
        Assert.That(runtimeData.DeviceAllowList[1].MinApi, Is.EqualTo(34));
        Assert.That(runtimeData.DeviceAllowList[1].DriverVersion, Is.EqualTo(0));
        
        Assert.That(runtimeData.DeviceAllowList[2].Brand, Is.EqualTo("fakefone"));
        Assert.That(runtimeData.DeviceAllowList[2].Device, Is.EqualTo("notafone"));
        Assert.That(runtimeData.DeviceAllowList[2].MinApi, Is.EqualTo(33));
        Assert.That(runtimeData.DeviceAllowList[2].DriverVersion, Is.EqualTo(256));
    }
    
    [Test]
    public void GpuListCsvImportTest()
    {
        var currentDirectory = System.IO.Directory.GetCurrentDirectory();
        var csvPath = $"{currentDirectory}{Path.DirectorySeparatorChar}testdata_gpulist.csv";
        
        Assert.That(File.Exists(csvPath), Is.EqualTo(true));

        RuntimeData runtimeData = new();
        GpuListCsvImporter.ImportGpuListCsvFile(runtimeData, csvPath, true);

        Assert.That(runtimeData.GpuPredictAllowList.Count, Is.EqualTo(2));

        Assert.That(runtimeData.GpuPredictAllowList[0].Brand, Is.EqualTo("9dfx"));
        Assert.That(runtimeData.GpuPredictAllowList[0].DeviceName, Is.EqualTo("doohoo 900"));
        Assert.That(runtimeData.GpuPredictAllowList[0].DeviceId, Is.EqualTo(8888));
        Assert.That(runtimeData.GpuPredictAllowList[0].VendorId, Is.EqualTo(193));
        Assert.That(runtimeData.GpuPredictAllowList[0].MinApi, Is.EqualTo(32));
        Assert.That(runtimeData.GpuPredictAllowList[0].DriverVersion, Is.EqualTo(65537));
        
        Assert.That(runtimeData.GpuPredictAllowList[1].Brand, Is.EqualTo("fakegpu"));
        Assert.That(runtimeData.GpuPredictAllowList[1].DeviceName, Is.EqualTo("pixelmaker deluxe"));
        Assert.That(runtimeData.GpuPredictAllowList[1].DeviceId, Is.EqualTo(249));
        Assert.That(runtimeData.GpuPredictAllowList[1].VendorId, Is.EqualTo(100));
        Assert.That(runtimeData.GpuPredictAllowList[1].MinApi, Is.EqualTo(33));
        Assert.That(runtimeData.GpuPredictAllowList[1].DriverVersion, Is.EqualTo(128));
    }
}
