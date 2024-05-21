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

public class RuntimeData
{
    public RuntimeData()
    {
        DeviceAllowList = new List<DeviceAllowListRecord>();
        GpuPredictAllowList = new List<GpuPredictRecord>();
        GpuPredictDenyList = new List<GpuPredictRecord>();
        DeviceAllowListFilter = new List<int>();
        DeviceAllowListFilter.Add(-1);
        GpuPredictAllowListFilter = new List<int>();
        GpuPredictAllowListFilter.Add(-1);
        GpuPredictDenyListFilter = new List<int>();
        GpuPredictDenyListFilter.Add(-1);
        ProjectPath = "";
        MergeProjectPath = "";
        UserPreferences = new UserPreferences();
    }

    public Dictionary<string, DeviceRecord>? PlayDeviceList { get; set; }
    public DeviceListProject? MainProject { get; set; }
    public DeviceListProject? MergeProject { get; set; }
    public string ProjectPath { get; set; }
    public string MergeProjectPath { get; set; }

    public List<DeviceAllowListRecord> DeviceAllowList { get; set; }
    public List<GpuPredictRecord> GpuPredictAllowList { get; set; }
    public List<GpuPredictRecord> GpuPredictDenyList { get; set; }
    
    public List<int> DeviceAllowListFilter { get; private set; }
    public List<int> GpuPredictAllowListFilter { get; private set; }
    public List<int> GpuPredictDenyListFilter { get; private set; }
    
    public bool ProjectNeedsSave { get; set; }
    
    public UserPreferences UserPreferences { get; private set; }
}
