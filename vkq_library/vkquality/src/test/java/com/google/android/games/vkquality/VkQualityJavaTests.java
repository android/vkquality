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
package com.google.android.games.vkquality;

import com.google.android.games.vkquality.SecurityPatchDate;
import com.google.android.games.vkquality.StartupMitigation;
import com.google.android.games.vkquality.StartupMitigationRecord;

import org.junit.Test;

public class VkQualityJavaTests {
    // Validate we can construct SecurityPatchDate from strings as expected
    @Test
    public void SecurityPatchDateConstructTest()
    {
        SecurityPatchDate patchDateGood = new SecurityPatchDate("2024-04-01");
        assert(patchDateGood.getYear() == 2024);
        assert(patchDateGood.getMonth() == 4);
        assert(patchDateGood.getDay() == 1);

        SecurityPatchDate patchDateBad1 = new SecurityPatchDate("cheese");
        assert(patchDateBad1.getYear() == 0);
        assert(patchDateBad1.getMonth() == 0);
        assert(patchDateBad1.getDay() == 0);

        SecurityPatchDate patchDateBad2 = new SecurityPatchDate("123-cheese-456");
        assert(patchDateBad2.getYear() == 0);
        assert(patchDateBad2.getMonth() == 0);
        assert(patchDateBad2.getDay() == 0);
    }


    // Validate we can compare SecurityPatchDates against each other  as expected
    @Test
    public void SecurityPatchDateCompareTest()
    {
        SecurityPatchDate patchDate2023_03_15 = new SecurityPatchDate("2023-04-05");
        SecurityPatchDate patchDate2023_12_01 = new SecurityPatchDate("2023-12-01");
        SecurityPatchDate patchDate2023_12_31 = new SecurityPatchDate("2023-12-31");
        SecurityPatchDate patchDate2024_03_30 = new SecurityPatchDate("2024-03-30");
        SecurityPatchDate patchDate2024_04_05 = new SecurityPatchDate("2024-04-05");
        SecurityPatchDate patchDate2024_04_06 = new SecurityPatchDate("2024-04-06");
        SecurityPatchDate patchDate2024_05_01 = new SecurityPatchDate("2024-05-01");
        SecurityPatchDate patchDate2025_03_30 = new SecurityPatchDate("2025-03-30");

        assert(patchDate2024_04_05.IsEqualOrLaterThan(patchDate2023_03_15));
        assert(patchDate2024_04_05.IsEqualOrLaterThan(patchDate2023_12_01));
        assert(patchDate2024_04_05.IsEqualOrLaterThan(patchDate2023_12_31));
        assert(patchDate2024_04_05.IsEqualOrLaterThan(patchDate2024_03_30));
        assert(patchDate2024_04_05.IsEqualOrLaterThan(patchDate2024_04_05));

        assert(!patchDate2024_04_05.IsEqualOrLaterThan(patchDate2024_04_06));
        assert(!patchDate2024_04_05.IsEqualOrLaterThan(patchDate2024_05_01));
        assert(!patchDate2024_04_05.IsEqualOrLaterThan(patchDate2025_03_30));
    }

