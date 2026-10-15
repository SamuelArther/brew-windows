class Libressl < Formula
  desc "Version of the SSL/TLS protocol forked from OpenSSL"
  homepage "https://www.libressl.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libressl-4.3.2-1-any.pkg.tar.zst"
  sha256 "efa89623347875c3831af5079ff7fb1a001403a2f231a77b1d1aa72437f3d497"
  license "OpenSSL"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/latest stable release is (\d+(?:\.\d+)+)/i)
  end

  keg_only "it conflicts with OpenSSL"

  def install
    msys2_install
    msys2_bin "ucrt64/bin/libressl-ocspcheck.exe", "libressl-ocspcheck"
    msys2_bin "ucrt64/bin/libressl-openssl.exe", "libressl-openssl"
  end

  test do
    assert_path_exists bin/"libressl-ocspcheck.exe"
  end
end
