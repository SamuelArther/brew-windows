class Slackcat < Formula
  desc "Command-line utility for posting snippets to Slack"
  homepage "https://github.com/bcicen/slackcat"
  url "https://github.com/bcicen/slackcat/archive/refs/tags/1.7.3.tar.gz"
  sha256 "2e3ed7ad5ab3075a8e80a6a0b08a8c52bb8e6e39f6ab03597f456278bfa7768b"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3fcd48b9b31746718533c432c8f64b81c7af1c887b74a667c1d09f5e46dca6d0"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/slackcat -v")
  end
end
