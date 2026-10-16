class OpensslAT3 < Formula
  desc "Cryptography and SSL/TLS Toolkit"
  homepage "https://openssl-library.org"
  url "https://download.firedaemon.com/FireDaemon-OpenSSL/openssl-3.5.9.zip"
  sha256 "76da391395be029b44794857b9ff380255e02a0960a98314cd3f6a6816d82696"
  license "Apache-2.0"
  version_scheme 1

  # OpenSSL publishes no Windows binaries. This is FireDaemon's widely used build, listed on the
  # OpenSSL wiki's third-party binaries page (not an official OpenSSL build).

  keg_only :versioned_formula

  def install
    install_tree
    windows_exe "libexec/x64/bin/openssl.exe", "openssl"
  end

  test do
    (testpath/"testfile.txt").write("This is a test file")
    expected_checksum = "e2d0fe1585a63ec6009c8016ff8dda8b17719a637405a4e23c0ff81339148249"
    system bin/"openssl", "dgst", "-sha256", "-out", "checksum.txt", "testfile.txt"
    checksum = (testpath/"checksum.txt").read.split("=").last.strip
    assert_equal expected_checksum, checksum
  end
end
