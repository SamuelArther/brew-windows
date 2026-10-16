class Ldns < Formula
  desc "DNS library written in C"
  homepage "https://nlnetlabs.nl/projects/ldns/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ldns-1.9.2-1-any.pkg.tar.zst"
  sha256 "48a7a6761b0326e79751db4ec71507f6e889106c324833dc0ed2f450377ea65a"
  license "BSD-3-Clause"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/NLnetLabs/ldns.git"
    regex(/^(?:release-)?v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/drill.exe", "drill"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/drill -version 2>&1")
  end
end
