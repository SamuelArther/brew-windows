class Ggml < Formula
  desc "Tensor library for machine learning"
  homepage "https://github.com/ggml-org/ggml"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ggml-0.26.0-1-any.pkg.tar.zst"
  sha256 "53a981dcca3b0f2f2124b598bce4c033467b0f2365f092300999963e2e412c82"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ggml-org/ggml/archive/refs/tags/v0.27.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ggml.h"
    assert_path_exists lib/"libggml-base.dll.a"
    assert_path_exists bin/"ggml-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ggml.pc").read
  end
end
