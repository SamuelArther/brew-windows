class Tfk8s < Formula
  desc "Kubernetes YAML manifests to Terraform HCL converter"
  homepage "https://github.com/jrhouston/tfk8s"
  url "https://github.com/jrhouston/tfk8s/archive/refs/tags/v0.1.10.tar.gz"
  sha256 "be2680e76311ac7dd814a1bb0dceb486e3511d8d68845421338f9fcf5a92d5f9"
  license "MIT"
  head "https://github.com/jrhouston/tfk8s.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6ebfc48744cc53bb0a4e8a49679f0dd8f30bcab17441e0799f0d3edae138b42c"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.toolVersion=#{version}")
  end

  test do
    (testpath/"input.yml").write <<~YAML
      apiVersion: v1
      kind: ConfigMap
      metadata:
        name: test
      data:
        TEST: test
    YAML

    expected = <<~HCL
      resource "kubernetes_manifest" "configmap_test" {
        manifest = {
          "apiVersion" = "v1"
          "data" = {
            "TEST" = "test"
          }
          "kind" = "ConfigMap"
          "metadata" = {
            "name" = "test"
          }
        }
      }
    HCL

    system bin/"tfk8s", "-f", "input.yml", "-o", "output.tf"
    assert_equal expected, File.read("output.tf")

    assert_match version.to_s, shell_output("#{bin}/tfk8s --version")
  end
end
