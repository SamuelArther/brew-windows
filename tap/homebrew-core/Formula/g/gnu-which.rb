class GnuWhich < Formula
  desc "GNU implementation of which utility"
  homepage "https://savannah.gnu.org/projects/which/"
  url "https://repo.msys2.org/msys/x86_64/which-2.25-1-x86_64.pkg.tar.zst"
  sha256 "a299e7b9076e5d9381e59fd67538aa39346eb36bb16a93834fb99cccc3f2055b"
  license "GPL-3.0-or-later"

  # GNU which via MSYS2, exposed as which and gwhich.

  resource "bash" do
    url "https://repo.msys2.org/msys/x86_64/bash-5.3.20-2-x86_64.pkg.tar.zst"
    sha256 "0951e809c552dd7dd2835df1b2d590822187f14e717d809e942e4eb677d0adfd"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/which.exe", "which"
    msys2_bin "usr/bin/which.exe", "gwhich"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gwhich --version")
  end
end
