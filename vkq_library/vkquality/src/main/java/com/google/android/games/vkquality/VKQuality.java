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

import android.content.Context;
import android.content.res.AssetManager;

public class VKQuality {
    // Used to load the 'vkqualitytest' library on application startup.
    static {
        System.loadLibrary("vkquality");
    }

    public static final int INIT_SUCCESS = 0;
    public static final int ERROR_INITIALIZATION_FAILURE = -1;
    public static final int ERROR_NO_VULKAN = -2;
    public static final int ERROR_INVALID_DATA_VERSION = -3;
    public static final int ERROR_INVALID_DATA_FILE = -4;
    public static final int ERROR_MISSING_DATA_FILE = -5;

    public static final int RECOMMENDATION_ERROR_NOT_INITIALIZED = -1;
    public static final int RECOMMENDATION_NOT_READY = -2;
    public static final int RECOMMENDATION_VULKAN_BECAUSE_DEVICE_MATCH = 0;
    public static final int RECOMMENDATION_VULKAN_BECAUSE_PREDICTION_MATCH = 1;
    public static final int RECOMMENDATION_VULKAN_BECAUSE_FUTURE_ANDROID = 2;
    public static final int RECOMMENDATION_GLES_BECAUSE_OLD_DEVICE = 3;
    public static final int RECOMMENDATION_GLES_BECAUSE_OLD_DRIVER = 4;
    public static final int RECOMMENDATION_GLES_BECAUSE_NO_DEVICE_MATCH = 5;
    public static final int RECOMMENDATION_GLES_BECAUSE_PREDICTION_MATCH = 6;

    private static final String DEFAULT_QUALITY_FILE = "vkqualitydata.vkq";

    public VKQuality(Context appContext) {
        mAppContext = appContext;
    }

    public int StartVkQuality(String customDataFilename) {
        String dataFilename = customDataFilename.isEmpty() ?
                DEFAULT_QUALITY_FILE : customDataFilename;
        return startVkQuality(mAppContext.getResources().getAssets(),
                mAppContext.getFilesDir().getAbsolutePath(),
                dataFilename);
    }

    public void StopVkQuality() {
        stopVkQuality();
    }

    public int GetVkQuality() {
        return getVkQuality();
    }

    private final Context mAppContext;

    // JNI native functions
    public native int startVkQuality(AssetManager jasset_manager, String storage_path,
                                     String data_filename);

    public native void stopVkQuality();

    public native int getVkQuality();
}
