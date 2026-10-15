class Helm < Formula
  desc "Kubernetes package manager"
  homepage "https://helm.sh/"
  url "https://get.helm.sh/helm-v4.3.1-windows-amd64.zip"
  version "4.3.1"
  sha256 "fcfd7b796dcf0f225abc40b5636a3b01f55f3de2fb1dfb18475df94e62c14112"
  license "Apache-2.0"

  def install
    bin.install "helm.exe"
  end

  test do
    system bin/"helm", "create", "foo"
    assert_predicate testpath/"foo/charts", :directory?

    version_output = shell_output("#{bin}/helm version 2>&1")
    assert_match "Version:\"v#{version}\"", version_output
  end
end
