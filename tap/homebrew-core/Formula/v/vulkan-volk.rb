class VulkanVolk < Formula
  desc "Meta loader for Vulkan API"
  homepage "https://github.com/zeux/volk"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vulkan-volk-1.4.363.0-1-any.pkg.tar.zst"
  sha256 "72824d43aeedf4f361e4afaed8ad3221556e4d72bea0be0da0e2e2f34b7b4dac"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/zeux/volk/archive/refs/tags/vulkan-sdk-1.4.363.0.tar.gz"
    regex(/^(?:vulkan[._-])?sdk[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  conflicts_with "volk", because: "both install volkConfig.cmake"

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"volk.h"
  end
end
