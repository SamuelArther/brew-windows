class Xevd < Formula
  desc "Very fast Essential Video Decoder, MPEG-5 EVC (Essential Video Coding)"
  homepage "https://github.com/mpeg5/xevd"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xevd-0.5.0-2-any.pkg.tar.zst"
  sha256 "e16dd82273293938c44b212785e6835c021c79b5a489db497f9532a8bf586d17"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/mpeg5/xevd/archive/refs/tags/v0.8.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/xevd_app.exe", "xevd_app"
  end

  test do
    assert_path_exists bin/"xevd_app.exe"
  end
end
