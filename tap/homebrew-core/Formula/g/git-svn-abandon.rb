class GitSvnAbandon < Formula
  desc "History-preserving svn-to-git migration"
  homepage "https://github.com/nothingmuch/git-svn-abandon"
  url "https://github.com/nothingmuch/git-svn-abandon/archive/refs/tags/0.0.1.tar.gz"
  sha256 "65c11b5e575e6af4d21ef7624941c4581a5570748d50e38714bd33fee56e4485"
  license "MIT"
  head "https://github.com/nothingmuch/git-svn-abandon.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-svn-abandon-0.0.1"
    sha256 cellar: :any, x86_64_windows: "b3683c39d396d7ebccb75c133a9e91ed392dc0d764d712d690cb7ef9d00bd2d2"
  end

  def install
    bin.install Dir["git-svn-abandon-*"]
  end

  test do
    system "git", "init"
    system "git", "symbolic-ref", "HEAD", "refs/heads/trunk"
    system "git", "commit", "--allow-empty", "-m", "foo"
    system "git", "svn-abandon-fix-refs"
    assert_equal "* master", shell_output("git branch -a").chomp
  end
end
