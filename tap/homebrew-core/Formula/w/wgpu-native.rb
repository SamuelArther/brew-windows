class WgpuNative < Formula
  desc "Native WebGPU implementation based on wgpu-core"
  homepage "https://github.com/gfx-rs/wgpu-native"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wgpu-native-29.0.1.1-2-any.pkg.tar.zst"
  sha256 "56e6409c97f001115936ee5422e57f153ef3ebf75b0c6dbb2e77427c202dd908"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/gfx-rs/wgpu-native.git"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"webgpu/wgpu.h"
    assert_path_exists lib/"libwgpu_native.dll.a"
    assert_path_exists bin/"wgpu_native.dll"
  end
end
