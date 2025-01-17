// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dxgi;

namespace AIDevGallery.Utils;

internal static class DeviceUtils
{
    private const uint INTEL_VENDOR_ID = 32902; // Used for naive check to see if video adapter is internal which needs to use shared memory

    public static int GetBestDeviceId() => GetBestDevice().DeviceId;

    public static ulong GetVram() => GetBestDevice().VideoMemory;

    private static DeviceInfo GetBestDevice()
    {
        int deviceId = 0;
        nuint videoMemory = 0;
        try
        {
            DXGI_CREATE_FACTORY_FLAGS createFlags = 0;
            Windows.Win32.PInvoke.CreateDXGIFactory2(createFlags, typeof(IDXGIFactory2).GUID, out object dxgiFactoryObj).ThrowOnFailure();
            IDXGIFactory2? dxgiFactory = (IDXGIFactory2)dxgiFactoryObj;

            IDXGIAdapter1? selectedAdapter = null;

            var index = 0u;
            do
            {
                var result = dxgiFactory.EnumAdapters1(index, out IDXGIAdapter1? dxgiAdapter1);

                if (result.Failed)
                {
                    if (result != HRESULT.DXGI_ERROR_NOT_FOUND)
                    {
                        result.ThrowOnFailure();
                    }

                    index = 0;
                }
                else
                {
                    DXGI_ADAPTER_DESC1 dxgiAdapterDesc = dxgiAdapter1.GetDesc1();

                    var isInternal = dxgiAdapterDesc.VendorId == INTEL_VENDOR_ID;
                    var adapterVideoMemory = isInternal ? dxgiAdapterDesc.SharedSystemMemory : dxgiAdapterDesc.DedicatedVideoMemory;
                    if (isInternal && (selectedAdapter == null || adapterVideoMemory > videoMemory))
                    {
                        videoMemory = adapterVideoMemory;
                        selectedAdapter = dxgiAdapter1;
                        deviceId = (int)index;
                    }

                    index++;
                    dxgiAdapter1 = null;
                }
            }
            while (index != 0);
        }
        catch (Exception)
        {
        }

        return new DeviceInfo(deviceId, videoMemory);
    }

    public static bool IsArm64()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
    }

    private record DeviceInfo(int DeviceId, ulong VideoMemory);
}