class Onnx < Formula
  desc "Open standard for machine learning interoperability"
  homepage "https://onnx.ai/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-onnx-1.21.0-6-any.pkg.tar.zst"
  sha256 "ce02b74fe22ddeb9a6cc856a3f49a2d78d963186555d1a3b0d6759646363ee13"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/onnx/onnx/archive/refs/tags/v1.23.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"onnx/checker.h"
    assert_path_exists lib/"libonnx.dll.a"
    assert_path_exists bin/"libonnx.dll"
  end
end
