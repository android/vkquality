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

namespace vkqlisteditor.editor;

// We want to sort the string table and device list (sorted by brand)
// A-Z (insensitive)
// 0-9
// Other chars
public class DeviceStringSorter : IComparer<string>
{
    private readonly CaseInsensitiveComparer _caseInsensitiveComparer = new();

    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) throw new NotImplementedException();
        if (x.Length == 0 || y.Length == 0) throw new NotImplementedException();
        var lowerX = x[0];
        var lowerY = y[0];
        if (!char.IsLower(lowerX)) lowerX = Char.ToLower(lowerX);
        if (!char.IsLower(lowerY)) lowerY = Char.ToLower(lowerY);
        if (lowerX is >= 'a' and <= 'z')
        {
            if (lowerY is >= 'a' and <= 'z') return _caseInsensitiveComparer.Compare(x, y);
            else return -1;
        }
        else if (lowerX is >= '0' and <= '9')
        {
            if (lowerY is >= 'a' and <= 'z') return 1;
            else if (lowerY is >= '0' and <= '9') return _caseInsensitiveComparer.Compare(x, y);
            else return -1;
        }
        else
        {
            if (lowerY is >= 'a' and <= 'z' or >= '0' and <= '9') return 1;
            else return _caseInsensitiveComparer.Compare(x, y);
        }
    }
}
