class Fcrackzip < Formula
  desc "Zip password cracker"
  homepage "https://oldhome.schmorp.de/marc/fcrackzip.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fcrackzip-1.0-2-any.pkg.tar.zst"
  sha256 "548a537a5e345b0e74c38eccf3d6a96c2f5a5fa87a4069d16b30759288bf92b5"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://oldhome.schmorp.de/marc/data/"
    regex(/href=.*?fcrackzip[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/fcrackzip.exe", "fcrackzip"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fcrackzip --version 2>&1")
  end
end
