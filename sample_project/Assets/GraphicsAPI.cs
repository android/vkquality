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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GraphicsAPI : MonoBehaviour
{
    public Text ApiText;
    // Start is called before the first frame update

    void Start()
    {
        switch (SystemInfo.graphicsDeviceType)
        {
            case GraphicsDeviceType.Direct3D11:
                ApiText.text = "D3D11";
            break;
            case GraphicsDeviceType.Null:
                ApiText.text = "Null";
            break;
            case GraphicsDeviceType.OpenGLES2:
                ApiText.text = "OpenGL ES 2";
            break;
            case GraphicsDeviceType.OpenGLES3:
                ApiText.text = "OpenGL ES 3";
            break;
            case GraphicsDeviceType.Metal:
                ApiText.text = "Metal";
            break;
            case GraphicsDeviceType.OpenGLCore:
                ApiText.text = "OpenGL Core";
            break;
            case GraphicsDeviceType.Direct3D12:
                ApiText.text = "D3D12";
            break;
            case GraphicsDeviceType.Vulkan:
                ApiText.text = "Vulkan";
            break;
            default:
                ApiText.text = "Unknown API";
            break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
