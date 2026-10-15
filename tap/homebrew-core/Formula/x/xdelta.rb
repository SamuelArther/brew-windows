class Xdelta < Formula
  desc "Binary diff, differential compression tools"
  homepage "https://github.com/jmacd/xdelta"
  url "https://github.com/jmacd/xdelta/releases/download/v3.2.2/xdelta3-3.2.2-windows-x86_64.zip"
  sha256 "d48f7651d86b78fdca397057b46601f89131f37219675df736db70af44ec3379"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jmacd/xdelta/archive/refs/tags/v3.2.2.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/xdelta3.exe", "xdelta3"
  end

  test do
    system bin/"xdelta3", "config"
  end
end
