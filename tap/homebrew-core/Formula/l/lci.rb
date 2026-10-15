class Lci < Formula
  desc "Interpreter for the lambda calculus"
  homepage "https://www.chatzi.org/lci/"
  url "https://github.com/chatziko/lci/releases/download/v1.1/lci-v1.1-win.zip"
  sha256 "ae69077f8d1a03a879c02ccfcfdf94b7dcb65400f0ab59bfbee5c66ffa215f89"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/chatziko/lci/releases/download/v1.1/lci-v1.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/lci.exe", "lci"
  end

  test do
    system bin/"lci", "--version"
  end
end
