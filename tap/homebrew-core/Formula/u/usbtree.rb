class Usbtree < Formula
  desc "Live USB device tree in your terminal"
  homepage "https://gnomeria.github.io/usbtree/"
  url "https://github.com/gnomeria/usbtree/releases/download/v0.1.1/usbtree_0.1.1_windows-amd64.zip"
  sha256 "2d315b069fc3bf51a11911a2d4b44be0f4f48e3b5f9e10f47c7109a45b182e4d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gnomeria/usbtree/archive/refs/tags/v0.1.1.tar.gz"
  end

  def install
    bin.install "usbtree.exe"
  end

  test do
    assert_match "controller", shell_output("#{bin}/usbtree --pci")
  end
end
