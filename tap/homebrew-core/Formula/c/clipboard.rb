class Clipboard < Formula
  desc "Cut, copy, and paste anything, anywhere, all from the terminal"
  homepage "https://getclipboard.app"
  url "https://github.com/Slackadays/Clipboard/releases/download/0.10.0/clipboard-windows-amd64.zip"
  sha256 "ec40414e692b4ee69a98f7ee89d5ce5ca4bbbf44211823726531da82815fa2ab"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Slackadays/Clipboard/archive/refs/tags/0.10.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/cb.exe", "cb"
  end

  test do
    assert_path_exists bin/"cb.exe"
  end
end
