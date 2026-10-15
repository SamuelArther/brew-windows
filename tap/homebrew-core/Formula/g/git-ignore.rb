class GitIgnore < Formula
  desc "List, fetch and generate .gitignore templates"
  homepage "https://github.com/sondr3/git-ignore"
  url "https://github.com/sondr3/git-ignore/releases/download/v1.4.0/git-ignore-x86_64-pc-windows-msvc.zip"
  sha256 "a9ad680aaec04e2df54090da23d98b47aa952b6e270f6d53a73ad7c23d1e8f6a"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sondr3/git-ignore/archive/refs/tags/v1.4.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/git-ignore.exe", "git-ignore"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-ignore --version 2>&1")
  end
end
