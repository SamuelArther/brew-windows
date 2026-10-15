class Onnxruntime < Formula
  desc "Cross-platform, high performance scoring engine for ML models"
  homepage "https://github.com/microsoft/onnxruntime"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-onnxruntime-1.29.0-2-any.pkg.tar.zst"
  sha256 "1eb0088472f374722deed5fdfd1f6e5f10d1d9c5ac37da0ca0b505637a3dc03c"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/microsoft/onnxruntime/archive/refs/tags/v1.31.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"onnxruntime/provider_options.h"
    assert_path_exists lib/"libonnxruntime.dll.a"
    assert_path_exists bin/"onnxruntime.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libonnxruntime.pc").read
  end
end
