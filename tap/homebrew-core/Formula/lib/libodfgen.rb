class Libodfgen < Formula
  desc "ODF export library for projects using librevenge"
  homepage "https://sourceforge.net/p/libwpd/wiki/libodfgen/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libodfgen-0.1.8-8-any.pkg.tar.zst"
  sha256 "e0b7a2e03ec7dd77f31ea35fb53414d5fd7c695386b053cded4cc392d7094df9"
  license any_of: ["MPL-2.0", "LGPL-2.1-or-later"]
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://dev-www.libreoffice.org/src/"
    regex(/href=["']?libodfgen[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libodfgen-0.1/libodfgen/libodfgen.hxx"
    assert_path_exists lib/"libodfgen-0.1.dll.a"
    assert_path_exists bin/"libodfgen-0.1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libodfgen-0.1.pc").read
  end
end
