class GitOctopus < Formula
  desc "Continuous merge workflow"
  homepage "https://github.com/lesfurets/git-octopus"
  url "https://github.com/lesfurets/git-octopus/archive/refs/tags/v1.4.tar.gz"
  sha256 "e2800eea829c6fc74da0d3f3fcb3f7d328d1ac8fbb7b2eca8c651c0c903a50c3"
  license "LGPL-3.0-only"

  deprecate! date: "2025-12-14", because: :repo_archived
  disable! date: "2026-12-14", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "2dde1b25b013122b7363ea60d8c186d159c085f88385894f3fe5f47910fa5c5e"
  end

  def install
    system "make", "build"
    bin.install "bin/git-octopus", "bin/git-conflict", "bin/git-apply-conflict-resolution"
    man1.install "doc/git-octopus.1", "doc/git-conflict.1"
  end

  test do
    system "git", "init"
    touch "homebrew"
    system "git", "add", "."
    system "git", "commit", "--message", "brewing"

    assert_empty shell_output("#{bin}/git-octopus 2>&1").strip
  end
end
