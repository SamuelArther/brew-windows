class Sshportal < Formula
  desc "SSH & Telnet bastion server"
  homepage "https://v1.manfred.life/sshportal/"
  url "https://github.com/moul/sshportal/archive/refs/tags/v1.19.5.tar.gz"
  sha256 "713be8542c93d91811f9643a8a2954ebc15130099e300fedb5ea4785b5337b52"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f13a706dfcb69cfa9c79e27fa5c089428cb1963fbe30900046e868ebed9feb37"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "1" if OS.linux? && Hardware::CPU.arm?

    ldflags = %W[
      -X main.GitSha=#{tap.user}
      -X main.GitTag=#{version}
    ]
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sshportal --version")

    require "pty"
    stdout, _stdin, _pid = PTY.spawn("#{bin}/sshportal server 2>&1")
    sleep 2
    assert_match "info: system migrated", stdout.readline
  end
end
