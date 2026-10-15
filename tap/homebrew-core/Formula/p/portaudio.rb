class Portaudio < Formula
  desc "Cross-platform library for audio I/O"
  homepage "https://www.portaudio.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-portaudio-1~19.7.0-7-any.pkg.tar.zst"
  version "19.7.0"
  sha256 "085e0ba89a659e6f66379552f7fb736b182ecaa7caf710d611e481f557e20c38"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://files.portaudio.com/download.html"
    regex(/href=.*?pa[._-]stable[._-]v?(\d+)(?:[._-]\d+)?\.t/i)
    strategy :page_match do |page, regex|
      # Modify filename version (190700) to match formula version (19.7.0)
      page.scan(regex).map { |match| match[0].scan(/\d{2}/).map(&:to_i).join(".") }
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pa_win_ds.h"
    assert_path_exists lib/"libportaudio.dll.a"
    assert_path_exists bin/"libportaudio.dll"
  end
end
