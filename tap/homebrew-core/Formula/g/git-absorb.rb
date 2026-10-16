class GitAbsorb < Formula
  desc "Automatic git commit --fixup"
  homepage "https://github.com/tummychow/git-absorb"
  url "https://github.com/tummychow/git-absorb/releases/download/0.9.0/git-absorb-0.9.0-x86_64-pc-windows-gnu.zip"
  sha256 "39ac387080c459990dc6d0e440c525fd4fc61ece2aa8fb621fc5b0e0904316da"
  license "BSD-3-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tummychow/git-absorb/archive/refs/tags/0.9.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/git-absorb.exe", "git-absorb"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-absorb --version 2>&1")
  end
end
