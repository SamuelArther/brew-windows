class Librdkafka < Formula
  desc "Apache Kafka C/C++ library"
  homepage "https://github.com/confluentinc/librdkafka"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librdkafka-2.15.0-1-any.pkg.tar.zst"
  sha256 "3fa0a139bfc0d171b20417d1f36ed90a1e613dd17b971c67097b6e1918ad9c46"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/confluentinc/librdkafka/archive/refs/tags/v2.16.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"librdkafka/rdkafka.h"
    assert_path_exists lib/"librdkafka++.dll.a"
    assert_path_exists bin/"librdkafka++.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/rdkafka++.pc").read
  end
end
