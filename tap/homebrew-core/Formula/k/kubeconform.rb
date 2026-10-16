class Kubeconform < Formula
  desc "FAST Kubernetes manifests validator, with support for Custom Resources!"
  homepage "https://github.com/yannh/kubeconform"
  url "https://github.com/yannh/kubeconform/releases/download/v0.8.0/kubeconform-windows-amd64.zip"
  sha256 "e3f56102bcf4f50b034a567e2482a1c5330799983ddd655952310211aef73d93"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yannh/kubeconform/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "kubeconform.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kubeconform -v 2>&1")
  end
end
