class Stow < Formula
  desc "Organize software neatly under a single directory tree (e.g. /usr/local)"
  homepage "https://www.gnu.org/software/stow/"
  url "https://repo.msys2.org/msys/x86_64/stow-2.4.1-1-any.pkg.tar.zst"
  sha256 "9bbf04ce78429c6db9d1fe315c6babbcf446a3acf650a213412836f3c53cf1dc"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/stow/stow-2.4.1.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"chkstow"
  end
end
