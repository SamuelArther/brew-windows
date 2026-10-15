class Lf < Formula
  desc "Terminal file manager"
  homepage "https://godoc.org/github.com/gokcehan/lf"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lf-42-1-any.pkg.tar.zst"
  version "42"
  sha256 "f02f888171dd5c4e9d970c106464dab2c06a66b4906f2045aa2c6295bf627ec4"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/gokcehan/lf/archive/refs/tags/r42.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/lf.exe", "lf"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/lf -version").chomp
    assert_match "file manager", shell_output("#{bin}/lf -doc")
  end
end
