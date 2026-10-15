class Unbound < Formula
  desc "Validating, recursive, caching DNS resolver"
  homepage "https://www.unbound.net"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unbound-1.26.1-1-any.pkg.tar.zst"
  sha256 "a04080b54fc7c209f7037c41a3ce7433b1b7d2998d5c39e78a53108c27d1369a"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :head
    regex(/^(?:release-)?v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-ldns" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ldns-1.9.2-1-any.pkg.tar.zst"
    sha256 "48a7a6761b0326e79751db4ec71507f6e889106c324833dc0ed2f450377ea65a"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/unbound-anchor.exe", "unbound-anchor"
    msys2_bin "ucrt64/bin/unbound-checkconf.exe", "unbound-checkconf"
    msys2_bin "ucrt64/bin/unbound-control.exe", "unbound-control"
    msys2_bin "ucrt64/bin/unbound-host.exe", "unbound-host"
    msys2_bin "ucrt64/bin/unbound.exe", "unbound"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/unbound -V 2>&1")
  end
end
