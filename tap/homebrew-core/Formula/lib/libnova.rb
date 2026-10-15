class Libnova < Formula
  desc "Celestial mechanics, astrometry and astrodynamics library"
  homepage "https://libnova.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libnova-0.15.0-3-any.pkg.tar.zst"
  sha256 "ba53695497cf589e734865d6203c1c8f2e26a0b4519883213cb4876ce8391cee"
  license all_of: ["LGPL-2.0-or-later", "GPL-2.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://git.code.sf.net/p/libnova/libnova.git"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/libnovaconfig.exe", "libnovaconfig"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/libnovaconfig --version 2>&1")
  end
end
