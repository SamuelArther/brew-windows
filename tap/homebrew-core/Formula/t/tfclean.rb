class Tfclean < Formula
  desc "Remove applied moved block, import block, etc"
  homepage "https://github.com/takaishi/tfclean"
  url "https://github.com/takaishi/tfclean/releases/download/v0.0.19/tfclean_Windows_x86_64.tar.gz"
  sha256 "a6906bd5aeb4c79cb5e7bb1faac942a8297a6c10a1dc2c22943394b1a161707a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/takaishi/tfclean/archive/refs/tags/v0.0.19.tar.gz"
  end

  def install
    install_tree
  end

  test do
    assert_path_exists prefix
  end
end
