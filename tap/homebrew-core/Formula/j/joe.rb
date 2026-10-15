class Joe < Formula
  desc "Full featured terminal-based screen editor"
  homepage "https://joe-editor.sourceforge.io/"
  url "https://repo.msys2.org/msys/x86_64/joe-4.8-1-any.pkg.tar.zst"
  sha256 "2cc1cedf22bd0d85ac0d34b0dc3781cd967ae6d979bdbe76c6b00fa28803d2fc"
  license "GPL-1.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://downloads.sourceforge.net/project/joe-editor/JOE%20sources/joe-4.8/joe-4.8.tar.gz"
    regex(%r{url=.*?/joe[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/jmacs.exe", "jmacs"
    msys2_bin "usr/bin/joe.exe", "joe"
    msys2_bin "usr/bin/jpico.exe", "jpico"
    msys2_bin "usr/bin/jstar.exe", "jstar"
    msys2_bin "usr/bin/rjoe.exe", "rjoe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/joe --version 2>&1")
  end
end
