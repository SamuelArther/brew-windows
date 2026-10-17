class GitCodereview < Formula
  desc "Tool for working with Gerrit code reviews"
  homepage "https://pkg.go.dev/golang.org/x/review/git-codereview"
  url "https://github.com/golang/review/archive/refs/tags/v1.21.0.tar.gz"
  sha256 "d1215854ba15b8f7d457bb00637f8f137f775004e5a6568dab82d27755dbd587"
  license "BSD-3-Clause"
  head "https://github.com/golang/review.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4d3eb9e581fffe94f0fffd521e1ec78b5a2284f0da30dcd9ecadd57181a2f387"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args, "./git-codereview"
  end

  test do
    system "git", "init"
    system "git", "codereview", "hooks"
    assert_match "git-codereview hook-invoke", (testpath/".git/hooks/commit-msg").read
  end
end
