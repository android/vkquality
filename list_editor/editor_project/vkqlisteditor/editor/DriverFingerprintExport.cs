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

public class DriverFingerprintExport
{
    private record FingerprintRecords
    {
        public HashSet<string> fingerprints = new();
    }

    private readonly Dictionary<string, FingerprintRecords> _driverDictionary = new();
    private readonly SortedList<string, List<string>> _driverTableSorted = new();
    private int _socTableSize;
    private int _fingerprintTableSize;

    public void AddDriverFingerprint(string soc, string fingerprint)
    {
        var socRecord = _driverDictionary.TryGetValue(soc, out var fingerprintRecord);
        if (socRecord && fingerprintRecord != null)
        {
            fingerprintRecord.fingerprints.Add(fingerprint);
        }
        else
        {
            fingerprintRecord = new FingerprintRecords();
            fingerprintRecord.fingerprints.Add(fingerprint);
            _driverDictionary.Add(soc, fingerprintRecord);
        }
    }

    public void GenerateLists()
    {
        foreach (var driverRecord in _driverDictionary)
        {
            List<string> fingerprints = new();
            foreach (var fingerprintEntry in driverRecord.Value.fingerprints)
            {
                fingerprints.Add(fingerprintEntry);
            }
            fingerprints.Sort();
            
            _driverTableSorted.Add(driverRecord.Key, fingerprints);
        }

        CalculateFingerprintSize();
        CalculateSocTableSize();
    }

    public uint GetFingerprintTableCount()
    {
        return (uint)(_fingerprintTableSize / 4);
    }

    public int GetFingerprintTableSize()
    {
        return _fingerprintTableSize;
    }
    
    public uint GetSocTableCount()
    {
        return (uint)_driverTableSorted.Count;
    }

    public int GetSocTableSize()
    {
        return _socTableSize;
    }
    
    private void CalculateSocTableSize()
    {
        // vkquality_file_format.h VkQualityDriverSoCEntry
        // Currently 3 x uint32
        _socTableSize = _driverTableSorted.Count * 12;
    }

    private void CalculateFingerprintSize()
    {
        // vkquality_file_format.h VkQualityDriverFingerprintEntry
        // Fingerprint table is per-SoC. Fingerprint string ids could
        // be present across multiple SoC fingerprint lists.
        // size is currently just 1 x uint32
        int totalSize = 0;
        foreach (var tableEntry in _driverTableSorted)
        {
            totalSize += tableEntry.Value.Count;
        }
        _fingerprintTableSize = totalSize * 4;
    }

    public int ExportSocTable(Span<byte> tableBuffer, StringTable stringTable)
    {
        int exportSoCCount = 0;
        var socTable = MemoryMarshal.Cast<byte, uint>(tableBuffer);
        int fingerprintTableOffset = 0;
        int spanOffset = 0;

        foreach (var tableEntry in _driverTableSorted)
        {
            int socIndex = stringTable.GetStringIndex(tableEntry.Key);
            if (socIndex > 0)
            {
                int fingerprintCount = tableEntry.Value.Count;
                socTable[spanOffset] = (uint)fingerprintCount;
                socTable[spanOffset + 1] = (uint)fingerprintTableOffset;
                socTable[spanOffset + 2] = (uint)socIndex;
                spanOffset += 3;
                ++exportSoCCount;
                // This is an index offset not a byte offset
                fingerprintTableOffset += fingerprintCount;
            }
        }

        return exportSoCCount;
    }
    public int ExportFingerprintTable(Span<byte> tableBuffer, StringTable stringTable)
    {
        int exportFingerprintCount = 0;
        var fingerprintTable = MemoryMarshal.Cast<byte, uint>(tableBuffer);
        int spanOffset = 0;
        foreach (var tableEntry in _driverTableSorted)
        {
            foreach (var fingerprintEntry in tableEntry.Value)
            {
                int fingerprintIndex = stringTable.GetStringIndex(fingerprintEntry);
                if (fingerprintIndex > 0)
                {
                    fingerprintTable[spanOffset] = (uint)fingerprintIndex;
                    spanOffset += 1;
                }
                ++exportFingerprintCount;
            }
        }

        return exportFingerprintCount;
    }
}