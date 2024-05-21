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
using System.Collections;
using System.Runtime.InteropServices;

namespace vkqlisteditor.editor;

public class StringTable
{
    private readonly SortedList<string, string> _stringTableSorted = new(new DeviceStringSorter());
    private readonly CaseInsensitiveComparer _caseInsensitiveComparer = new();

    public void AddStringToTable(string stringToAdd)
    {
        // Ignore matches to our hardcoded 0 index null string
        if (string.IsNullOrEmpty(stringToAdd)) return;
        if (_caseInsensitiveComparer.Compare(stringToAdd, "none") == 0) return;

        _stringTableSorted.TryAdd(stringToAdd, stringToAdd);
    }

    public int GetStringIndex(string stringToIndex)
    {
        // null/empty/"none" always match the 0 index null string
        if (string.IsNullOrEmpty(stringToIndex)) return 0;
        if (_caseInsensitiveComparer.Compare(stringToIndex, "none") == 0) return 0;

        if (_stringTableSorted.ContainsKey(stringToIndex))
        {
            // + 1 because of manual null string inserted at export time
            return _stringTableSorted.IndexOfKey(stringToIndex) + 1;
        }

        return -1;
    }

    public int CalculateStringTableSize()
    {
        // 32-bit offset entries in the string table, so 4 bytes per entry
        // Also we have a default 'null' string at index 0
        var totalTableSize = (_stringTableSorted.Count + 1) * 4;

        // Strings will be UTF8, one byte per char
        foreach (var currentString in _stringTableSorted)
        {
            totalTableSize += (currentString.Key.Length + 1); // +1 for C string null terminator
        }

        totalTableSize += 1; // null 0 index string
        return totalTableSize;
    }

    public void ExportStringTable(Span<byte> tableBuffer, uint offsetTableOffset)
    {
        var actualEntryCount = _stringTableSorted.Count + 1;
        var stringBuffer = tableBuffer.Slice(actualEntryCount * 4);
        var offsetTable = MemoryMarshal.Cast<byte, uint>(tableBuffer);
        var stringOffset = offsetTableOffset + ((uint) actualEntryCount * 4);
        var offsetIndex = 0;
        var stringIndex = 0;
        
        // Write the null string at index 0
        offsetTable[offsetIndex] = stringOffset;
        ++offsetIndex;
        ++stringOffset;
        var nullString = stringBuffer.Slice(stringIndex, 1);
        nullString[0] = 0;
        stringIndex += 1;
        
        foreach (var currentString in _stringTableSorted)
        {
            // Write the offset table entry for the next string
            offsetTable[offsetIndex] = stringOffset;
            var stringLength = currentString.Key.Length + 1;
            stringOffset += (UInt32)stringLength;
            ++offsetIndex;

            // Write out the string bytes in UTF8
            var destString = stringBuffer.Slice(stringIndex, stringLength);
            var stringBytes = System.Text.Encoding.UTF8.GetBytes(currentString.Key);
            Span<byte> srcString = stringBytes;
            srcString.CopyTo(destString);
            destString[stringLength - 1] = 0; // Terminate C string
            stringIndex += stringLength;
        }
    }

    public uint GetCount()
    {
        return (uint) _stringTableSorted.Count + 1; // +1 for inserted null 0 index string
    }
}
