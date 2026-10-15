class KubernetesCli < Formula
  desc "Kubernetes command-line interface"
  homepage "https://kubernetes.io/docs/reference/kubectl/"
  version "1.37.1"
  url "https://dl.k8s.io/release/v1.37.1/bin/windows/amd64/kubectl.exe"
  sha256 "14b93c4916a6f37a06fbc1f46b0a1d2404c4f9741be09f00d6caf8af3b05f1d3"
  license "Apache-2.0"

  def install
    bin.install "kubectl.exe"
  end

  test do
    run_output = shell_output("#{bin}/kubectl 2>&1")
    assert_match "kubectl controls the Kubernetes cluster manager.", run_output

    version_output = shell_output("#{bin}/kubectl version --client --output=yaml 2>&1")
    assert_match "gitVersion: v#{version}", version_output
  end
end