    // Validate that we correctly identify mitigation devices and can specify
    // a Vulkan API selection
    @Test
    public void MitigateVulkanTest()
    {
        int mockApiLevel = 34;
        String mockBrand = "f0nebrand";
        String mockDevice = "f0nedevice";
        String mockSoC = "f0neSoC";
        SecurityPatchDate mockDeviceDate = new SecurityPatchDate("2024-07-01");

        SecurityPatchDate genericVulkanDate = new SecurityPatchDate("2024-06-01");

        // Date used to specify 'unreachable'/'unfixed'
        SecurityPatchDate genericUnfixedDate = new SecurityPatchDate("2099-12-31");
        int unfixedApiLevel = 99;

        // Should match, should be affected, should recommend Vulkan
        // Pass both device and SoC on affected list
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, unfixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, mockDevice, mockSoC);
            assert (recordMatch);
            boolean useVulkan = record.RecommendAffectedVulkan(mockDeviceDate);
            assert (useVulkan);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_AFFECTED);
        }

        // Should match, should be affected, should recommend Vulkan
        // Pass only device on affected list
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, unfixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, mockDevice, "");
            assert (recordMatch);
            boolean useVulkan = record.RecommendAffectedVulkan(mockDeviceDate);
            assert (useVulkan);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_AFFECTED);
        }

        // Should match, should be affected, should recommend Vulkan
        // Pass only SoC on affected list
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, unfixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, "", mockSoC);
            assert (recordMatch);
            boolean useVulkan = record.RecommendAffectedVulkan(mockDeviceDate);
            assert (useVulkan);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_AFFECTED);
        }

        // Should match, should be affected, should recommend Vulkan
        // Pass only SoC on affected list and in mitigation record
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, "",
                    mockSoC, mockApiLevel, unfixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, "", mockSoC);
            assert (recordMatch);
            boolean useVulkan = record.RecommendAffectedVulkan(mockDeviceDate);
            assert (useVulkan);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_AFFECTED);
        }
    }

    // Validate that we correctly identify mitigation devices and can specify
    // a GLES API selection
    @Test
    public void MitigateGLESTest() {
        int mockApiLevel = 34;
        String mockBrand = "f0nebrand";
        String mockDevice = "f0nedevice";
        String mockSoC = "f0neSoC";
        SecurityPatchDate mockDeviceDate = new SecurityPatchDate("2024-03-01");

        SecurityPatchDate genericVulkanDate = new SecurityPatchDate("2024-06-01");

        // Date used to specify 'unreachable'/'unfixed'
        SecurityPatchDate genericUnfixedDate = new SecurityPatchDate("2099-12-31");
        int unfixedApiLevel = 99;

        // Should match, should be affected, should recommend Vulkan
        // Pass both device and SoC on affected list, other device/SoC combinations are tested
        // in the base MigrateVulkanTest scenario
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, unfixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, mockDevice, mockSoC);
            assert (recordMatch);
            boolean useVulkan = record.RecommendAffectedVulkan(mockDeviceDate);
            assert (!useVulkan);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_AFFECTED);
        }
    }

    // Validate that we flag devices as unaffected if they meet the API requirements
    @Test
    public void UnmitigatedAPITest() {
        int mockApiLevel = 36;
        String mockBrand = "f0nebrand";
        String mockDevice = "f0nedevice";
        String mockSoC = "f0neSoC";
        SecurityPatchDate mockDeviceDate = new SecurityPatchDate("2024-09-01");

        SecurityPatchDate genericVulkanDate = new SecurityPatchDate("2024-06-01");

        // Date used to specify 'unreachable'/'unfixed'
        SecurityPatchDate genericUnfixedDate = new SecurityPatchDate("2099-12-31");
        int fixedApiLevel = 35;

        // Should match, should be affected, should recommend Vulkan
        // Pass both device and SoC on affected list, other device/SoC combinations are tested
        // in the base MigrateVulkanTest scenario
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, fixedApiLevel, genericUnfixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, mockDevice, mockSoC);
            assert (recordMatch);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_UNAFFECTED);
        }
    }

    // Validate that we flag devices as unaffected if they meet the API requirements
    @Test
    public void UnmitigatedPatchDateTest() {
        int mockApiLevel = 34;
        String mockBrand = "f0nebrand";
        String mockDevice = "f0nedevice";
        String mockSoC = "f0neSoC";
        SecurityPatchDate mockDeviceDate = new SecurityPatchDate("2024-09-01");

        SecurityPatchDate genericVulkanDate = new SecurityPatchDate("2024-06-01");

        // Date used to specify 'unreachable'/'unfixed'
        SecurityPatchDate fixedDate = new SecurityPatchDate("2024-07-01");
        int fixedApiLevel = 35;

        // Should match, should be affected, should recommend Vulkan
        // Pass both device and SoC on affected list, other device/SoC combinations are tested
        // in the base MigrateVulkanTest scenario
        {
            StartupMitigationRecord record = new StartupMitigationRecord(mockBrand, mockDevice,
                    mockSoC, mockApiLevel, fixedApiLevel, fixedDate, genericVulkanDate);

            boolean recordMatch = record.RecordMatch(mockBrand, mockDevice, mockSoC);
            assert (recordMatch);
            int isAffected = record.IsDeviceAffected(mockApiLevel, mockDeviceDate);
            assert (isAffected == StartupMitigation.DEVICE_UNAFFECTED);
        }
    }
}
