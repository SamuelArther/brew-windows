class Oauth2l < Formula
  desc "Simple CLI for interacting with Google oauth tokens"
  homepage "https://github.com/google/oauth2l"
  url "https://github.com/google/oauth2l/archive/refs/tags/v1.3.6.tar.gz"
  sha256 "5e09371bc245946cadecd3c73de7b782c1f933816678f7f5dff0d5632da5ae0a"
  license "Apache-2.0"
  head "https://github.com/google/oauth2l.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f40455db46f685252ba4d1dd79377f84dc23fb801c8c4a3aa3624e859a7bdbc1"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "Invalid Value", shell_output("#{bin}/oauth2l info abcd1234")
  end
end
