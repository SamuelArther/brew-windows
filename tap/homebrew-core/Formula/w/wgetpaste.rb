class Wgetpaste < Formula
  desc "Automate pasting to a number of pastebin services"
  homepage "https://wgetpaste.zlin.dk/"
  url "https://github.com/zlin/wgetpaste/releases/download/2.34/wgetpaste-2.34.tar.xz"
  sha256 "bd6d06ed901a3d63c9c8c57126084ff0994f09901bfb1638b87953eac1e9433f"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "edb6abbb6a7a995a30df43c2acbc70773cb754712e1fe3ddfe05d27464c9eb42"
  end

  depends_on "wget"

  def install
    bin.install "wgetpaste"
    zsh_completion.install "_wgetpaste"
  end

  test do
    system bin/"wgetpaste", "-S"
  end
end
