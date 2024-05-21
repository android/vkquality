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

public static class CsvConstants
{
    public enum CsvEnums
    {
        Brand,
        Device,
        Manufacturer,
        Model,
        RAM,
        FormFactor,
        SOC,
        GPU,
        ScreenSizes,
        ScreenDensities,
        ABIs,
        SDKs,
        GLES,
        InstallBase,
        UserANR,
        UserCrash,
        Slow30,
        Slow20
    }

    public const string Brand = "Brand";
    public const string Device = "Device";
    public const string Manufacturer = "Manufacturer";
    public const string Model = "Model Name";
    public const string RAM = "RAM (TotalMem)";
    public const string FormFactor = "Form Factor";
    public const string SOC = "System on Chip";
    public const string GPU = "GPU";
    public const string ScreenSizes = "Screen Sizes";
    public const string ScreenDensities = "Screen Densities";
    public const string ABIs = "ABIs";
    public const string SDKs = "Android SDK Versions";
    public const string GLES = "OpenGL ES Versions";
    public const string InstallBase = "Install base";
    public const string UserANR = "User-perceived ANR rate";
    public const string UserCrash = "User-perceived crash rate";
    public const string Slow30 = "Slow session rate (30 FPS)";
    public const string Slow20 = "Slow session rate (20 FPS)";

    public const string MinApi = "MinApi";
    public const string MinDriver = "MinDriver";

    public const string GpuName = "GpuName";
    public const string DeviceId = "DeviceID";
    public const string VendorId = "VendorID";
}
