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
#include "gtest/gtest.h"
#include "vkquality_manager.h"
#include "vkquality_matching.h"

// From Vulkan.h, so we don't have to pull in the whole header
#define VK_MAKE_API_VERSION(variant, major, minor, patch) \
    ((((uint32_t)(variant)) << 29) | (((uint32_t)(major)) << 22) | (((uint32_t)(minor)) << 12) | ((uint32_t)(patch)))

#define VK_API_VERSION_1_0 VK_MAKE_API_VERSION(0, 1, 0, 0)// Patch version should always be set to 0
#define VK_API_VERSION_1_1 VK_MAKE_API_VERSION(0, 1, 1, 0)// Patch version should always be set to 0
#define VK_API_VERSION_1_3 VK_MAKE_API_VERSION(0, 1, 3, 0)// Patch version should always be set to 0

using namespace vkquality;
using namespace std;

class MemoryBuffer {
public:
  static constexpr size_t kDefaultBufferSize = 1024 * 1024;
  MemoryBuffer(const size_t buffer_size = kDefaultBufferSize, bool free_destruct = false);
  ~MemoryBuffer();
  void* GetPtr() { return buffer_; }
  size_t GetUsedSize() { return cursor_; }
  size_t GetTotalSize() { return size_; }
  size_t Push(void *data, const size_t size);
  size_t PushZero(const size_t size);
  size_t PushString(const char *str);
private:
  void *buffer_ = nullptr;
  size_t size_ = 0;
  size_t cursor_ = 0;
  bool free_on_destruct_ = false;
};

MemoryBuffer::MemoryBuffer(const size_t buffer_size, bool free_destruct) {
  buffer_ = malloc(buffer_size);
  size_ = buffer_size;
  cursor_ = 0;
  free_on_destruct_ = free_destruct;
}

MemoryBuffer::~MemoryBuffer() {
  if (buffer_ != nullptr && free_on_destruct_) {
    free(buffer_);
  }
}

size_t MemoryBuffer::Push(void *data, const size_t size) {
  size_t old_cursor = cursor_;
  if (cursor_ + size <= size_) {
    uint8_t *base = static_cast<uint8_t *>(buffer_);
    base += cursor_;
    memcpy(base, data, size);
    cursor_ += size;
  }
  return old_cursor;
}

size_t MemoryBuffer::PushZero(const size_t size) {
  size_t old_cursor = cursor_;
  if (cursor_ + size <= size_) {
    uint8_t *base = static_cast<uint8_t *>(buffer_);
    base += cursor_;
    memset(base, 0, size);
    cursor_ += size;
  }
  return old_cursor;
}

size_t MemoryBuffer::PushString(const char *string_input) {
  size_t old_cursor = cursor_;
  size_t string_length = strlen(string_input) + 1; // +1 for 0 terminator
  if (cursor_ + string_length <= size_) {
    uint8_t *base = static_cast<uint8_t *>(buffer_);
    base += cursor_;
    memcpy(base, string_input, string_length);
    cursor_ += string_length;
  }
  return old_cursor;
}

#define PUSH_BUFFER(a) memory_buffer.Push((void*)a, sizeof(a))
#define PUSH_ZERO(a) memory_buffer.PushZero(a)
#define PUSH_STRING(a) memory_buffer.PushString(a)

TEST(MemoryBufferTests, Validity)
{
  MemoryBuffer memory_buffer(256);
  EXPECT_EQ(memory_buffer.GetTotalSize(), 256);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 0);
  void* start = memory_buffer.GetPtr();
  EXPECT_NE(start, nullptr);
  static constexpr uint32_t kTestBuffer1[4] = {0x12345678, 0x87654321, 0x0d0d0d0d, 0x11223344};
  EXPECT_EQ(memory_buffer.Push((void*)kTestBuffer1, sizeof(kTestBuffer1)), 0);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 16);
  void* too_big = malloc(512);
  EXPECT_EQ(memory_buffer.Push(too_big, 512), 16);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 16);
  EXPECT_EQ(memory_buffer.PushZero(16), 16);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 32);
  const char *buffer_string_test = "Test String";
  EXPECT_EQ(memory_buffer.PushString(buffer_string_test), 32);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 44);
  size_t remaining_size = 256 - memory_buffer.GetUsedSize();
  EXPECT_EQ(memory_buffer.PushZero(remaining_size), 44);
  EXPECT_EQ(memory_buffer.GetUsedSize(), 256);
  EXPECT_EQ(memory_buffer.PushString(buffer_string_test), 256);
  free(too_big);
}

