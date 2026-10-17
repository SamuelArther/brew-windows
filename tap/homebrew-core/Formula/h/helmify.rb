class Helmify < Formula
  desc "Create Helm chart from Kubernetes yaml"
  homepage "https://github.com/arttor/helmify"
  url "https://github.com/arttor/helmify/archive/refs/tags/v0.4.20.tar.gz"
  sha256 "34febd73d083e119cadc0a880152ea4802396e59fe09bf5243ab440f5a54be91"
  license "MIT"
  head "https://github.com/arttor/helmify.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0690ee821355516b4212ada539e7aeec5981113f7dc0abe2634357e347b9be3e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X main.version=#{version}
      -X main.date=#{time.iso8601}
      -X main.commit=#{tap.user}
    ]
    system "go", "build", *std_go_args(ldflags:), "./cmd/helmify"
  end

  test do
    test_service = testpath/"service.yml"
    test_service.write <<~YAML
      apiVersion: v1
      kind: Service
      metadata:
        name: brew-test
      spec:
        type: LoadBalancer
    YAML

    expected_values_yaml = <<~YAML
      brewTest:
        ports: []
        type: LoadBalancer
      kubernetesClusterDomain: cluster.local
    YAML

    system "cat #{test_service} | #{bin}/helmify brewtest"
    assert_path_exists testpath/"brewtest/Chart.yaml"
    assert_equal expected_values_yaml, (testpath/"brewtest/values.yaml").read

    assert_match version.to_s, shell_output("#{bin}/helmify --version")
  end
end
