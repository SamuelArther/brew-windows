class Chicken < Formula
  desc "Compiler for the Scheme programming language"
  homepage "https://www.call-cc.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-chicken-5.4.0-1-any.pkg.tar.zst"
  sha256 "56b8e231ce00fa5812d4d2b77f8a5d288298596c71bd0316980ca36ffd94302e"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://code.call-cc.org/releases/current/"
    regex(/href=.*?chicken[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/chicken-do.exe", "chicken-do"
    msys2_bin "ucrt64/bin/chicken-install.exe", "chicken-install"
    msys2_bin "ucrt64/bin/chicken-profile.exe", "chicken-profile"
    msys2_bin "ucrt64/bin/chicken-status.exe", "chicken-status"
    msys2_bin "ucrt64/bin/chicken-uninstall.exe", "chicken-uninstall"
    msys2_bin "ucrt64/bin/chicken.exe", "chicken"
    msys2_bin "ucrt64/bin/csc.exe", "csc"
    msys2_bin "ucrt64/bin/csi.exe", "csi"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/chicken --version 2>&1")
  end
end
