class SvtAv1 < Formula
  desc "AV1 encoder"
  homepage "https://gitlab.com/AOMediaCodec/SVT-AV1"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-svt-av1-4.2.0-1-any.pkg.tar.zst"
  sha256 "87c97a68bab7dd67413ea0cae024c3d2e4bdc2f96fccf38365304642b1b44ee5"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://gitlab.com/AOMediaCodec/SVT-AV1/-/archive/v4.2.0/SVT-AV1-v4.2.0.tar.bz2"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/SvtAv1EncApp.exe", "SvtAv1EncApp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/SvtAv1EncApp --version 2>&1")
  end
end
