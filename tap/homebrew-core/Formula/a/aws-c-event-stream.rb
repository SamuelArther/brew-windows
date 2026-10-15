class AwsCEventStream < Formula
  desc "C99 implementation of the vnd.amazon.eventstream content-type"
  homepage "https://github.com/awslabs/aws-c-event-stream"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-event-stream-1.0.0-1-any.pkg.tar.zst"
  sha256 "546d1f36e78c90b02e90f953f42dbb063f4674bead2dddc7615b07b40980c549"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-event-stream/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/event-stream/event_stream.h"
    assert_path_exists lib/"libaws-c-event-stream.dll.a"
    assert_path_exists bin/"libaws-c-event-stream.dll"
  end
end