static constexpr uint32_t kDefaultMinAndroidApi = 34;

static constexpr uint32_t kValidVersion = 0x10000;

static constexpr uint32_t kTestString_Empty = 0;
static constexpr uint32_t kTestString_GpuFakeGoogle250 = 1;
static constexpr uint32_t kTestString_GpuFakeGoogle290 = 2;
static constexpr uint32_t kTestString_BrandGoogle = 3;
static constexpr uint32_t kTestString_DevicePixelPi = 4;
static constexpr uint32_t kTestString_DevicePixel7 = 5;
static constexpr uint32_t kTestString_BrandSuperfone = 6;
static constexpr uint32_t kTestString_DeviceSuperfone9000 = 7;
static constexpr uint32_t kTestString_GpuZMistake = 8;
static constexpr uint32_t kTestString_Gpu9dfx = 9;

static constexpr uint32_t kTestStringTableCount = 10;

static constexpr const char *kTestStrings[kTestStringTableCount] = {
    "",
    "gGPU a250",
    "gGPU a290",
    "google",
    "pixel3.14",
    "pixel7",
    "superfone",
    "superfone 9000",
    "zmistake XL",
    "9dfx doovoo 500"
};

static constexpr VkQualityFileHeader kGoodHeaderTemplate {
    VkQualityPredictionFile::kVkQuality_File_Identifier,
    kValidVersion,
    kValidVersion,
    1,
    36,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0
};

static constexpr uint32_t kFakeGpuVendorId_Google = 0xc0000;
static constexpr uint32_t kFakeGpuVendorId_9dfx = 0x938a;
static constexpr uint32_t kFakeGpuVendorId_ZMistake = 0x31100;
static constexpr uint32_t kFakeGpuVendor_Google_MinDriverVersion = 0x1000;
static constexpr uint32_t kFakeGpuVendor_9dfx_MinDriverVersion = 0x100;
static constexpr uint32_t kFakeGpuVendor_ZMistake_MinDriverVersion = 0x8000;

static constexpr size_t kDefaultDeviceListCount = 4;
static constexpr VkQualityDeviceAllowListEntry kDefaultDeviceList[kDefaultDeviceListCount] = {
    {
      kTestString_BrandGoogle, kTestString_DevicePixelPi,
      kDefaultMinAndroidApi, kFakeGpuVendor_Google_MinDriverVersion
    },
    {
        kTestString_BrandGoogle, kTestString_DevicePixel7,
        kDefaultMinAndroidApi, kFakeGpuVendor_Google_MinDriverVersion
    },
    {
        kTestString_BrandGoogle, kTestString_Empty,
        kDefaultMinAndroidApi, kFakeGpuVendor_Google_MinDriverVersion
    },
    {
        kTestString_BrandSuperfone, kTestString_DeviceSuperfone9000,
        kDefaultMinAndroidApi, kFakeGpuVendor_9dfx_MinDriverVersion
    }
};

static constexpr size_t kDefaultGpuAllowCount = 3;
static constexpr VkQualityGpuPredictEntry kDefaultGpuAllowList[kDefaultGpuAllowCount] = {
    {
        kTestString_GpuFakeGoogle250, kDefaultMinAndroidApi,
        0xc0250, kFakeGpuVendorId_Google, kFakeGpuVendor_Google_MinDriverVersion
    },
    {
        kTestString_GpuFakeGoogle290, kDefaultMinAndroidApi,
        0xc0250, kFakeGpuVendorId_Google, kFakeGpuVendor_Google_MinDriverVersion
    },
    {
        kTestString_Gpu9dfx, kDefaultMinAndroidApi,
        0xc0250, kFakeGpuVendorId_9dfx, kFakeGpuVendor_9dfx_MinDriverVersion
    }
};

