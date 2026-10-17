class Gitbatch < Formula
  desc "Manage your git repositories in one place"
  homepage "https://github.com/isacikgoz/gitbatch"
  url "https://github.com/isacikgoz/gitbatch/archive/refs/tags/v0.6.1.tar.gz"
  sha256 "0ef36a4ea0b6cf4beb51928dd51281ec106006ba800c439d2588515c1bfeaf41"
  license "MIT"
  head "https://github.com/isacikgoz/gitbatch.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d6ede9be207c194f28f8769bfc622f1bccb39aed95406036b76f7f901b61baee"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/gitbatch"
  end

  test do
    mkdir testpath/"repo" do
      system "git", "init"
    end
    assert_match "1 repositories finished", shell_output("#{bin}/gitbatch -q")
  end
end
