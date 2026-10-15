class AwsChecksums < Formula
  desc "Cross-Platform HW accelerated CRC32c and CRC32 with fallback"
  homepage "https://github.com/awslabs/aws-checksums"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-checksums-1.0.1-1-any.pkg.tar.zst"
  sha256 "339c11e15703e955ad86cb4bd69d1d4fcbfeb93d89daee90107f8c272a17c374"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-checksums/archive/refs/tags/v1.0.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/checksums/crc.h"
    assert_path_exists lib/"libaws-checksums.dll.a"
    assert_path_exists bin/"libaws-checksums.dll"
  end
end
