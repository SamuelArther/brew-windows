class AwsCMqtt < Formula
  desc "C99 implementation of the MQTT 3.1.1 specification"
  homepage "https://github.com/awslabs/aws-c-mqtt"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-aws-c-mqtt-1.0.0-1-any.pkg.tar.zst"
  sha256 "0175e4ce24c222f54795b6598182b3171016124b8810536ecb0360ca91532c8b"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/awslabs/aws-c-mqtt/archive/refs/tags/v1.1.2.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"aws/mqtt/mqtt.h"
    assert_path_exists lib/"libaws-c-mqtt.dll.a"
    assert_path_exists bin/"libaws-c-mqtt.dll"
  end
end
