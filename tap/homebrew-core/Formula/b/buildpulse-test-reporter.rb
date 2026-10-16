class BuildpulseTestReporter < Formula
  desc "Connect your CI to BuildPulse to detect, track, and rank flaky tests"
  homepage "https://buildpulse.io"
  url "https://github.com/buildpulse/test-reporter/releases/download/v0.29.0/test-reporter-windows-amd64.exe"
  sha256 "f331deaefde0ebb7291ee966ff176025ea2e4ab309b20e90b87658c516cfe458"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/buildpulse/test-reporter/archive/refs/tags/v0.29.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "test-reporter-windows-amd64.exe" => "buildpulse-test-reporter.exe"
  end

  test do
    binary = bin/"buildpulse-test-reporter"
    assert_match version.to_s, shell_output("#{binary} --version")

    fake_dir = "im-not-real"
    assert_match "Received args: #{fake_dir}", shell_output("#{binary} submit #{fake_dir}", 1)
  end
end