static constexpr size_t kDefaultGpuDenyCount = 1;
static constexpr VkQualityGpuPredictEntry kDefaultGpuDenyList[kDefaultGpuDenyCount] = {
    {
        kTestString_GpuZMistake, kDefaultMinAndroidApi,
        0xc0250, kFakeGpuVendorId_ZMistake, kFakeGpuVendor_ZMistake_MinDriverVersion
    }
};

// Make sure NotEmpty works
TEST(VkQualityTestNE, NotEmpty) {
EXPECT_NE(sizeof(VkQualityFileHeader), 0);
}

// Make sure Validity works
// Also checks assumptions on data structure sizes
TEST(VkQualityTestValidity, Validity)
{
const size_t fh_size = sizeof(VkQualityFileHeader);
EXPECT_EQ(fh_size, 56);
EXPECT_NE(fh_size, 0);
}

// VkQUalityPredictionFile ParseFileData tests
static constexpr uint32_t kTooSmallBuffer[4] {0, 0, 0, 0};

TEST(VkQualityFileParseSizeCheck, Validity)
{
  VkQualityPredictionFile file;
  void *ptr = (void*)kTooSmallBuffer;
  const auto result = file.ParseFileData(ptr, sizeof(kTooSmallBuffer),
                                         kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_TooSmall);
}

static constexpr uint32_t kOldVersion = 0x100;

TEST(VkQualityFileParseIdentifierCheck, Validity)
{
  VkQualityFileHeader header {
      0,
      kValidVersion,
      kValidVersion,
      1,
      36,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
  };

  VkQualityPredictionFile file;

  const auto result = file.ParseFileData(&header, sizeof(header),
                                         kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_InvalidIdentifier);
}

TEST(VkQualityFileParseHeaderVersionCheck, Validity)
{
  VkQualityPredictionFile file;
  void *ptr = (void*)&kGoodHeaderTemplate;
  const auto result = file.ParseFileData(ptr, sizeof(kGoodHeaderTemplate),
                                         kOldVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_LibraryTooOldForFile);
}

//int debug_counter = 0;
//void *debug_ptr = nullptr;

static void ConstructValidFile(MemoryBuffer &memory_buffer) {
  EXPECT_EQ(memory_buffer.GetTotalSize(), MemoryBuffer::kDefaultBufferSize);
  void *zero_buffer = malloc(1024*1024);
  memset(zero_buffer, 0, 1024*1024);

  memory_buffer.Push((void*)&kGoodHeaderTemplate, sizeof(VkQualityFileHeader));
  EXPECT_EQ(memory_buffer.GetUsedSize(), sizeof(VkQualityFileHeader));
  uint8_t *base = reinterpret_cast<uint8_t *>(memory_buffer.GetPtr());
  VkQualityFileHeader *header = reinterpret_cast<VkQualityFileHeader*>(base);
  header->device_list_count = kDefaultDeviceListCount;
  header->gpu_allow_predict_count = kDefaultGpuAllowCount;
  header->gpu_deny_predict_count = kDefaultGpuDenyCount;
  header->string_table_count = kTestStringTableCount;

  header->device_list_shortcuts_offset = 0x7FFFFFFF;
  header->device_list_offset = 0x7FFFFFFF;
  header->gpu_allow_predict_offset = 0x7FFFFFFF;
  header->gpu_deny_predict_offset = 0x7FFFFFFF;
  header->string_table_offset = 0x7FFFFFFF;

  size_t device_list_offset = PUSH_BUFFER(kDefaultDeviceList);
  header->device_list_offset = static_cast<uint32_t>(device_list_offset);
  size_t device_list_size = memory_buffer.GetUsedSize() - device_list_offset;
  EXPECT_EQ(device_list_size, header->device_list_count * sizeof(VkQualityDeviceAllowListEntry));

  size_t gpu_allow_offset = PUSH_BUFFER(kDefaultGpuAllowList);
  header->gpu_allow_predict_offset = static_cast<uint32_t>(gpu_allow_offset);
  size_t allow_list_size = memory_buffer.GetUsedSize() - gpu_allow_offset;
  EXPECT_EQ(allow_list_size, header->gpu_allow_predict_count * sizeof(VkQualityGpuPredictEntry));

  size_t gpu_deny_offset = PUSH_BUFFER(kDefaultGpuDenyList);
  header->gpu_deny_predict_offset = static_cast<uint32_t>(gpu_deny_offset);
  size_t deny_list_size = memory_buffer.GetUsedSize() - gpu_deny_offset;
  EXPECT_EQ(deny_list_size, header->gpu_deny_predict_count * sizeof(VkQualityGpuPredictEntry));

  const size_t string_offset_size = sizeof(uint32_t) * header->string_table_count;
  const size_t strings_offset = PUSH_ZERO(string_offset_size);
  header->string_table_offset = static_cast<uint32_t>(strings_offset);
  uint32_t *string_offsets = reinterpret_cast<uint32_t *>(base + strings_offset);
  for (uint32_t i = 0; i < header->string_table_count; ++i) {
    const char *test_string = kTestStrings[i];
    const size_t string_offset = PUSH_STRING(test_string);
    string_offsets[i] = string_offset;
  }

  // Zero values for shortcut indices are valid, just starts a search from the beginning
  const size_t zero_shortcut_size = sizeof(uint32_t) * VkQualityPredictionFile::kShortcut_Offset_Count;
  const size_t shortcut_offset = PUSH_ZERO(zero_shortcut_size);
  header->device_list_shortcuts_offset = static_cast<uint32_t>(shortcut_offset);

  free(zero_buffer);
}

