class Trivy < Formula
  desc "Vulnerability scanner for container images, file systems, and Git repos"
  homepage "https://trivy.dev/"
  url "https://github.com/aquasecurity/trivy/releases/download/v0.76.0/trivy_0.76.0_windows-64bit.zip"
  sha256 "7b4762a5a5326cb4b83dfd2bb5d9aaf15a1ed867c75cad7dd8b38840a28e7be6"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "trivy.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/trivy --version")
  end
end
