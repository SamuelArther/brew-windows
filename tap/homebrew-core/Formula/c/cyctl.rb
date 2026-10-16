class Cyctl < Formula
  desc "Customizable UI for Kubernetes workloads"
  homepage "https://cyclops-ui.com/"
  url "https://github.com/cyclops-ui/cyclops/releases/download/v0.21.1/cyctl_windows_amd64_v0.21.1.zip"
  sha256 "b6c6bc24450598870b17ef5c5176154a259a07ee6ea2dbed2482397a66d43917"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cyclops-ui/cyclops/archive/refs/tags/v0.21.1.tar.gz"
  end

  def install
    bin.install "cyctl.exe"
  end

  test do
    system bin/"cyctl", "--version"
  end
end
