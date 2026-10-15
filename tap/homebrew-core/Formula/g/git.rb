class Git < Formula
  desc "Distributed revision control system"
  homepage "https://git-scm.com/"
  version "2.56.0"
  url "https://github.com/git-for-windows/git/releases/download/v2.56.0.windows.2/MinGit-2.56.0.2-64-bit.zip"
  sha256 "da35e72aa21c005a5a0d298cfbae110bc1609a815730ea0dde84b01a1b3cd3be"
  license "GPL-2.0-only"

  # MinGit, the official minimal Git for Windows distribution; GUI tools are omitted.

  def install
    install_tree
    windows_exe "libexec/cmd/git.exe", "git"
  end
end
