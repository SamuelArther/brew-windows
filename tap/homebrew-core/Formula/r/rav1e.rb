class Rav1e < Formula
  desc "Fastest and safest AV1 video encoder"
  homepage "https://github.com/xiph/rav1e"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-rav1e-0.8.1-1-any.pkg.tar.zst"
  sha256 "00a03a734f96f66405ad79bae2ffb1c5cf2b050ef90a4ffa508ddbfdb64c0fb1"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/xiph/rav1e/archive/refs/tags/v0.8.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/rav1e.exe", "rav1e"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rav1e --version 2>&1")
  end
end
