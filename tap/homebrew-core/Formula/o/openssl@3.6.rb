class OpensslAT36 < Formula
  desc "Cryptography and SSL/TLS Toolkit"
  homepage "https://openssl-library.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
  sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://openssl-library.org/source/"
    regex(/href=.*?openssl[._-]v?(3\.6(?:\.\d+)+)\.t/i)
  end

  keg_only :versioned_formula

  def install
    msys2_install
    msys2_bin "ucrt64/bin/openssl.exe", "openssl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/openssl --version 2>&1")
  end
end
