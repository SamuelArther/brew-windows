class MoltenVk < Formula
  desc "Vulkan SDK pieces for Windows: loader, headers, SPIRV-Cross and SPIRV-Tools"
  homepage "https://vulkan.lunarg.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vulkan-loader-1~1.4.363.0-1-any.pkg.tar.zst"
  version "1.4.363.0"
  sha256 "065076fe13d39ab97eea4740596426d5ae741b5044e48e1c9495d04835c92dbe"
  license "Apache-2.0"

  # MoltenVK implements Vulkan on top of Metal because macOS has no Vulkan driver. Windows does: every
  # GPU driver installs a Vulkan ICD and the system loader (vulkan-1.dll), so programs only need the
  # loader's import library and the headers to build. This keg lays them out like MoltenVK's does
  # (prebuilt UCRT64 packages from MSYS2, Vulkan SDK 1.4.363.0):
  #   libexec/include  vulkan/, vk_video/, spirv_cross/, spirv-tools/, spirv/ headers
  #   libexec/lib      libvulkan-1.dll.a, SPIRV-Cross and SPIRV-Tools libraries
  #   lib/libMoltenVK.dll.a  the loader's import library, so `-lMoltenVK` links Windows' Vulkan
  #   include/MoltenVK/mvk_vulkan.h  includes <vulkan/vulkan.h> with the Win32 platform enabled
  # MoltenVKShaderConverter's job (SPIR-V to shading languages) is done by SPIRV-Cross, so the
  # command is spirv-cross with its own flags: --msl for Metal, --hlsl for Direct3D, --version 450.

  livecheck do
    url "https://github.com/KhronosGroup/Vulkan-Loader/archive/refs/tags/vulkan-sdk-1.4.363.0.tar.gz"
    regex(/^vulkan-sdk[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-vulkan-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vulkan-headers-1~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "be602b2657cace531316e78b1eda4087e8f7f84ebb877f08fba198a2db06406a"
  end

  resource "mingw-w64-ucrt-x86_64-spirv-cross" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spirv-cross-1~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "246759d1a8391c67a4180ad4d103a78219182ef7057d70469394fa6c40851187"
  end

  resource "mingw-w64-ucrt-x86_64-spirv-tools" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spirv-tools-3~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "d31d67fe918aeee21d1e930fd96360856a60146d23094e4ae8baf071a4049aaa"
  end

  resource "mingw-w64-ucrt-x86_64-spirv-headers" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spirv-headers-2~1.4.363.0-1-any.pkg.tar.zst"
    sha256 "c15f7df169ccd4bf57fc9053d34e762818fa96e3da229c92501d7eedf4aa71dd"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  def install
    msys2_install
    libexec.install libexec/"ucrt64/include", libexec/"ucrt64/lib"
    lib.mkpath
    cp libexec/"lib/libvulkan-1.dll.a", lib/"libMoltenVK.dll.a"
    (include/"MoltenVK/mvk_vulkan.h").write <<~C
      /* MoltenVK's mvk_vulkan.h selects the Metal surface platform and includes Vulkan. On Windows the
       * driver-provided Vulkan runs natively, so this selects the Win32 surface platform instead. */
      #ifndef MVK_VULKAN_H
      #define MVK_VULKAN_H
      #ifndef VK_USE_PLATFORM_WIN32_KHR
      #define VK_USE_PLATFORM_WIN32_KHR 1
      #endif
      #include <vulkan/vulkan.h>
      #endif
    C
    windows_exe "libexec/ucrt64/bin/spirv-cross.exe", "MoltenVKShaderConverter"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program; check the Vulkan
    # build files, then cross-compile a SPIR-V fragment shader to Metal like the shader converter.
    assert_path_exists libexec/"include/vulkan/vulkan.h"
    assert_path_exists libexec/"include/spirv_cross/spirv_msl.hpp"
    assert_path_exists libexec/"include/spirv-tools/libspirv.h"
    assert_path_exists lib/"libMoltenVK.dll.a"
    assert_match "VK_USE_PLATFORM_WIN32_KHR", (include/"MoltenVK/mvk_vulkan.h").read

    (testpath/"frag.spvasm").write <<~SPVASM
                     OpCapability Shader
                     OpMemoryModel Logical GLSL450
                     OpEntryPoint Fragment %main "main" %color
                     OpExecutionMode %main OriginUpperLeft
                     OpDecorate %color Location 0
             %void = OpTypeVoid
               %fn = OpTypeFunction %void
            %float = OpTypeFloat 32
          %v4float = OpTypeVector %float 4
      %_ptr_Output = OpTypePointer Output %v4float
            %color = OpVariable %_ptr_Output Output
          %float_1 = OpConstant %float 1
            %white = OpConstantComposite %v4float %float_1 %float_1 %float_1 %float_1
             %main = OpFunction %void None %fn
            %entry = OpLabel
                     OpStore %color %white
                     OpReturn
                     OpFunctionEnd
    SPVASM
    system libexec/"ucrt64/bin/spirv-as.exe", testpath/"frag.spvasm", "-o", testpath/"frag.spv"
    msl = shell_output("#{bin}/MoltenVKShaderConverter #{testpath}/frag.spv --msl")
    assert_match "fragment main0_out main0()", msl
    assert_match "float4(1.0)", msl
  end
end
