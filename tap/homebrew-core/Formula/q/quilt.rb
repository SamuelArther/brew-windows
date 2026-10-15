class Quilt < Formula
  desc "Work with series of patches"
  homepage "https://savannah.nongnu.org/projects/quilt"
  url "https://repo.msys2.org/msys/x86_64/quilt-0.69-2-any.pkg.tar.zst"
  sha256 "28f9051aa5582fd5c6f4a03593ac2a2f2c0e378d0b2b80ab0a484785264287de"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.savannah.gnu.org/releases/quilt/"
    regex(/href=.*?quilt[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"guards"
  end
end
