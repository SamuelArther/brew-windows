class Spandsp < Formula
  desc "DSP functions library for telephony"
  homepage "https://www.soft-switch.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-spandsp-0.0.6-6-any.pkg.tar.zst"
  sha256 "ddd9cca98e7ad05e9cb83f3ca404db8eba601d20825dbbbc4605e21e8cbe430b"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.soft-switch.org/downloads/spandsp/"
    regex(/href=.*?spandsp[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"spandsp.h"
    assert_path_exists lib/"libspandsp.dll.a"
    assert_path_exists bin/"libspandsp-2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/spandsp.pc").read
  end
end
