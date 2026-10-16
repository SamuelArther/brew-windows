class Kustomizer < Formula
  desc "Package manager for distributing Kubernetes configuration as OCI artifacts"
  homepage "https://kustomizer.dev"
  url "https://github.com/stefanprodan/kustomizer/releases/download/v2.2.1/kustomizer_2.2.1_windows_amd64.zip"
  sha256 "c8ae1781a9e307a58bfb0cea0da7dc8fd356d142a706c904f0f04e4c6ca66f0a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stefanprodan/kustomizer/archive/refs/tags/v2.2.1.tar.gz"
  end

  def install
    bin.install "kustomizer.exe"
  end

  test do
    system bin/"kustomizer", "config", "init"
    assert_match "apiVersion: kustomizer.dev/v1", (testpath/".kustomizer/config").read

    output = shell_output("#{bin}/kustomizer list artifact 2>&1", 1)
    assert_match "you must specify an artifact repository e.g. 'oci://docker.io/user/repo'", output
  end
end
