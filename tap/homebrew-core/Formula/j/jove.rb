class Jove < Formula
  desc "Emacs-style editor with vi-like memory, CPU, and size requirements"
  homepage "https://directory.fsf.org/wiki/Jove"
  url "https://github.com/jonmacs/jove/releases/download/4.17.5.5/jove-4.17.5.5-x86_64-w64-mingw32.zip"
  sha256 "c1c5c65ff63db63720c0fa2449f3cded63b73d769515b8c291ee73afcc59da6d"
  license "jove"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jonmacs/jove/archive/refs/tags/4.17.5.5.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/jove.exe", "jove"
  end

  test do
    assert_path_exists bin/"jove.exe"
  end
end
