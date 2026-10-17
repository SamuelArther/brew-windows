class Matcha < Formula
  desc "Daily digest generator for your RSS feeds"
  homepage "https://github.com/piqoni/matcha"
  url "https://github.com/piqoni/matcha/archive/refs/tags/v0.9.0.tar.gz"
  sha256 "be0f8674638bdb1c34bd523ca6622f8d73efcfbef18d8002558af2b295caa5e3"
  license "MIT"
  head "https://github.com/piqoni/matcha.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "474286e035125294ba6cdc3988b57d8eac0cf6ad217239c0cfab1590129cebc8"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-s -w")
  end

  test do
    assert_match "Hacker News: Best", shell_output("#{bin}/matcha -t")
  end
end
