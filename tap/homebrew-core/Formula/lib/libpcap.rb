class Libpcap < Formula
  desc "Portable library for network traffic capture"
  homepage "https://www.tcpdump.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpcap-1.11.0-1-any.pkg.tar.zst"
  sha256 "420a18bc6d20015c82ba97558dfbc6e3c18670caa7aaf321ca78fc888c31e9a1"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.tcpdump.org/release/"
    regex(/href=.*?libpcap[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pcap.h"
    assert_path_exists lib/"libpcap.dll.a"
    assert_path_exists bin/"libpcap.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libpcap.pc").read
  end
end
