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

public class DeviceFilter
{
    private readonly CsvConstants.CsvEnums _filterKey;
    private readonly string _filterMatch;

    public DeviceFilter(CsvConstants.CsvEnums key, string match)
    {
        _filterKey = key;
        _filterMatch = match;
    }

    private static string GetDeviceRecordValue(CsvConstants.CsvEnums key, DeviceRecord record)
    {
        switch (key)
        {
            case CsvConstants.CsvEnums.Brand:
                return record.Brand;
            case CsvConstants.CsvEnums.Device:
                return record.Device;
            case CsvConstants.CsvEnums.Manufacturer:
                return record.Manufacturer;
            case CsvConstants.CsvEnums.Model:
                return record.Model;
            case CsvConstants.CsvEnums.RAM:
                return record.RAM;
            case CsvConstants.CsvEnums.FormFactor:
                return record.FormFactor;
            case CsvConstants.CsvEnums.SOC:
                return record.SOC;
            case CsvConstants.CsvEnums.GPU:
                return record.GPU;
            case CsvConstants.CsvEnums.ScreenSizes:
                return record.ScreenSizes;
            case CsvConstants.CsvEnums.ScreenDensities:
                return record.ScreenDensities;
            case CsvConstants.CsvEnums.ABIs:
                return record.ABIs;
            case CsvConstants.CsvEnums.SDKs:
                return record.SDKs;
            case CsvConstants.CsvEnums.GLES:
                return record.GLES;
            case CsvConstants.CsvEnums.InstallBase:
                return record.InstallBase;
            case CsvConstants.CsvEnums.UserANR:
                return record.UserANR;
            case CsvConstants.CsvEnums.UserCrash:
                return record.UserCrash;
            case CsvConstants.CsvEnums.Slow30:
                return record.Slow30;
            case CsvConstants.CsvEnums.Slow20:
                return record.Slow20;
        }

        return "";
    }

    public bool CheckFilterMatch(DeviceRecord record)
    {
        string recordValue = GetDeviceRecordValue(_filterKey, record);
        if (recordValue.Length > 0)
        {
            if (recordValue.Contains(_filterMatch))
            {
                return true;
            }
        }

        return false;
    }
}
