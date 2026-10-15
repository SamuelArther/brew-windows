class Lzop < Formula
  desc "File compressor"
  homepage "https://www.lzop.org/"
  url "https://repo.msys2.org/msys/x86_64/lzop-1.04-2-x86_64.pkg.tar.zst"
  sha256 "78451d6636f3aefda6f14eadf850a69fa71b71d68b22f808cf7778d5e2613495"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://www.lzop.org/download/"
    regex(/href=.*?lzop[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "liblzo2" do
    url "https://repo.msys2.org/msys/x86_64/liblzo2-2.10-3-x86_64.pkg.tar.zst"
    sha256 "5881251a3b73ebf7a6b2491e092bafbd62a44925d538d69d0776903d26119794"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/lzop.exe", "lzop"
  end

  test do
    path = testpath/"test"
    text = "This is Homebrew"
    path.write text

    system bin/"lzop", "test"
    assert_path_exists testpath/"test.lzo"
    rm path

    system bin/"lzop", "-d", "test.lzo"
    assert_equal text, path.read
  end
end
