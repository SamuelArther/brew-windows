class Orc < Formula
  desc "Oil Runtime Compiler (ORC)"
  homepage "https://gstreamer.freedesktop.org/modules/orc.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-orc-0.4.44-1-any.pkg.tar.zst"
  sha256 "f333a8207fbec0178e34d6293e412015c7450e3e73dfa1627279cd922f5d622f"
  license all_of: ["BSD-2-Clause", "BSD-3-Clause"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://gstreamer.freedesktop.org/src/orc/"
    regex(/href=.*?orc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/orc-bugreport.exe", "orc-bugreport"
    msys2_bin "ucrt64/bin/orcc.exe", "orcc"
  end

  test do
    system bin/"orc-bugreport", "--help"
  end
end
