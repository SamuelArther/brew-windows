class Screenfetch < Formula
  desc "Generate ASCII art with terminal, shell, and OS info"
  homepage "https://github.com/KittyKatt/screenFetch"
  url "https://repo.msys2.org/msys/x86_64/screenfetch-3.9.9-1-any.pkg.tar.zst"
  sha256 "4253de122fe38364890bdd385f361bce0a39720e49f96662c49530680f015b0d"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/KittyKatt/screenFetch/archive/refs/tags/v3.9.9.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"screenfetch"
  end
end
