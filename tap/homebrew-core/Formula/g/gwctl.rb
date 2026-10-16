class Gwctl < Formula
  desc "CLI for managing and inspecting Gateway API resources in Kubernetes clusters"
  homepage "https://github.com/kubernetes-sigs/gwctl"
  url "https://github.com/kubernetes-sigs/gwctl/archive/refs/tags/v0.2.0.tar.gz"
  sha256 "ee3bf78df7645f518db0adc21aa156507847c4f0727eb949d7d49c8aa8cb9c73"
  license "Apache-2.0"
  head "https://github.com/kubernetes-sigs/gwctl.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fd04570668d9e9850d530ee3d815d80e60ee2e123e4b89403415de6c610a3264"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X sigs.k8s.io/gwctl/pkg/version.version=#{version}
      -X sigs.k8s.io/gwctl/pkg/version.gitCommit=#{tap.user}
      -X sigs.k8s.io/gwctl/pkg/version.buildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"gwctl", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gwctl version")

    output = shell_output("#{bin}/gwctl get gatewayclasses 2>&1", 1)
    assert_match "couldn't get current server API group list", output
  end
end
