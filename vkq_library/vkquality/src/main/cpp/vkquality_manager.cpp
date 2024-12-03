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

#include <iostream>
#include <jni.h>
#include <string>
#include <sys/stat.h>
#include <vector>
#include <android/api-level.h>
#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>
#include <android/log.h>

#include "vkquality_manager.h"
#include "gles_util.h"
#include "vulkan_util.h"

#define ALOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)
#define LOG_TAG "VKQUALITY"

extern "C" uint32_t VkQuality_getVersion();

namespace vkquality {
// Recommendation cache filename
constexpr const char *kCacheFilename = "vkqcache.bin";

// Build.SOC_MODEL requires API 31 or higher
constexpr const int kMinSoCAPI = 31;

// Device info class and field name constants for Android
constexpr const char *kAndroidBuildClass = "android/os/Build";
constexpr const char *kBrandField = "BRAND";
constexpr const char *kDeviceField = "DEVICE";
constexpr const char *kSoCField = "SOC_MODEL";

std::mutex VkQualityManager::instance_mutex_;
std::unique_ptr<VkQualityManager> VkQualityManager::instance_ = nullptr;

vkQualityInitResult VkQualityManager::Init(JNIEnv *env, AAssetManager *asset_manager,
                                           const char *storage_path,
                                           const char *asset_filename,
                                           int32_t flags) {
  std::lock_guard<std::mutex> lock(instance_mutex_);
  if (instance_ != nullptr) {
    // Already initialized
    return kErrorInitializationFailure;
  }

  instance_ = std::make_unique<VkQualityManager>(env, asset_manager,
                                                 storage_path, asset_filename, flags,
                                                 ConstructorTag{});
  if (instance_ == nullptr) {
    return kErrorInitializationFailure;
  }
  return instance_->StartRecommendation();
}

VkQualityManager* VkQualityManager::GetInstance() {
  std::lock_guard<std::mutex> lock(instance_mutex_);
  return instance_.get();
}

void VkQualityManager::DestroyInstance(JNIEnv */*env*/) {
  std::lock_guard<std::mutex> lock(instance_mutex_);
  instance_.reset();
}

vkQualityRecommendation VkQualityManager::GetQualityRecommendation() {
  VkQualityManager* mgr = VkQualityManager::GetInstance();
  if (mgr == nullptr) {
    return kRecommendationErrorNotInitialized;
  }
  return mgr->quality_recommendation_;
}

VkQualityManager::VkQualityManager(JNIEnv *env, AAssetManager *asset_manager,
                                   const char *storage_path, const char *asset_filename, int32_t flags,
                                   ConstructorTag)
    :asset_manager_(asset_manager)
    ,env_(env)
    ,asset_filename_(asset_filename)
    ,storage_path_()
    ,flags_(flags) {
  //ALOGE("INIT PATHS %s %s", storage_path, asset_filename);
  if (storage_path != nullptr) {
    storage_path_ = storage_path;
  }
}

std::string VkQualityManager::GetStaticStringField(JNIEnv *env, jclass clz,
                                                   const char *name) {
  jfieldID field_id = env->GetStaticFieldID(clz, name, "Ljava/lang/String;");
  if (env->ExceptionCheck()) {
    env->ExceptionClear();
    ALOGE("Failed to get string field %s", name);
    return "";
  }

  auto jstr = reinterpret_cast<jstring>(env->GetStaticObjectField(clz, field_id));
  if (env->ExceptionCheck()) {
    env->ExceptionClear();
    ALOGE("Failed to get string %s", name);
    return "";
  }
  auto cstr = env->GetStringUTFChars(jstr, nullptr);
  auto length = env->GetStringUTFLength(jstr);
  std::string ret_value(cstr, length);
  env->ReleaseStringUTFChars(jstr, cstr);
  env->DeleteLocalRef(jstr);
  return ret_value;
}

vkQualityInitResult VkQualityManager::InitDeviceInfo(JNIEnv *env, DeviceInfo &device_info) {
  jclass build_class = env->FindClass(kAndroidBuildClass);
  if (env->ExceptionCheck()) {
    env->ExceptionClear();
    ALOGE("Failed to get Build class");
    return kErrorInitializationFailure;
  }

  device_info.brand = GetStaticStringField(env, build_class, kBrandField);
  if (device_info.brand.empty()) return kErrorInitializationFailure;

  device_info.device = GetStaticStringField(env, build_class, kDeviceField);
  if (device_info.device.empty()) return kErrorInitializationFailure;

  device_info.api_level = android_get_device_api_level();

  device_info.gles_version = GLESUtil::GetGLESVersionString();

  // SoC string will be empty if we can't retrieve it due to older Android version
  if (device_info.api_level >= kMinSoCAPI) {
    device_info.soc = GetStaticStringField(env, build_class, kSoCField);
    if (device_info.soc.empty()) return kErrorInitializationFailure;
  }

  return VulkanUtil::GetDeviceVulkanInfo(device_info);
}

bool VkQualityManager::LoadCache(const DeviceInfo &device_info) {
  if (storage_path_.empty()) {
    return false;
  }
  bool loaded_cache = false;
  size_t cache_size = 0;
  void *cache_bytes = nullptr;
  auto result = LoadFile(nullptr, storage_path_, kCacheFilename, cache_size, &cache_bytes);
  if (result == kSuccess && cache_bytes != nullptr) {
    if (cache_size == sizeof(CacheFile)) {
      const CacheFile &cache_file = *(reinterpret_cast<CacheFile *>(cache_bytes));
      if (cache_file.schema_version == kCacheSchemaVersion) {
        if (cache_file.device_id == device_info.vk_device_id &&
            cache_file.vendor_id == device_info.vk_vendor_id &&
            cache_file.driver_version == device_info.vk_driver_version) {
          cache_list_version_ = cache_file.list_version;
          cache_recommendation_ = static_cast<vkQualityRecommendation>(cache_file.recommendation);
          loaded_cache = true;
        }
      }
    }
  }
  if (cache_bytes != nullptr) {
    free(cache_bytes);
  }
  return loaded_cache;
}

void VkQualityManager::SaveCache(const DeviceInfo &device_info) {
  CacheFile cache_file {kCacheSchemaVersion,
                        cache_list_version_,
                        quality_recommendation_,
                        device_info.vk_device_id,
                        device_info.vk_vendor_id,
                        device_info.vk_driver_version,
                        0, 0};
  SaveFile(storage_path_, kCacheFilename, sizeof(cache_file), &cache_file);
}

vkQualityInitResult VkQualityManager::LoadFile(AAssetManager *asset_manager,
                                               const std::string &storage_path,
                                               const std::string &file_name,
                                               size_t &file_size, void **file_bytes) {
  // Try and load it from the storage directory first
  if (!storage_path.empty()) {
    std::string full_path = storage_path + "/" + file_name;
    FILE *fp = fopen(full_path.c_str(), "rb");
    if (fp != nullptr) {
      struct stat fileStats{};
      int statResult = fstat(fileno(fp), &fileStats);
      if (statResult == 0) {
        file_size = fileStats.st_size;
      }
      if (file_size == 0) {
        fclose(fp);
        return kErrorInvalidDataFile;
      }
      *file_bytes = malloc(file_size + 1);
      if (*file_bytes == nullptr) {
        fclose(fp);
        return kErrorInitializationFailure;
      }
      fread((*file_bytes), file_size, 1, fp);
      fclose(fp);
    }
  }

  // Search in the app bundle second
  if (*file_bytes == nullptr && asset_manager != nullptr) {
    AAsset *json_asset = AAssetManager_open(asset_manager, file_name.c_str(), AASSET_MODE_STREAMING);
    if (json_asset != nullptr) {
      file_size = AAsset_getLength(json_asset);
      if (file_size == 0) {
        AAsset_close(json_asset);
        return kErrorInvalidDataFile;
      }
      *file_bytes = malloc(file_size + 1);
      if ((*file_bytes) == nullptr) {
        AAsset_close(json_asset);
        return kErrorInitializationFailure;
      }
      AAsset_read(json_asset, (*file_bytes), file_size);
      AAsset_close(json_asset);
    }
  }

  if ((*file_bytes) == nullptr) {
    return kErrorMissingDataFile;
  }

  return kSuccess;
}

bool VkQualityManager::SaveFile(const std::string &storage_path,
                                const std::string &file_name,
                                const size_t file_size, const void *file_bytes) {
  if (storage_path.empty()) {
    return false;
  }

  std::string full_path = storage_path + "/" + file_name;
  size_t count_written;
  FILE *fp = fopen(full_path.c_str(), "wb");
  if (fp != nullptr) {
    count_written = fwrite(file_bytes, file_size, 1, fp);
    fclose(fp);
  } else {
    return false;
  }
  return ((count_written * file_size) == file_size);
}

vkQualityInitResult VkQualityManager::StartRecommendation() {
  if (android_get_device_api_level() < __ANDROID_API_Q__) {
    // GLES recommendation when running on pre-Android 10
    quality_recommendation_ = kRecommendationGLESBecauseOldDevice;
    return kSuccess;
  }

  DeviceInfo device_info;
  vkQualityInitResult result = InitDeviceInfo(env_, device_info);
  if (result != kSuccess) {
    return result;
  }

  if (device_info.vk_api_version < VulkanUtil::GetMinimumRecommendedVulkanVersion()) {
    // GLES recommendation on devices limited to Vulkan 1.0.x
    quality_recommendation_ = kRecommendationGLESBecauseOldDevice;
    return kSuccess;
  }

  // Load cache after obtaining device info, we invalidate the cache if
  // the device info doesn't match the cached versions, in case the cache file
  // somehow got copied to a different device
  bool loaded_cache = LoadCache(device_info);

  if (asset_filename_.find(".vkq") != std::string::npos) {
    size_t vkq_size = 0;
    void *vkq_bytes = nullptr;
    result = LoadFile(asset_manager_, storage_path_, asset_filename_, vkq_size, &vkq_bytes);
    if (result == kSuccess) {
      const VkQualityPredictionFile::FileParseResult parse_result =
          prediction_file_.ParseFileData(vkq_bytes, vkq_size, VkQuality_getVersion());
      if (parse_result != VkQualityPredictionFile::kFileParseResult_Success) {
        ALOGE("Parsing VkQuality data file failed for reason: %s",
              prediction_file_.GetParseErrorString().c_str());
        free(vkq_bytes);
        if (parse_result == VkQualityPredictionFile::kFileParseResult_Error_LibraryTooOldForFile) {
          return kErrorInvalidDataVersion;
        }
        return kErrorInvalidDataFile;
      } else {
        if (loaded_cache && cache_list_version_ == prediction_file_.GetListVersion()) {
          quality_recommendation_ = cache_recommendation_;
        } else {
          VkQualityPredictionFile::FileMatchResult match_result =
              prediction_file_.FindDeviceMatch(device_info, flags_);

          switch (match_result) {
            case VkQualityPredictionFile::kFileMatch_ExactDevice:
            case VkQualityPredictionFile::kFileMatch_BrandWildcard:
              quality_recommendation_ = kRecommendationVulkanBecauseDeviceMatch;
              break;
            case VkQualityPredictionFile::kFileMatch_DeviceOldVersion:
              quality_recommendation_ = kRecommendationGLESBecauseOldDriver;
              break;
            case VkQualityPredictionFile::kFileMatch_DriverAllow:
            case VkQualityPredictionFile::kFileMatch_GpuAllow:
              quality_recommendation_ = kRecommendationVulkanBecausePredictionMatch;
              break;
            case VkQualityPredictionFile::kFileMatch_DriverDeny:
            case VkQualityPredictionFile::kFileMatch_GpuDeny:
              quality_recommendation_ = kRecommendationGLESBecausePredictionMatch;
              break;
            default:
              quality_recommendation_ = kRecommendationGLESBecauseNoDeviceMatch;
          }

          if (quality_recommendation_ == kRecommendationGLESBecauseNoDeviceMatch &&
              device_info.api_level >= prediction_file_.GetFutureAndroidAPILevel()) {
            quality_recommendation_ = kRecommendationVulkanBecauseFutureAndroid;
          }
          cache_list_version_ = prediction_file_.GetListVersion();
          SaveCache(device_info);
        }
      }
    }
  }

  return result;
}



} // namespace vkquality