TEST(VkQualityFileParseHeaderValid, Validity)
{
  MemoryBuffer memory_buffer;
  ConstructValidFile(memory_buffer);

  VkQualityPredictionFile file;
  const auto result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                                         kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Success);
  //  << " debug_counter: " << debug_counter << " debug_ptr: " <<
  //    "0x" << std::uppercase << std::setfill('0') << std::setw(8) << std::hex <<((uint64_t)debug_ptr);
}

// Verify bounds-check of offset table counts
TEST(VkQualityFileParseHeaderOffsetCounts, Validity)
{
  MemoryBuffer memory_buffer;
  ConstructValidFile(memory_buffer);

  uint8_t *base = reinterpret_cast<uint8_t *>(memory_buffer.GetPtr());
  VkQualityFileHeader *header = reinterpret_cast<VkQualityFileHeader*>(base);

  VkQualityPredictionFile file;

  // corrupt offset table counts
  uint32_t old_count;

  old_count = header->device_list_count;
  header->device_list_count = 0x7FFFFFFF;
  auto result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                                   kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_DeviceListOverflow);
  header->device_list_count = old_count;

  old_count = header->gpu_allow_predict_count;
  header->gpu_allow_predict_count = 0x7FFFFFFF;
  result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                              kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_GpuAllowOverflow);
  header->gpu_allow_predict_count = old_count;

  old_count = header->gpu_deny_predict_count;
  header->gpu_deny_predict_count = 0x7FFFFFFF;
  result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                              kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_GpuDenyOverflow);
  header->gpu_deny_predict_count = old_count;

  old_count = header->string_table_count;
  header->string_table_count = 0x7FFFFFFF;
  result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                              kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_StringOffsetOverflow);
  header->string_table_count = old_count;
}

// Verify bounds-check of offsets in string table
TEST(VkQualityFileParseHeaderOffsetTables, Validity)
{
  MemoryBuffer memory_buffer;
  ConstructValidFile(memory_buffer);

  uint8_t *base = reinterpret_cast<uint8_t *>(memory_buffer.GetPtr());
  VkQualityFileHeader *header = reinterpret_cast<VkQualityFileHeader*>(base);

  VkQualityPredictionFile file;

  // corrupt offsets to offset tables
  uint32_t old_offset;

  uint32_t *string_offsets = reinterpret_cast<uint32_t *>((base + header->string_table_offset));
  old_offset = *string_offsets;
  *string_offsets = 0x7FFFFFFF;
  auto result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                                   kValidVersion);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileParseResult_Error_StringOffsetOverflow);
  *string_offsets = old_offset;
}

