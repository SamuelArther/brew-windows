class VoAmrwbenc < Formula
  desc "Library for the VisualOn Adaptive Multi Rate Wideband (AMR-WB) audio encoder"
  homepage "https://sourceforge.net/projects/opencore-amr/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-vo-amrwbenc-0.1.3-2-any.pkg.tar.zst"
  sha256 "731b4ebecd81d8b5738a396bc8d0fddbd435c550c77876dadf6b2ce4f1d6003a"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://sourceforge.net/projects/opencore-amr/rss?path=/vo-amrwbenc"
    regex(%r{url=.*?/vo-amrwbenc[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"vo-amrwbenc/enc_if.h"
    assert_path_exists lib/"libvo-amrwbenc.dll.a"
    assert_path_exists bin/"libvo-amrwbenc-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/vo-amrwbenc.pc").read
  end
end
