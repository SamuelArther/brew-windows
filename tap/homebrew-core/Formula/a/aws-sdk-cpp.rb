class AwsSdkCpp < Formula
  desc "AWS SDK for C++"
  homepage "https://github.com/aws/aws-sdk-cpp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-sdk-cpp-1.11.893-1-any.pkg.tar.zst"
  sha256 "f2dc1b6d84eabb300e88e324ff9a5fe44a6d8c1aac20f3d6b4a380a5df57cae6"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    throttle 15
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"smithy/Smithy_EXPORTS.h"
    assert_path_exists lib/"libaws-cpp-sdk-access-management.dll.a"
    assert_path_exists bin/"libaws-cpp-sdk-access-management.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/aws-cpp-sdk-access-management.pc").read
  end
end
