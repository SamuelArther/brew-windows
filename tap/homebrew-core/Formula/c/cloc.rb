class Cloc < Formula
  desc "Statistics utility to count lines of code"
  homepage "https://github.com/AlDanial/cloc/"
  url "https://repo.msys2.org/msys/x86_64/cloc-2.10-2-any.pkg.tar.zst"
  sha256 "c1f808f5d23e52563d584c9db1d57441a1b5d0febdaa4e017d59fbfb140d7406"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/AlDanial/cloc/archive/refs/tags/v2.10.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"cloc"
  end
end
