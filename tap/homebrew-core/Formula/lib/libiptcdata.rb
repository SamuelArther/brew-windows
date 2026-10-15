class Libiptcdata < Formula
  desc "Virtual package provided by libiptcdata0"
  homepage "https://libiptcdata.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiptcdata-1.0.5-2-any.pkg.tar.zst"
  sha256 "281d62b3aba2fb008c1ad2f04a522b1e5ed4fed489b42d33d907844aec5a5a9c"
  license "LGPL-2.0-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ianw/libiptcdata/releases/download/release_1_0_5/libiptcdata-1.0.5.tar.gz"
    regex(/^release[._-]v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.filter_map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libiptcdata/_stdint.h"
    assert_path_exists lib/"libiptcdata.dll.a"
    assert_path_exists bin/"libiptcdata-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libiptcdata.pc").read
  end
end
