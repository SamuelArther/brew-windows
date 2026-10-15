class Mujs < Formula
  desc "Embeddable Javascript interpreter"
  homepage "https://www.mujs.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mujs-1.3.10-1-any.pkg.tar.zst"
  sha256 "680d989b5cbb85c927c0e2a5c7333cec866a6bde8bc9be94e90212b1dd15ab64"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://mujs.com/downloads/"
    regex(/href=.*?mujs[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mujs-pp.exe", "mujs-pp"
    msys2_bin "ucrt64/bin/mujs.exe", "mujs"
  end

  test do
    assert_path_exists bin/"mujs.exe"
  end
end
