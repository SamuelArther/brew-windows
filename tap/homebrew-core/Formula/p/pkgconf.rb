class Pkgconf < Formula
  desc "Package compiler and linker metadata toolkit"
  homepage "https://github.com/pkgconf/pkgconf"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pkgconf-1~3.0.8-1-any.pkg.tar.zst"
  version "3.0.8"
  sha256 "d3f54c0ce414bf3ecf9792417d4e49075881a7ce2c5149d5107c3e00ead4ed92"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://distfiles.ariadne.space/pkgconf/"
    regex(/href=.*?pkgconf[._-]v?(\d+\.\d+(?:\.[1-8]?\d(?:\.\d+)*)?)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/bomtool.exe", "bomtool"
    msys2_bin "ucrt64/bin/pccritic.exe", "pccritic"
    msys2_bin "ucrt64/bin/pkg-config.exe", "pkg-config"
    msys2_bin "ucrt64/bin/pkgconf.exe", "pkgconf"
    msys2_bin "ucrt64/bin/spdxtool.exe", "spdxtool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pkgconf --version 2>&1")
  end
end
