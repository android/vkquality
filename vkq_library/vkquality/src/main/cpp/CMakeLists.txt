#
# Copyright 2024 Google LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

# Sets the minimum CMake version required for this project.
cmake_minimum_required(VERSION 3.22.1)

# Declares the project name. The project name can be accessed via ${PROJECT_NAME},
# Since this is the top level CMakeLists.txt, the project name is also accessible
# with ${CMAKE_PROJECT_NAME} (both CMake variables are in-sync within the top level
# build script scope).
project("vkquality")

# Unit test dependencies
find_package(googletest REQUIRED CONFIG)
find_package(junit-gtest REQUIRED CONFIG)

set(VKQ_SRCS
        # List C/C++ source files with relative paths to this CMakeLists.txt.
        vkquality_matching.cpp
        vkquality_prediction_file.cpp)

add_library(vkq OBJECT ${VKQ_SRCS})

# Creates and names a library, sets it as either STATIC
# or SHARED, and provides the relative paths to its source code.
# You can define multiple libraries, and CMake builds them for you.
# Gradle automatically packages shared libraries with your APK.
#
# In this top level CMakeLists.txt, ${CMAKE_PROJECT_NAME} is used to define
# the target library name; in the sub-module's CMakeLists.txt, ${PROJECT_NAME}
# is preferred for the same purpose.
#
# In order to load a library into your app from Java/Kotlin, you must call
# System.loadLibrary() and pass the name of the library defined here;
# for GameActivity/NativeActivity derived applications, the same library name must be
# used in the AndroidManifest.xml file.
add_library(${CMAKE_PROJECT_NAME} SHARED
        $<TARGET_OBJECTS:vkq>
        gles_util.cpp
        vkquality_c.cpp
        vkquality_manager.cpp
        vulkan_util.cpp)


set_target_properties(${CMAKE_PROJECT_NAME} PROPERTIES CXX_STANDARD 17)
target_compile_options(${CMAKE_PROJECT_NAME} PRIVATE -Wall -Werror)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Os")
set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Os")

# Specifies libraries CMake should link to your target library. You
# can link libraries from various origins, such as libraries defined in this
# build script, prebuilt third-party libraries, or Android system libraries.
target_link_libraries(${CMAKE_PROJECT_NAME}
        # List libraries link to the target library
        android
        EGL
        GLESv3
        log)

add_library(vkq_tests SHARED vkquality_tests.cpp)
target_link_libraries(vkq_tests
        PRIVATE
        $<TARGET_OBJECTS:vkq>
        googletest::gtest
        junit-gtest::junit-gtest
)