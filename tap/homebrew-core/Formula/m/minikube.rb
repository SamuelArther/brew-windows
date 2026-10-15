class Minikube < Formula
  desc "Run a Kubernetes cluster locally"
  homepage "https://minikube.sigs.k8s.io/"
  version "1.39.0"
  url "https://github.com/kubernetes/minikube/releases/download/v1.39.0/minikube-windows-amd64.exe"
  sha256 "776386465ded2cf610ae397fe302de32c44d12134b5ce7ce98ab05bd7713b360"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "minikube-windows-amd64.exe" => "minikube.exe"
  end

  test do
    output = shell_output("#{bin}/minikube version")
    assert_match "version: v#{version}", output
  end
end
