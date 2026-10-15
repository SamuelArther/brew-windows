class AwsCrtCpp < Formula
  desc "C++ wrapper around the aws-c-* libraries"
  homepage "https://github.com/awslabs/aws-crt-cpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-crt-cpp-0.43.7-1-any.pkg.tar.zst"
  sha256 "5b9612c8c9ae7d971daa467f3be1d8d4917ae4e72d536987b5c21b778df5458d"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-crt-cpp/archive/refs/tags/v0.43.9.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/crt/Api.h"
    assert_path_exists lib/"libaws-crt-cpp.dll.a"
    assert_path_exists bin/"libaws-crt-cpp.dll"
  end
end
