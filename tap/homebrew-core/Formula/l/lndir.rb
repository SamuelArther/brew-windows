class Lndir < Formula
  desc "Create a shadow directory of symbolic links to another directory tree"
  homepage "https://gitlab.freedesktop.org/xorg/util/lndir"
  url "https://repo.msys2.org/msys/x86_64/lndir-1.0.6-1-x86_64.pkg.tar.zst"
  sha256 "cbd991ad0e1c5bc119355012b2db5387dd17a7624ca2a9aa9ab1aa9b7385731d"
  license "MIT-open-group"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://www.x.org/releases/individual/util/lndir-1.0.6.tar.xz"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/lndir.exe", "lndir"
  end

  test do
    mkdir "test"
    system bin/"lndir", bin, "test"
    assert_path_exists testpath/"test/lndir"
  end
end
