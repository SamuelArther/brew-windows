class CrosstoolNg < Formula
  desc "Tool for building toolchains"
  homepage "https://crosstool-ng.github.io/"
  url "https://repo.msys2.org/msys/x86_64/crosstool-ng-1.28.0-1-x86_64.pkg.tar.zst"
  sha256 "a6aaad832bd6fb07e7ed2d3cf8339075e03a710a465c9992f78f756188231de6"
  license "GPL-2.0-only"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://crosstool-ng.github.io/download/"
    regex(/href=.*?crosstool-ng[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"ct-ng"
  end
end
