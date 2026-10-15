class Fq < Formula
  desc "Brokered message queue optimized for performance"
  homepage "https://github.com/circonus-labs/fq"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fq-0.18.0-1-any.pkg.tar.zst"
  sha256 "9f8af36a61f0ab9b62347c4960e3a1dccb30b033d63c73e7e896800aef1a3111"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/circonus-labs/fq/archive/refs/tags/v0.13.12.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/fq.exe", "fq"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fq --version 2>&1")
  end
end
