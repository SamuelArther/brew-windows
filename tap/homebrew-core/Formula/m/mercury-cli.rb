class MercuryCli < Formula
  desc "CLI interface for Mercury banking"
  homepage "https://github.com/MercuryTechnologies/mercury-cli"
  url "https://github.com/MercuryTechnologies/mercury-cli/releases/download/v0.11.0/mercury_0.11.0_windows_amd64.zip"
  sha256 "2bb8cc72725db17a83c69291ff5ea7b3c6ca5f898badf30db5a5bed2a3e532ff"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MercuryTechnologies/mercury-cli/archive/refs/tags/v0.11.8.tar.gz"
  end

  def install
    bin.install "mercury.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mercury --version")
    assert_match "Authentication Status", shell_output("#{bin}/mercury status 2>&1")
    assert_match "Your dedication to modern banking has not gone unnoticed", pipe_output("#{bin}/mercury hat 2>&1")
  end
end
