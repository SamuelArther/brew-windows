class Jaq < Formula
  desc "JQ clone focussed on correctness, speed, and simplicity"
  homepage "https://github.com/01mf02/jaq"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-jaq-3.1.1-1-any.pkg.tar.zst"
  sha256 "b645e13d2d4f6250e058cf6163ccf8a0ae41bcbe12ce159d99550d785ea8c112"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/01mf02/jaq/archive/refs/tags/v3.1.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/jaq.exe", "jaq"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jaq --version 2>&1")
  end
end
