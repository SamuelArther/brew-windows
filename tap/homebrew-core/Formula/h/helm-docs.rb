class HelmDocs < Formula
  desc "Tool for automatically generating markdown documentation for helm charts"
  homepage "https://github.com/norwoodj/helm-docs"
  url "https://github.com/norwoodj/helm-docs/releases/download/v1.14.2/helm-docs_1.14.2_Windows_x86_64.tar.gz"
  sha256 "3c54fd78d99e2769cf83a0faf12cf6cd4de1cac6ed7bee8d908a2c8fc23f538c"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/norwoodj/helm-docs/archive/refs/tags/v1.14.2.tar.gz"
    regex(/^v?(\d+(?:\.\d{1,3})(?:\.\d)*)$/i)
  end

  def install
    bin.install "helm-docs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/helm-docs --version")

    (testpath/"Chart.yaml").write <<~YAML
      apiVersion: v2
      name: test-app
      description: A test Helm chart
      version: 0.1.0
      type: application
    YAML

    (testpath/"values.yaml").write <<~YAML
      replicaCount: 1
      image: "nginx:1.19.10"
      service:
        type: ClusterIP
        port: 80
    YAML

    output = shell_output("#{bin}/helm-docs --chart-search-root . 2>&1")
    assert_match "Generating README Documentation for chart .", output
    assert_match "A test Helm chart", (testpath/"README.md").read
  end
end
