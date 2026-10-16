class DroneCli < Formula
  desc "Command-line client for the Drone continuous integration server"
  homepage "https://drone.io"
  url "https://github.com/harness/drone-cli/archive/refs/tags/v1.9.0.tar.gz"
  sha256 "f19786bb5da9e506f6d175c0639bfd0a3f8acf8487ac575a54afba222e0b70d7"
  license "Apache-2.0"
  head "https://github.com/harness/drone-cli.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7b7d8602b78c71823d76e65636cfd60696ec415adba03245f8bba093c5f6ea22"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"
    ldflags = "-X main.version=#{version}"
    system "go", "build", *std_go_args(output: bin/"drone", ldflags:), "drone/main.go"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/drone --version")

    assert_match "manage logs", shell_output("#{bin}/drone log 2>&1")
  end
end
