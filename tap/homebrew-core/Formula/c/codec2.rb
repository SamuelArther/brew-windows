class Codec2 < Formula
  desc "Open source speech codec"
  homepage "https://www.rowetel.com/?page_id=452"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-codec2-1.2.0-2-any.pkg.tar.zst"
  sha256 "11a85ff886cc34f85cb8c9f2ceaeb7809a9d601ebec068305998a1962bbfec41"
  license "LGPL-2.1-only"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/drowe67/codec2/archive/refs/tags/1.2.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
    strategy :git do |tags, regex|
      malformed_tags = ["v1.03"].freeze
      tags.map do |tag|
        next if malformed_tags.include?(tag)

        tag[regex, 1]
      end
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"codec2/fsk.h"
    assert_path_exists lib/"libcodec2.dll.a"
    assert_path_exists bin/"libcodec2.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/codec2.pc").read
  end
end