TEST(VkQualityStringComparison, Validity)
{
  std::string start = "Match Me A";

  std::string exact_b = "Not a match";
  std::string exact_c = "Match Me A";
  std::string exact_d = "match me a";
  auto result = VkQualityMatching::StringMatches(start, exact_b);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_None);
  result = VkQualityMatching::StringMatches(start, exact_c);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_Exact);
  result = VkQualityMatching::StringMatches(start, exact_d);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_None);

  std::string start_a = "^ Me A";
  std::string start_b = "^Match Me";
  result = VkQualityMatching::StringMatches(start, start_a);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_None);
  result = VkQualityMatching::StringMatches(start, start_b);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_Substring_Start);

  std::string subs_a = "*Be A";
  std::string subs_b = "* Me ";
  result = VkQualityMatching::StringMatches(start, subs_a);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_None);
  result = VkQualityMatching::StringMatches(start, subs_b);
  EXPECT_EQ(result, VkQualityMatching::kStringMatch_Substring);
}

TEST(VkQualityDeviceMatchTests, Validity)
{
  DeviceInfo device_info {
    "moogle",
    "nixel 5",
    "mobilegpu a8",
    30,
    VK_API_VERSION_1_1,
    0x3330000,
    0x10000,
    0x4440000
  };

  auto result = VkQualityMatching::CheckDeviceMatch(device_info,
                                                    "meowphone", "kibbleplus", 0, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 6", 0, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "voogle", "nixel 5", 0, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5", 0, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_ExactDevice);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5", device_info.api_level, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_ExactDevice);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5", 0, device_info.vk_driver_version);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_ExactDevice);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5",
                                               device_info.api_level, device_info.vk_driver_version);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_ExactDevice);

  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5",
                                               device_info.api_level + 1, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_DeviceOldVersion);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5",
                                               0, device_info.vk_driver_version + 1);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_DeviceOldVersion);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "nixel 5",
                                               device_info.api_level + 1, device_info.vk_driver_version + 1);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_DeviceOldVersion);

  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "", 0, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_BrandWildcard);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "", device_info.api_level, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_BrandWildcard);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "", 0, device_info.vk_driver_version);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_BrandWildcard);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "",
                                               device_info.api_level, device_info.vk_driver_version);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_BrandWildcard);

  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "", device_info.api_level + 1, 0);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "", 0, device_info.vk_driver_version + 1);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);
  result = VkQualityMatching::CheckDeviceMatch(device_info,
                                               "moogle", "",
                                               device_info.api_level + 1, device_info.vk_driver_version + 1);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

}

TEST(VkQualityGpuTests, Validity)
{
  DeviceInfo device_info {
      "moogle",
      "nixel 5",
      "mobilegpu a8",
      30,
      VK_API_VERSION_1_1,
      0x3330000,
      0x10000,
      0x4440000
  };

  auto result = VkQualityMatching::CheckGpuMatch(device_info,
                                                 "desktopgpu",
                                                 0x123,
                                                 0x456,
                                                 0,
                                                 0,
                                                 VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "desktopgpu",
                                            0,
                                            0,
                                            0,
                                            0,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            0x123,
                                            0x456,
                                            0,
                                            0,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "mobilegpu a8",
                                            0,
                                            0,
                                            0,
                                            0,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_GpuAllow);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "^mobilegpu a8",
                                            0,
                                            0,
                                            0,
                                            0,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_GpuAllow);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "mobilegpu a8",
                                            0,
                                            0,
                                            device_info.api_level,
                                            device_info.vk_driver_version,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_GpuAllow);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "mobilegpu a8",
                                            0,
                                            0,
                                            device_info.api_level + 1,
                                            device_info.vk_driver_version,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "mobilegpu a8",
                                            0,
                                            0,
                                            device_info.api_level,
                                            device_info.vk_driver_version + 1,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "mobilegpu a8",
                                            0,
                                            0,
                                            device_info.api_level + 1,
                                            device_info.vk_driver_version + 1,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            device_info.vk_device_id,
                                            device_info.vk_vendor_id,
                                            0,
                                            0,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_GpuAllow);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            device_info.vk_device_id,
                                            device_info.vk_vendor_id,
                                            device_info.api_level,
                                            device_info.vk_driver_version,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_GpuAllow);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            device_info.vk_device_id,
                                            device_info.vk_vendor_id,
                                            device_info.api_level + 1,
                                            device_info.vk_driver_version,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            device_info.vk_device_id,
                                            device_info.vk_vendor_id,
                                            device_info.api_level,
                                            device_info.vk_driver_version + 1,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

  result = VkQualityMatching::CheckGpuMatch(device_info,
                                            "",
                                            device_info.vk_device_id,
                                            device_info.vk_vendor_id,
                                            device_info.api_level + 1,
                                            device_info.vk_driver_version + 1,
                                            VkQualityPredictionFile::kFileMatch_GpuAllow);
  EXPECT_EQ(result, VkQualityPredictionFile::kFileMatch_None);

}

