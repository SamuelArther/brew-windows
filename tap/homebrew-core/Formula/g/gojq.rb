class Gojq < Formula
  desc "Pure Go implementation of jq"
  homepage "https://github.com/itchyny/gojq"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gojq-0.12.19-2-any.pkg.tar.zst"
  sha256 "a16d10c01eda195af920037b7eacc052ccd796cc6c2e1451193082c2a995714b"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/itchyny/gojq.git"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/gojq.exe", "gojq"
  end

  test do
    assert_path_exists bin/"gojq.exe"
  end
end
