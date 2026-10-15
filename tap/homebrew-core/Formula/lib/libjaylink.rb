class Libjaylink < Formula
  desc "Provide interoperability with JLINK hardware"
  homepage "https://gitlab.zapb.de/libjaylink/libjaylink"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libjaylink-0.5.0-1-any.pkg.tar.zst"
  sha256 "4c8bf13fb702867eda21308b2156e4e654b7880a047c42b24574e88bbf6f8854"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gitlab.zapb.de/libjaylink/libjaylink/-/archive/0.5.0/libjaylink-0.5.0.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libjaylink/version.h"
    assert_path_exists lib/"libjaylink.dll.a"
    assert_path_exists bin/"libjaylink-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libjaylink.pc").read
  end
end