TEST(VkQualityRecommendationTests, Validity) {
  // Device list matching
  DeviceInfo device_info {
      "google",
      "pixel3.14",
      "gGPU",
      kDefaultMinAndroidApi,
      VK_API_VERSION_1_3,
      0x111,
      kFakeGpuVendor_Google_MinDriverVersion,
      kFakeGpuVendorId_Google
  };

  MemoryBuffer memory_buffer;
  ConstructValidFile(memory_buffer);

  VkQualityPredictionFile file;
  const auto parse_result = file.ParseFileData(memory_buffer.GetPtr(), memory_buffer.GetUsedSize(),
                                         kValidVersion);
  EXPECT_EQ(parse_result, VkQualityPredictionFile::kFileParseResult_Success);

  auto recommendation = file.FindDeviceMatch(device_info);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_ExactDevice);

  device_info.vk_driver_version -= 1;
  recommendation = file.FindDeviceMatch(device_info);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_DeviceOldVersion);

  device_info.vk_driver_version = kFakeGpuVendor_Google_MinDriverVersion;
  device_info.api_level -= 1;
  recommendation = file.FindDeviceMatch(device_info);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_DeviceOldVersion);

  // Brand wildcard matching
  DeviceInfo device_info_brand {
      "google",
      "",
      "gGPU",
      kDefaultMinAndroidApi + 1,
      VK_API_VERSION_1_3,
      0x111,
      kFakeGpuVendor_Google_MinDriverVersion,
      kFakeGpuVendorId_Google
  };
  recommendation = file.FindDeviceMatch(device_info_brand);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_BrandWildcard);

  // GPU allow matchine
  DeviceInfo device_info_gpu_allow {
      "fakebrand",
      "fakefone",
      "9dfx doovoo 500",
      kDefaultMinAndroidApi,
      VK_API_VERSION_1_3,
      0x333,
      kFakeGpuVendor_9dfx_MinDriverVersion,
      kFakeGpuVendorId_9dfx
  };
  recommendation = file.FindDeviceMatch(device_info_gpu_allow);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_GpuAllow);

  device_info_gpu_allow.vk_driver_version -= 1;
  recommendation = file.FindDeviceMatch(device_info_gpu_allow);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_None);

  device_info_gpu_allow.vk_driver_version = kFakeGpuVendor_Google_MinDriverVersion;
  device_info_gpu_allow.api_level -= 1;
  recommendation = file.FindDeviceMatch(device_info_gpu_allow);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_None);

  // GPU deny matchine
  DeviceInfo device_info_gpu_deny {
      "notrealbrand",
      "notrealfone",
      "zmistake XL",
      kDefaultMinAndroidApi,
      VK_API_VERSION_1_3,
      0x222,
      kFakeGpuVendor_ZMistake_MinDriverVersion,
      kFakeGpuVendorId_ZMistake
  };
  recommendation = file.FindDeviceMatch(device_info_gpu_deny);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_GpuDeny);

  device_info_gpu_deny.vk_driver_version += 1;
  recommendation = file.FindDeviceMatch(device_info_gpu_deny);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_None);

  device_info_gpu_deny.vk_driver_version = kFakeGpuVendor_Google_MinDriverVersion;
  device_info_gpu_deny.api_level += 1;
  recommendation = file.FindDeviceMatch(device_info_gpu_deny);
  EXPECT_EQ(recommendation, VkQualityPredictionFile::kFileMatch_None);
}