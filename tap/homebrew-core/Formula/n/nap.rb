class Nap < Formula
  desc "Code snippets in your terminal"
  homepage "https://github.com/maaslalani/nap"
  url "https://github.com/maaslalani/nap/archive/refs/tags/v0.1.1.tar.gz"
  sha256 "2954577d2bd99c1114989d31e994d7bef0f1c934795fc559b7c90f6370d9f98b"
  license "MIT"
  head "https://github.com/maaslalani/nap.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6f86ec524e520da6bcc1d208d786df5b7b714e74dea2a33ca8634f7e4131a6fd"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "misc/Untitled Snippet.go", shell_output("#{bin}/nap list")
  end
end
