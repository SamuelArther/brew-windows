class Signmykey < Formula
  desc "Automated SSH Certificate Authority"
  homepage "https://signmykey.io/"
  url "https://github.com/signmykeyio/signmykey/archive/refs/tags/v0.8.9.tar.gz"
  sha256 "106a3a3d07aa2841d280ab378ce382a2bfca10101080936e2b02c4cc4e7d7392"
  license "MIT"
  head "https://github.com/signmykeyio/signmykey.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b4691acb4d2500b81f0d64ecec99d9607604c24c7353d9aa4f758f9c78671ffd"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/signmykeyio/signmykey/cmd.versionString=#{version}]
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"signmykey", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/signmykey version")

    require "pty"
    stdout, _stdin, _pid = PTY.spawn("#{bin}/signmykey server dev -u myremoteuser")
    sleep 2
    assert_match "Starting signmykey server in DEV mode", stdout.readline
  end
end
