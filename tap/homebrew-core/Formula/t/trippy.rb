class Trippy < Formula
  desc "Network diagnostic tool, inspired by mtr"
  homepage "https://trippy.rs/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-trippy-0.13.0-1-any.pkg.tar.zst"
  sha256 "ca7a481a5585d4e288479872c5a490c87d2432f13d739308c635036c71f103f0"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/fujiapple852/trippy/archive/refs/tags/0.13.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/trip.exe", "trip"
  end

  test do
    # https://github.com/fujiapple852/trippy#privileges
    expected = "Error: privileges are required"
    output = shell_output("#{bin}/trip brew.sh 2>&1", 1)
    assert_match expected, output

    assert_match "trip #{version}", shell_output("#{bin}/trip --version")
  end
end
