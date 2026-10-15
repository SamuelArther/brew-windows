class Kind < Formula
  desc "Run local Kubernetes cluster in Docker"
  homepage "https://kind.sigs.k8s.io/"
  url "https://github.com/kubernetes-sigs/kind/releases/download/v0.33.0/kind-windows-amd64"
  sha256 "4b22adaa135368c5a465d56bbd8e520cbea87272a06ca00b6078e7b81515c9fc"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "kind-windows-amd64" => "kind.exe"
  end

  test do
    assert_match "kind v#{version}", shell_output("#{bin}/kind version")
  end
end
