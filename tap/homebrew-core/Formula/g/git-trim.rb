class GitTrim < Formula
  desc "Trim your git remote tracking branches that are merged or gone"
  homepage "https://github.com/foriequal0/git-trim"
  url "https://github.com/foriequal0/git-trim/releases/download/v0.4.3/git-trim-win-v0.4.3.tgz"
  sha256 "6bc6079e664121fe8c0add605c258c6f985f6ee4517fa49589c049014fc6c480"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/foriequal0/git-trim.git"
  end

  def install
    install_tree
    windows_exe "libexec/git-trim.exe", "git-trim"
  end

  test do
    system "git", "clone", "https://github.com/foriequal0/git-trim"
    Dir.chdir("git-trim")
    system "git", "branch", "brew-test"
    assert_match "brew-test", shell_output("git trim")
  end
end
