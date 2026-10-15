class Gdu < Formula
  desc "Disk usage analyzer with console interface written in Go"
  homepage "https://github.com/dundee/gdu"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gdu-5.38.0-1-any.pkg.tar.zst"
  sha256 "350bade831794d14be8d83f0da832ec0a1226153e3c40c5ca8c5454376394710"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/dundee/gdu/archive/refs/tags/v5.38.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/gdu.exe", "gdu"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gdu --version 2>&1")
  end
end
